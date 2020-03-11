using System;
using System.Collections.Generic;
using Assets.Scripts.Entities;
using Assets.Scripts.I76Types;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.CarSystems
{
    public class CarAI
    {
        private const float FollowDistanceTreshold = 10f;
        private const float SteeringSensitivity = 1.5f;
        private const float RoadOffsetDistance = 2f;

        private readonly Car _controller;
        private readonly CarPhysics _carPhysics;
        private readonly Transform _transform;
        private readonly Rigidbody _rigidBody;
        private FSMPath _currentPath;
        private bool _pathReversed;
        private Road _currentRoad;
        private Vector2 _targetRoadSegment;
        private Vector2 _nextRoadSegment;
        private Vector2 _targetPos;
        private int _targetSpeed;
        private readonly Transform _worldTransform;
        private int _nodeIndex;
        private float _angleVelocity;
        private float _steerAngle;
        private Car _followTarget;
        private int _followXOffset;
        private Vector2 _lastRoadOffset;

        public CarAI(Car controller)
        {
            _controller = controller;
            _transform = _controller.transform;
            _carPhysics = controller.GetComponent<CarPhysics>();
            _rigidBody = _controller.GetComponent<Rigidbody>();
            _worldTransform = GameObject.Find("World").transform;
        }

        public void Sit()
        {
            _currentPath = null;
        }

        public void DrawGizmos()
        {
            if (_currentPath == null || !_controller.Alive)
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

        public void Navigate()
        {
            float adjustedTargetSpeed = _targetSpeed;

            if (_followTarget != null)
            {
                Vector3 localPos = _transform.position;
                Vector3 carPos = _followTarget.transform.position;
                Vector3 direction = _followTarget.transform.forward;
                _targetPos.x = carPos.x + direction.x * 20f;
                _targetPos.y = carPos.z + direction.z * 20f;

                float distance = Vector2.Distance(_targetPos, new Vector2(localPos.x, localPos.z));

                if (distance > 10f)
                {
                    adjustedTargetSpeed = float.MaxValue;
                }
                else if (distance < 5f)
                {
                    adjustedTargetSpeed = _followTarget.AI._targetSpeed - 5;
                }
            }

            if (_controller.Arrived || (_currentPath == null && _followTarget == null))
            {
                return;
            }

            Vector3 pos = _transform.position;
            Vector2 pos2D;
            pos2D.x = pos.x - _lastRoadOffset.x;
            pos2D.y = pos.z - _lastRoadOffset.y;

            float velocity = _rigidBody.velocity.magnitude;
            float distanceTreshold = Constants.PathMinDistanceTreshold;

            // Find the closest road point.
            List<Road> closestRoads = RoadManager.Instance.GetRoadsAroundPoint(pos);
            Vector2 targetVector = (_targetPos - pos2D).normalized;
            Vector2 lineOffset = targetVector * Mathf.Max(5f, velocity);
            Road nextRoad = null;

            // Check for the closest road and it's closest segment.
            _targetRoadSegment = _targetPos;

            const float minRoadDistance = Constants.PathMinDistanceTreshold; // If roads are not closer than this this then we should prefer to drive off-road instead.

            float currentClosestSegmentDistance = minRoadDistance;
            float nextClosestSegmentDistance = minRoadDistance;

            Vector2 vecOffset;
            int roadCount = closestRoads.Count;
            for (int i = 0; i < roadCount; ++i)
            {
                Road closestRoad = closestRoads[i];
                for (int j = 0; j < closestRoad.Segments.Length; ++j)
                {
                    Vector2 segmentPos = closestRoad.Segments[j];

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
                        nextRoad = closestRoad;
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
            _carPhysics.Throttle = velocity < adjustedTargetSpeed ? 1.0f : 0.0f;

            // Brake if we're going too fast.
            _carPhysics.Brake = (velocity > adjustedTargetSpeed + 5.0f) ? 1.0f : 0.0f;

            // Offset to right side of road.
            if (_followTarget == null)
            { 
                Vector2 roadVector = (_nextRoadSegment - _targetRoadSegment).normalized;
                Vector3 roadCross = Vector3.Cross(Vector3.up, new Vector3(roadVector.x, 0f, roadVector.y)) * RoadOffsetDistance;
                _lastRoadOffset.x = roadCross.x;
                _lastRoadOffset.y = roadCross.z;
            }

            // Steer towards target.
            Vector2 segmentVector;
            if (_followXOffset != 0 && _followTarget != null)
            {
                Vector3 relativeRight = Vector3.Cross(Vector3.up, (_followTarget.transform.position - _transform.position).normalized) * _followXOffset * 0.5f;
                segmentVector = ((_targetRoadSegment + new Vector2(relativeRight.x, relativeRight.z)) - pos2D).normalized;
            }
            else
            {
                segmentVector = (_targetRoadSegment - pos2D).normalized;
            }

            Vector3 segmentVector3D = new Vector3(segmentVector.x, 0.0f, segmentVector.y);
            float dot = Vector3.Dot(_transform.right, segmentVector3D) * SteeringSensitivity;
            _steerAngle = Mathf.SmoothDampAngle(_steerAngle, dot, ref _angleVelocity, 0.1f);
            _carPhysics.Steer = _steerAngle;

            // Check if we've reached the next node in path.
            if (_followTarget == null)
            {
                vecOffset.x = pos2D.x - _targetPos.x;
                vecOffset.y = pos2D.y - _targetPos.y;
                if ((float) Math.Sqrt(vecOffset.x * vecOffset.x + vecOffset.y * vecOffset.y) < distanceTreshold)
                {
                    NextWaypoint();
                }
            }
        }

        public bool AtFollowTarget()
        {
            if (_followTarget == null)
            {
                return false;
            }

            Vector3 offset = _followTarget.transform.forward * 20f;
            float distance = Vector3.Distance(_transform.position, _followTarget.transform.position + offset);
            return distance < FollowDistanceTreshold;
        }

        public void SetFollowTarget(Car car, int xOffset, int targetSpeed)
        {
            _targetSpeed = targetSpeed;
            _followXOffset = xOffset;

            if (_followTarget == car)
            {
                return;
            }

            _followTarget = null;
            _currentPath = null;

            if (!car.Alive)
            {
                return;
            }

            _followTarget = car;
        }

        public void SetTargetPath(FSMPath path, int targetSpeed)
        {
            _followTarget = null;

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

            _currentPath = path;
            _targetSpeed = targetSpeed;
            _targetPos = new Vector2(worldPos.x + path.Nodes[_nodeIndex].x, worldPos.z + path.Nodes[_nodeIndex].z);
        }

        private void NextWaypoint()
        {
            _nodeIndex += _pathReversed ? -1 : 1;

            if (_nodeIndex < 0 || _nodeIndex == _currentPath.Nodes.Length)
            {
                _controller.Arrived = true;
                _currentPath = null;
                _currentRoad = null;
                _carPhysics.Throttle = 0.0f;
            }
            else
            {
                Vector3 worldPos = _worldTransform.position;
                _targetPos.x = worldPos.x + _currentPath.Nodes[_nodeIndex].x;
                _targetPos.y = worldPos.z + _currentPath.Nodes[_nodeIndex].z;
            }
        }

        public bool IsWithinNav(FSMPath path, int distance)
        {
            I76Vector3[] nodes = path.Nodes;
            int nodeCountMin1 = nodes.Length - 1;

            Vector3 worldPos = _worldTransform.position;
            Vector3 carPos = _transform.position;
            Vector2 pos2D = new Vector2(carPos.x, carPos.z);
            
            if (nodeCountMin1 == 0)
            {
                Vector2 nodePos;
                nodePos.x = worldPos.x + nodes[0].x;
                nodePos.y = worldPos.z + nodes[0].z;
                return Vector2.Distance(nodePos, pos2D) < distance;
            }

            for (int i = 0; i < nodeCountMin1; ++i)
            {
                Vector2 nodeStart, nodeEnd;
                nodeStart.x = worldPos.x + nodes[i].x;
                nodeStart.y = worldPos.z + nodes[i].z;
                nodeEnd.x = worldPos.x + nodes[i + 1].x;
                nodeEnd.y = worldPos.z + nodes[i + 1].z;

                float navDistance = Utils.DistanceFromPointToLine(nodeStart, nodeEnd, pos2D);
                if (navDistance < distance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
