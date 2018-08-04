using System.Collections.Generic;
using Assets;
using Assets.Car;
using Assets.Scripts.I76Types;
using Assets.Scripts.System;
using Assets.System;
using UnityEngine;

public class CarAI : MonoBehaviour
{
    private Transform _transform;
    private Vector3 _targetPos;
    private FSMPath _currentPath;
    private Rigidbody _rigidBody;
    private int _nodeIndex;
    private float _targetSpeed;
    private bool _pathReversed;
    private Transform _worldTransform;
    private float _angleVelocity;
    private float _steerAngle;
    private NewCar _car;
    private Road _currentRoad;
    private Vector3 _targetRoadSegment;
    private Vector3 _nextRoadSegment;
    private int _healthGroups;
    private int _health;

    public bool Arrived { get; set; }
    public bool Alive
    {
        get { return _health > 0; }
    }
    public bool Attacked { get; private set; }
    public int TeamId { get; set; }
    
    public int Health
    {
        get { return _health; }
        private set
        {
            if (_health <= 0)
            {
                return;
            }

            _health = value;
            SetHealthGroup(_healthGroups - Mathf.FloorToInt((float)_health / (StartHealth + 1) * _healthGroups));

            if (_health <= 0)
            {
                Explode();
            }
        }
    }

    private const float SteeringSensitivity = 1.5f;
    private const int StartHealth = 100;

    private void Awake()
    {
        _transform = transform;
        _health = StartHealth;
        _worldTransform = GameObject.Find("World").transform;
        _car = GetComponent<NewCar>();
        _rigidBody = GetComponent<Rigidbody>();
        _healthGroups = transform.Find("Chassis/ThirdPerson").childCount;
    }

    private void Update()
    {
        if (_health <= 0)
        {
            return;
        }

        Navigate();
    }

    private void SetHealthGroup(int healthGroupIndex)
    {
        Transform parent = transform.Find("Chassis/ThirdPerson");
        for (int i = 0; i < _healthGroups; ++i)
        {
            Transform child = parent.GetChild(i);
            child.gameObject.SetActive(healthGroupIndex == i + 1);
        }
    }

    private void Navigate()
    {
        if (Arrived || _currentPath == null)
        {
            return;
        }

        Vector3 pos = _transform.position;
        Vector3 flatPos = new Vector3(pos.x, 0.0f, pos.z);
        float velocity = _rigidBody.velocity.magnitude;
        
        float adjustedTargetSpeed = _targetSpeed;
        float distanceTreshold = Constants.PathMinDistanceTreshold;
        
        // Find the closest road point.
        List<Road> closestRoads = RoadManager.Instance.GetRoadsAroundPoint(pos);
        Vector3 targetVector = (_targetPos - pos).normalized;
        Vector3 lineOffset = targetVector * Mathf.Max(5f, velocity);
        Road nextRoad = null;

        // Check for the closest road and it's closest segment.
        _targetRoadSegment = _targetPos;

        const float minRoadDistance = Constants.PathMinDistanceTreshold; // If roads are not closer than this this then we should prefer to drive off-road instead.

        float currentClosestSegmentDistance = minRoadDistance;
        float nextClosestSegmentDistance = minRoadDistance;
        for (int i = 0; i < closestRoads.Count; ++i)
        {
            for (int j = 0; j < closestRoads[i].Segments.Length; ++j)
            {
                Vector3 segmentPos = closestRoads[i].Segments[j];
                Vector3 currentRoadPoint = Utils.GetClosestPointOnLineSegment(pos + lineOffset, _targetPos, segmentPos);
                Vector3 nextRoadPoint = Utils.GetClosestPointOnLineSegment(pos + lineOffset * 2, _targetPos, segmentPos);

                // Check for the closest point to the car's position.
                float currentRoadDistance = Vector3.Distance(currentRoadPoint, segmentPos);
                if (currentRoadDistance < currentClosestSegmentDistance)
                {
                    currentClosestSegmentDistance = currentRoadDistance;
                    _targetRoadSegment = segmentPos;
                }

                // Check for the next road point.
                float nextRoadDistance = Vector3.Distance(nextRoadPoint, segmentPos);
                if (nextRoadDistance < nextClosestSegmentDistance)
                {
                    nextClosestSegmentDistance = nextRoadDistance;
                    _nextRoadSegment = segmentPos;
                    nextRoad = closestRoads[i];
                }
            }
        }
        
        // If the next road point is not on the same road as the current one, we slow down and make sure we don't cut corners.
        if (_currentRoad != nextRoad)
        {
            if (_currentRoad == null)
            {
                _currentRoad = nextRoad;
                _targetRoadSegment = _nextRoadSegment;
            }
            else
            {
                distanceTreshold = 5f;
                adjustedTargetSpeed = 5f;
            }
            
            // Check if we need to transition between roads.
            Vector3 flatRoadSegment = new Vector3(_targetRoadSegment.x, 0f, _targetRoadSegment.z);
            if (Vector3.Distance(flatPos, flatRoadSegment) < distanceTreshold)
            {
                _currentRoad = nextRoad;
            }
        }

        // Accelerate if we're going below the target speed.
        _car.Throttle = velocity < adjustedTargetSpeed ? 1.0f : 0.0f;

        // Brake if we're going too fast.
        _car.Brake = (velocity > adjustedTargetSpeed + 5.0f) ? 1.0f : 0.0f;

        // Steer towards target.
        Vector3 segmentVector = (_targetRoadSegment - pos).normalized;
        float dot = Vector3.Dot(_transform.right, segmentVector) * SteeringSensitivity;
        _steerAngle = Mathf.SmoothDampAngle(_steerAngle, dot, ref _angleVelocity, 0.1f);
        _car.Steer = _steerAngle;
        
        // Check if we've reached the next node in path.
        if (Vector3.Distance(flatPos, _targetPos) < distanceTreshold)
        {
            NextWaypoint();
        }
    }

    private void NextWaypoint()
    {
        _nodeIndex += _pathReversed ? -1 : 1;

        if (_nodeIndex < 0 || _nodeIndex == _currentPath.Nodes.Length)
        {
            Arrived = true;
            _currentPath = null;
            _currentRoad = null;
            _car.Throttle = 0.0f;
        }
        else
        {
            Vector3 worldPos = _worldTransform.position;
            _targetPos = new Vector3(worldPos.x + _currentPath.Nodes[_nodeIndex].x, 0.0f, worldPos.z + _currentPath.Nodes[_nodeIndex].z);
        }
    }

    private void Explode()
    {
        _rigidBody.AddForce(Vector3.up * _rigidBody.mass * 5f, ForceMode.Impulse);

        SoundManager.Instance.PlaySoundAtObject(SoundEffect.VehicleExplode, transform);

        InputCarController inputController = GetComponent<InputCarController>();
        if (inputController != null)
        {
            Destroy(inputController);
        }

        NewCar carPhysics = GetComponent<NewCar>();
        if (carPhysics != null)
        {
            Destroy(carPhysics);
        }

        Destroy(transform.Find("FrontLeft").gameObject);
        Destroy(transform.Find("FrontRight").gameObject);
        Destroy(transform.Find("BackLeft").gameObject);
        Destroy(transform.Find("BackRight").gameObject);
    }

    public void Kill()
    {
        Health = 0;
    }

    public void Sit()
    {
        _car.Brake = 1.0f;
        _currentPath = null;
    }

    private void OnDrawGizmos()
    {
        if (_currentPath == null || _health <= 0)
        {
            return;
        }

        Vector3 pos = _transform.position;

        // Draw line to target path node.
        Gizmos.color = Color.green;
        Vector3 targetPos = _targetPos;
        targetPos.y = Utils.GroundHeightAtPoint(targetPos);
        Gizmos.DrawLine(pos, targetPos);

        // Draw line to current road segment.
        Gizmos.color = Color.yellow;
        targetPos = _targetRoadSegment;
        Gizmos.DrawLine(pos, targetPos);

        // Draw line to next road segment.
        Gizmos.color = Color.magenta;
        targetPos = _nextRoadSegment;
        Gizmos.DrawLine(pos, targetPos);
    }

    public void SetSpeed(int targetSpeed)
    {
        _rigidBody.velocity = _transform.forward * targetSpeed;
    }

    public void SetTargetPath(FSMPath path, int targetSpeed)
    {
        // Ignore instruction if path is the current path.
        if (path == _currentPath)
        {
            return;
        }

        // Check which end of the path is closest.
        // This is correct if you look at the training mission - there are two cars that share the same path from opposite ends.
        Vector3 pos = _transform.position;
        Vector3 worldPos = _worldTransform.position;
        Vector3 pathStart = worldPos + path.Nodes[0].ToVector3();
        Vector3 pathEnd = worldPos + path.Nodes[path.Nodes.Length - 1].ToVector3();

        float startDistance = Vector3.Distance(pos, pathStart);
        float endDistance = Vector3.Distance(pos, pathEnd);

        if (startDistance < endDistance)
        {
            _nodeIndex = 0;
            _pathReversed = false;
        }
        else
        {
            _nodeIndex = path.Nodes.Length - 1;
            _pathReversed = true;
        }

        _car.Brake = 0.0f;
        _currentPath = path;
        _targetSpeed = targetSpeed * Constants.SpeedUnitRatio;
        _targetPos = new Vector3(worldPos.x + path.Nodes[_nodeIndex].x, 0.0f, worldPos.z + path.Nodes[_nodeIndex].z);
        Arrived = false;
    }
}