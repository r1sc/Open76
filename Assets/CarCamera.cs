using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarCamera : MonoBehaviour
{
    public Transform ObjectToFollow;
    public Vector3 RelativePosition;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
	{
	    transform.position = ObjectToFollow.TransformPoint(RelativePosition);
        transform.LookAt(ObjectToFollow, Vector3.up);
	}
}
