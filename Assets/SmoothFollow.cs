using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class SmoothFollow : MonoBehaviour
{
    /*
    This camera smoothes out rotation around the y-axis and height.
    Horizontal Distance to the target is always fixed.
    
    There are many different ways to smooth the rotation but doing it this way gives you a lot of control over how the camera behaves.
    
    For every of those smoothed values we calculate the wanted value and the current value.
    Then we smooth it using the Lerp function.
    Then we apply the smoothed values to the transform's position.
    */
    
    // The target we are following
    public Transform Target;
    // The distance in the x-z plane to the target
    public float Distance = 10.0f;
    // the height we want the camera to be above the target
    public float Height = 5.0f;
    // How much we 
    public float HeightDamping = 2.0f;
    public float RotationDamping = 3.0f;


    void LateUpdate()
    {
        // Early out if we don't have a target
        if (!Target)
            return;

        // Calculate the current rotation angles
        var wantedRotationAngle = Target.eulerAngles.y;
        var wantedHeight = Target.position.y + Height;
        var currentRotationAngle = transform.eulerAngles.y;
        var currentHeight = transform.position.y;
        // Damp the rotation around the y-axis
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, RotationDamping * Time.deltaTime);
        // Damp the height
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, HeightDamping * Time.deltaTime);
        // Convert the angle into a rotation
        var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
        // Set the position of the camera on the x-z plane to:
        // distance meters behind the target
        transform.position = Target.position;
        transform.position -= currentRotation * Vector3.forward * Distance;
        // Set the height of the camera
        var pos = transform.position;

        pos.y = currentHeight;
        transform.position = pos;
        // Always look at the target
        transform.LookAt(Target);

    }
}
