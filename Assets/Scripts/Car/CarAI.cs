using Assets.Car;
using Assets.Scripts.I76Types;
using Assets.Scripts.System;
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

    public bool Arrived { get; set; }
    public bool Alive { get; private set; }
    public bool Attacked { get; private set; }
    public int Health { get; private set; }
    public int TeamId { get; set; }

    private const float SteeringSensitivity = 1.5f;

    private void Awake()
    {
        Alive = true;
        _transform = transform;
        Health = 100;
        _worldTransform = GameObject.Find("World").transform;
        _car = GetComponent<NewCar>();
        _rigidBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Arrived || _currentPath == null)
        {
            return;
        }

        Vector3 pos = _transform.position;
        pos.y = 0f;

        // Check if we've reached the next node in path.
        if (Vector3.Distance(pos, _targetPos) < Constants.PathMinDistanceTreshold)
        {
            _nodeIndex += _pathReversed ? -1 : 1;

            if (_nodeIndex < 0 || _nodeIndex == _currentPath.Nodes.Length)
            {
                Arrived = true;
                _currentPath = null;
                _car.Throttle = 0.0f;
            }
            else
            {
                Vector3 worldPos = _worldTransform.position;
                _targetPos = new Vector3(worldPos.x + _currentPath.Nodes[_nodeIndex].x, 0.0f, worldPos.z + _currentPath.Nodes[_nodeIndex].z);
            }
        }
        else
        {
            // 'An attempt' at driving in the right direction.

            // Calculate throttle based on target speed.
            float velocity = _rigidBody.velocity.magnitude;
            _car.Throttle = velocity < _targetSpeed ? 1.0f : 0.0f;

            // Steer towards target.
            Vector3 targetVector = (_targetPos - _transform.position).normalized;
            float dot = Vector3.Dot(_transform.right, targetVector) * SteeringSensitivity;
            _steerAngle = Mathf.SmoothDampAngle(_steerAngle, dot, ref _angleVelocity, 0.1f);
            _car.Steer = _steerAngle;
        }
    }

    public void Sit()
    {
        _car.Brake = 1.0f;
        _currentPath = null;
    }

    private void OnDrawGizmos()
    {
        if (Arrived)
        {
            return;
        }
        
        // Draw destination to current path node for debugging.
        Gizmos.color = Color.green;
        Vector3 targetPos = _targetPos;
        targetPos.y = _transform.position.y;
        Gizmos.DrawLine(_transform.position, targetPos);
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