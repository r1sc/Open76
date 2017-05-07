using UnityEngine;
using System.Collections;

public class Swaybar : MonoBehaviour
{
    public WheelCollider WheelL, WheelR;
    public float AntiRollForce = 1500;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {

        WheelHit hit;
        float travelL;
        var groundedL = WheelL.GetGroundHit(out hit);
        if (groundedL)
            travelL = (-WheelL.transform.InverseTransformPoint(hit.point).y - WheelL.radius) / WheelL.suspensionDistance;
        else
            travelL = 1.0f;

        float travelR;
        var groundedR = WheelR.GetGroundHit(out hit);
        if (groundedR)
            travelR = (-WheelR.transform.InverseTransformPoint(hit.point).y - WheelR.radius) / WheelR.suspensionDistance;
        else
            travelR = 1.0f;

        var antiRollForce = (travelL - travelR) * AntiRollForce;
        if (groundedL)
            GetComponent<Rigidbody>().AddForceAtPosition(WheelL.transform.up * -antiRollForce, WheelL.transform.position);

        if (groundedR)
            GetComponent<Rigidbody>().AddForceAtPosition(WheelR.transform.up * antiRollForce, WheelR.transform.position);
    }
}
