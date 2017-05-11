using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    [HideInInspector]
    public WheelCollider WheelColider;

    private Transform _meshTransform;

    void Awake()
    {
        WheelColider = GetComponent<WheelCollider>();
    }

    void Start()
    {
        _meshTransform = transform.Find("Mesh");
    }

    void FixedUpdate()
    {
        if (_meshTransform == null)
            return;
        Vector3 position;
        Quaternion rotation;
        WheelColider.GetWorldPose(out position, out rotation);
        _meshTransform.position = position;
        _meshTransform.rotation = rotation;
    }
}
