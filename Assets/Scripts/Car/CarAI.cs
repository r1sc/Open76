﻿using System;
using Assets.Scripts.I76Types;
using Assets.Scripts.System;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.Car
{
    public class CarAI
    {
        private const float SteeringSensitivity = 1.5f;

        private readonly CarController _controller;
        private readonly CarMovement _movement;
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

        public CarAI(CarController controller)
        {
            _controller = controller;
            _transform = _controller.transform;
            _movement = controller.Movement;
            _rigidBody = _controller.GetComponent<Rigidbody>();
            _worldTransform = GameObject.Find("World").transform;
        }

        public void Sit()
        {
            _currentPath = null;
        }

        public void DrawGizmos()
        {
            if (_currentPath == null || _controller.Health <= 0)
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
            if (_controller.Arrived || _currentPath == null)
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
            _movement.Throttle = velocity < adjustedTargetSpeed ? 1.0f : 0.0f;

            // Brake if we're going too fast.
            _movement.Brake = (velocity > adjustedTargetSpeed + 5.0f) ? 1.0f : 0.0f;

            // Steer towards target.
            Vector2 segmentVector = (_targetRoadSegment - pos2D).normalized;
            Vector3 segmentVector3D = new Vector3(segmentVector.x, 0.0f, segmentVector.y);
            float dot = Vector3.Dot(_transform.right, segmentVector3D) * SteeringSensitivity;
            _steerAngle = Mathf.SmoothDampAngle(_steerAngle, dot, ref _angleVelocity, 0.1f);
            _movement.Steer = _steerAngle;

            // Check if we've reached the next node in path.
            vecOffset.x = pos2D.x - _targetPos.x;
            vecOffset.y = pos2D.y - _targetPos.y;
            if ((float)Math.Sqrt(vecOffset.x * vecOffset.x + vecOffset.y * vecOffset.y) < distanceTreshold)
            {
                NextWaypoint();
            }
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
                _movement.Throttle = 0.0f;
            }
            else
            {
                Vector3 worldPos = _worldTransform.position;
                _targetPos.x = worldPos.x + _currentPath.Nodes[_nodeIndex].x;
                _targetPos.y = worldPos.z + _currentPath.Nodes[_nodeIndex].z;
            }
        }
    }
}
