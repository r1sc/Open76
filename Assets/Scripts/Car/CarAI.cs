using System;
using Assets;
using Assets.Car;
using Assets.Fileparsers;
using Assets.Scripts.I76Types;
using Assets.Scripts.System;
using Assets.System;
using UnityEngine;

public class CarAI : MonoBehaviour
{
    private Transform _transform;
    private Vector2 _targetPos;
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
    private Vector2 _targetRoadSegment;
    private Vector2 _nextRoadSegment;
    private int _healthGroups;
    private int _currentHealthGroup;
    private int _health;
    private AudioSource _engineStartSound;
    private AudioSource _engineLoopSound;
    private bool _engineStarting;
    private float _engineStartTimer;
    private CacheManager _cacheManager;
    private GameObject _gameObject;
    private bool _isPlayer;

    public bool Arrived { get; set; }
    public bool Alive
    {
        get { return _health > 0; }
    }
    public bool Attacked { get; private set; }
    public int TeamId { get; set; }
    public bool EngineRunning { get; private set; }
    public Vdf Vdf { get; set; }

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

    private void UpdateEngineSounds()
    {
        string engineStartSound;
        string engineLoopSound;

        switch (Vdf.VehicleSize)
        {
            case 1: // Small car
                engineLoopSound = "eishp";
                engineStartSound = "esshp";
                engineStartSound += _currentHealthGroup;
                break;
            case 2: // Medium car
                engineLoopSound = "eihp";
                engineStartSound = "eshp";
                engineStartSound += _currentHealthGroup;
                break;
            case 3: // Large car
                engineLoopSound = "einp1";
                engineStartSound = "esnp";
                engineStartSound += _currentHealthGroup;
                break;
            case 4: // Van
                engineLoopSound = "eisv";
                engineStartSound = "essv";
                break;
            case 5: // Heavy vehicle
                engineLoopSound = "eimarx";
                engineStartSound = "esmarx";
                break;
            case 6: // Tank
                engineLoopSound = "eitank";
                engineStartSound = "estank";
                break;
            default:
                Debug.LogWarningFormat("Unhandled vehicle size '{0}'. No vehicle sounds loaded.", Vdf.VehicleSize);
                return;
        }

        engineStartSound += ".gpw";
        engineLoopSound += ".gpw";
        if (_engineStartSound == null || _engineStartSound.clip.name != engineStartSound)
        {
            if (_engineStartSound != null)
            {
                Destroy(_engineStartSound);
            }

            _engineStartSound = _cacheManager.GetAudioSource(_gameObject, engineStartSound);
            _engineStartSound.volume = 0.6f;
        }

        if (_engineLoopSound == null || _engineLoopSound.clip.name != engineLoopSound)
        {
            if (_engineLoopSound != null)
            {
                Destroy(_engineLoopSound);
            }

            _engineLoopSound = _cacheManager.GetAudioSource(_gameObject, engineLoopSound);
            _engineLoopSound.loop = true;
            _engineLoopSound.volume = 0.6f;

            if (EngineRunning)
            {
                _engineLoopSound.Play();
            }
        }
    }

    private void Start()
    {
        _isPlayer = TeamId == 1;
        _transform = transform;
        _health = StartHealth;
        _worldTransform = GameObject.Find("World").transform;
        _car = GetComponent<NewCar>();
        _rigidBody = GetComponent<Rigidbody>();
        EngineRunning = true;
        _cacheManager = FindObjectOfType<CacheManager>();
        _gameObject = gameObject;
        _currentHealthGroup = 1;

        UpdateEngineSounds();

        if (_transform.childCount > 0)
        {
            _healthGroups = _transform.Find("Chassis/ThirdPerson").childCount;
        }
    }

    private void ToggleEngine()
    {
        if (_engineStartSound.isPlaying)
        {
            return;
        }

        if (EngineRunning)
        {
            _engineLoopSound.Stop();
            EngineRunning = false;
        }
        else
        {
            _engineStartSound.Play();
            _engineStarting = true;
            _engineStartTimer = 0f;
        }
    }
    
    private void Update()
    {
        if (_health <= 0)
        {
            return;
        }

        if (EngineRunning)
        {
            // Simple and temporary engine pitch adjustment code based on rigidbody velocity - should be using wheels.
            const float firstGearTopSpeed = 40f;
            const float gearRatioAdjustment = 1.5f;
            const float minPitch = 0.6f;
            const float maxPitch = 1.2f;

            float velocity = _rigidBody.velocity.magnitude;
            float gearMaxSpeed = firstGearTopSpeed;
            while (velocity / gearMaxSpeed > maxPitch - minPitch)
            {
                gearMaxSpeed *= gearRatioAdjustment;
            }

            float enginePitch = minPitch + velocity / gearMaxSpeed;
            _engineLoopSound.pitch = enginePitch;
        }

        if (_engineStarting)
        {
            _engineStartTimer += Time.deltaTime;
            if (_engineStartTimer > _engineStartSound.clip.length - 0.5f)
            {
                _engineLoopSound.Play();
                EngineRunning = true;
                _engineStarting = false;
                _engineStartTimer = 0f;
            }
        }

        // Start / Stop engine.
        if (_isPlayer && Input.GetKeyDown(KeyCode.S))
        {
            ToggleEngine();
        }

        Navigate();
    }

    private void SetHealthGroup(int healthGroupIndex)
    {
        _currentHealthGroup = healthGroupIndex;
        Transform parent = transform.Find("Chassis/ThirdPerson");
        for (int i = 0; i < _healthGroups; ++i)
        {
            Transform child = parent.GetChild(i);
            child.gameObject.SetActive(healthGroupIndex == i + 1);
        }

        if (_health > 0)
        {
            UpdateEngineSounds();
        }
    }

    private void Navigate()
    {
        if (Arrived || _currentPath == null)
        {
            return;
        }

        Vector3 pos = _transform.position;
        Vector2 pos2D;
        pos2D.x = pos.x;
        pos2D.y = pos.z;

        float velocity = _rigidBody.velocity.magnitude;
        float adjustedTargetSpeed = _targetSpeed;
        float distanceTreshold = Constants.PathMinDistanceTreshold;
        
        // Find the closest road point.
        Road[] closestRoads = RoadManager.Instance.GetRoadsAroundPoint(pos);
        Vector2 targetVector = (_targetPos - pos2D).normalized;
        Vector2 lineOffset = targetVector * Mathf.Max(5f, velocity);
        Road nextRoad = null;

        // Check for the closest road and it's closest segment.
        _targetRoadSegment = _targetPos;

        const float minRoadDistance = Constants.PathMinDistanceTreshold; // If roads are not closer than this this then we should prefer to drive off-road instead.

        float currentClosestSegmentDistance = minRoadDistance;
        float nextClosestSegmentDistance = minRoadDistance;

        Vector2 vecOffset;
        for (int i = 0; i < closestRoads.Length; ++i)
        {
            for (int j = 0; j < closestRoads[i].Segments.Length; ++j)
            {
                Vector2 segmentPos = closestRoads[i].Segments[j];

                Vector2 lineStartPoint;
                lineStartPoint.x = pos2D.x + lineOffset.x;
                lineStartPoint.y = pos2D.y + lineOffset.y;

                // Check for the closest point to the car's position.
                vecOffset.x = lineStartPoint.x - segmentPos.x;
                vecOffset.y = lineStartPoint.y - segmentPos.y;
                float currentRoadDistance = (float)Math.Sqrt(vecOffset.x * vecOffset.x + vecOffset.y * vecOffset.y);
                if (currentRoadDistance < currentClosestSegmentDistance)
                {
                    currentClosestSegmentDistance = currentRoadDistance;
                    _targetRoadSegment = segmentPos;
                }

                lineStartPoint.x = pos2D.x + lineOffset.x * 2;
                lineStartPoint.y = pos2D.y + lineOffset.y * 2;

                // Check for the next road point.
                vecOffset.x = lineStartPoint.x - segmentPos.x;
                vecOffset.y = lineStartPoint.y - segmentPos.y;
                float nextRoadDistance = (float)Math.Sqrt(vecOffset.x * vecOffset.x + vecOffset.y * vecOffset.y);
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
            vecOffset.x = pos2D.x - _targetRoadSegment.x;
            vecOffset.y = pos2D.y - _targetRoadSegment.y;
            if ((float)Math.Sqrt(vecOffset.x * vecOffset.x + vecOffset.y * vecOffset.y) < distanceTreshold)
            {
                _currentRoad = nextRoad;
            }
        }

        // Accelerate if we're going below the target speed.
        _car.Throttle = velocity < adjustedTargetSpeed ? 1.0f : 0.0f;

        // Brake if we're going too fast.
        _car.Brake = (velocity > adjustedTargetSpeed + 5.0f) ? 1.0f : 0.0f;

        // Steer towards target.
        Vector2 segmentVector = (_targetRoadSegment - pos2D).normalized;
        Vector3 segmentVector3D = new Vector3(segmentVector.x, 0.0f, segmentVector.y);
        float dot = Vector3.Dot(_transform.right, segmentVector3D) * SteeringSensitivity;
        _steerAngle = Mathf.SmoothDampAngle(_steerAngle, dot, ref _angleVelocity, 0.1f);
        _car.Steer = _steerAngle;
        
        // Check if we've reached the next node in path.
        vecOffset.x = pos2D.x - _targetPos.x;
        vecOffset.y = pos2D.y - _targetPos.y;
        if ((float)Math.Sqrt(vecOffset.x * vecOffset.x + vecOffset.y * vecOffset.y) < distanceTreshold)
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
            _targetPos.x = worldPos.x + _currentPath.Nodes[_nodeIndex].x;
            _targetPos.y = worldPos.z + _currentPath.Nodes[_nodeIndex].z;
        }
    }

    private void Explode()
    {
        AudioSource explosionSource = _cacheManager.GetAudioSource(_gameObject, "xcar");
        if (explosionSource != null)
        {
            explosionSource.volume = 0.9f;
            explosionSource.Play();
        }

        _rigidBody.AddForce(Vector3.up * _rigidBody.mass * 5f, ForceMode.Impulse);
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

        EngineRunning = false;
        Destroy(_engineLoopSound);
        Destroy(_engineStartSound);
        Destroy(GetComponent<NewCar>());

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
        Vector3 targetPos = new Vector3(_targetPos.x, Utils.GroundHeightAtPoint(_targetPos.x, _targetPos.y), _targetPos.y);
        Gizmos.DrawLine(pos, targetPos);

        // Draw line to current road segment.
        Gizmos.color = Color.yellow;
        targetPos = new Vector3(_targetRoadSegment.x, Utils.GroundHeightAtPoint(_targetRoadSegment.x, _targetRoadSegment.y), _targetRoadSegment.y);
        Gizmos.DrawLine(pos, targetPos);

        // Draw line to next road segment.
        Gizmos.color = Color.magenta;
        targetPos = new Vector3(_nextRoadSegment.x, Utils.GroundHeightAtPoint(_nextRoadSegment.x, _nextRoadSegment.y), _nextRoadSegment.y);
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
        _targetPos = new Vector2(worldPos.x + path.Nodes[_nodeIndex].x, worldPos.z + path.Nodes[_nodeIndex].z);
        Arrived = false;
    }
}