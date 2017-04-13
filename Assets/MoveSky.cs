using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSky : MonoBehaviour
{
    public Vector2 Speed;
    public float Height;
    private Material _material;

    // Use this for initialization
	void Start ()
	{
	    _material = GetComponent<MeshRenderer>().sharedMaterial;
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
	    _material.mainTextureOffset += Speed*Time.deltaTime;
	    transform.position = Camera.main.transform.position + Vector3.up* Height;
	}
}
