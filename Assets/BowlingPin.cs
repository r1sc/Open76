using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowlingPin : MonoBehaviour
{
    public Transform[] Wheels;
    public float SuspensionLength;
    private Rigidbody _rigidbody;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass += Vector3.down;
    }

    // Update is called once per frame
    void Update()
    {
        //var c1 = GetSuspensionCompression(Wheels[0].position);
        //var tp1 = Wheels[0].position + transform.up*c1*0.25f;
        //var c2 = GetSuspensionCompression(Wheels[1].position);
        //var tp2 = Wheels[1].position + transform.up * c2 * 0.25f;
        //var c3 = GetSuspensionCompression(Wheels[2].position);
        //var tp3 = Wheels[2].position + transform.up * c3 * 0.25f;
        //var c4 = GetSuspensionCompression(Wheels[3].position);
        //var tp4 = Wheels[3].position + transform.up * c4 * 0.25f;


        ////var v1 = GetWheelPosition(tp1) - GetWheelPosition(Wheels[0].position);
        ////var v2 = GetWheelPosition(Wheels[2].position) - GetWheelPosition(Wheels[0].position);
        ////var v3 = GetWheelPosition(Wheels[3].position) - GetWheelPosition(Wheels[0].position);
        //var normal1 = Vector3.Cross(tp1, tp3);
        //var normal2 = Vector3.Cross(tp2, tp4);
        //var normal = ((normal1 + normal2) / 2.0f).normalized;
        //Debug.DrawLine(transform.position, transform.position + normal * 2);
        ////transform.position += normal;
        
        //transform.position += transform.up*((c1 + c2 + c3 + c4)/4.0f);
        
        //var targetPosition = transform.up;
    }

    Vector3 GetWheelPosition(Vector3 suspensionPosition)
    {
        RaycastHit hit;
        if (Physics.Raycast(suspensionPosition, -transform.up, out hit, SuspensionLength))
        {
            return hit.point;
        }
        return suspensionPosition;
    }

    float GetSuspensionCompression(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position, -transform.up, out hit, SuspensionLength))
        {
            var compression = SuspensionLength - hit.distance;
            return compression;
        }
        return 0;
    }

    void FixedUpdate()
    {

    }
}
