using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class simpleMove : MonoBehaviour {
	public Transform rot;
	public float speed= 1f;

	
	// Update is called once per frame
	void Update () {
		if(Input.GetAxis("Horizontal")!= 0 || Input.GetAxis("Vertical") != 0){
			float f = Input.GetAxis("Vertical");
			float s = Input.GetAxis("Horizontal");
			transform.position += (rot.forward*f*speed + rot.right*s*speed);
		}
	}
}
