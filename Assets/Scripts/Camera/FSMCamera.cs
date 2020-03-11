using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.I76Types;
using Assets.Scripts.System;
using UnityEngine;

public class FSMCamera : MonoBehaviour
{
    public bool Arrived { get; set; }
    private FSMPath _currentPath;
    private Vector2 _targetPos;
    private float _heightOffset;
    private float _targetSpeed;
    private Transform _worldTransform;
    private int _nodeIndex;
    private bool _pathReversed;
    private Vector3 _lastOffset;

    // Start is called before the first frame update
    void Start()
    {
        _worldTransform = GameObject.Find("World").transform;
    }

    public void DrawGizmos()
    {
        if (_currentPath == null)
        {
            return;
        }

        Vector3 pos = transform.position;

        // Draw line to target path node.
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, _targetPos);
    }

    // Update is called once per frame
    void Update()
    {
        FollowPath();
    }

    public void FollowPath()
    {
        if (_currentPath == null)
        {
            return;
        }

        float distanceTreshold = Constants.PathMinDistanceTreshold;

        var pos2d = new Vector2(transform.position.x, transform.position.z);
        var offset = _targetPos - pos2d;
        var direction = offset.normalized;

        var newPos2d = pos2d + direction * _targetSpeed * Time.deltaTime;
        var newHeight = Utils.GroundHeightAtPoint(newPos2d.x, newPos2d.y) + _heightOffset;
        var newPos3d = new Vector3(newPos2d.x, newHeight, newPos2d.y);

        transform.position = newPos3d;

        // Check if we've reached the next node in path.
        if (offset.magnitude < distanceTreshold)
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
        }
        else
        {
            Vector3 worldPos = _worldTransform.position;
            _targetPos.x = worldPos.x + _currentPath.Nodes[_nodeIndex].x;
            _targetPos.y = worldPos.z + _currentPath.Nodes[_nodeIndex].z;
        }
    }


    public void SetTargetPath(FSMPath path, int targetSpeed, float heightOffset)
    {
        _heightOffset = heightOffset;

        // Ignore instruction if path is the current path.
        if (path == _currentPath)
        {
            return;
        }

        // Check which end of the path is closest.
        // This is correct if you look at the training mission - there are two cars that share the same path from opposite ends.
        Vector3 pos = transform.position;
        Vector3 worldPos = GameObject.Find("World").transform.position;
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

}
