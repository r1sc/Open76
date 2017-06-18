using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class InCarCamera : MonoBehaviour
{
    private ArcadeCar _arcadecar;

    void Start()
    {
        _arcadecar = GetComponentInParent<ArcadeCar>();
    }

    void Update()
    {
        var rotX = (_arcadecar.Throttle - _arcadecar.Brake) * 4;
        var rotZ = _arcadecar.Steering*2;
        transform.localRotation = Quaternion.AngleAxis(-rotX, Vector3.right) * Quaternion.AngleAxis(rotZ, Vector3.forward);
    }
}