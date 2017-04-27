using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Wheel : MonoBehaviour
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
        Vector3 position;
        Quaternion rotation;
        WheelColider.GetWorldPose(out position, out rotation);
        _meshTransform.position = position;
        _meshTransform.rotation = rotation;
    }
}
