using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour {

	private float horizontalInput;
	private float verticalInput;
	private float steeringAngle;
	public WheelCollider frontDriverW, frontPassangerW;
	public WheelCollider rearDriverW, rearPassangerW;
	public Transform frontDriverT, frontPassangerT;
	public Transform rearDriverT, rearPassangerT;
	public float maxSteeringAngle = 30f;
	public float motorForce = 50f;

	public void GetImput(){
		horizontalInput = Input.GetAxis("Horizontal");
		verticalInput = Input.GetAxis("Vertical");
	}
	private void Steer(){
		steeringAngle = maxSteeringAngle * horizontalInput;
		frontDriverW.steerAngle = steeringAngle;
		frontPassangerW.steerAngle = steeringAngle;
	}
	private void Accalerate(){
		frontDriverW.motorTorque = motorForce* verticalInput;
		frontPassangerW.motorTorque = motorForce* verticalInput;
		if(verticalInput == 0){
			
		}
	}
	private void UpdateWheelPoses(){
		UpdateWheelPose(frontDriverW,frontDriverT);
		UpdateWheelPose(rearDriverW,rearDriverT);
		UpdateWheelPose(frontPassangerW,frontPassangerT);
		UpdateWheelPose(rearPassangerW,rearPassangerT);
	}
	private void UpdateWheelPose(WheelCollider _collider, Transform _transform){
		Vector3 pos = _transform.position;
		Quaternion quat = _transform.rotation;
		_collider.GetWorldPose(out pos, out quat);
		_transform.position = pos;
		_transform.rotation = quat;
	}
	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate()
	{
		GetImput();
		Steer();
		Accalerate();
		UpdateWheelPoses();
	}
}
