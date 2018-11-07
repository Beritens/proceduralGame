using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class changeFogColor : MonoBehaviour {

	Camera cam;
	public Transform Player;
	public Gradient gradient;
	public float startHeight = 0f;
	public float endHeight =0f;


	// Use this for initialization
	void Start () {
		cam = GetComponent<Camera>();

	}
	
	// Update is called once per frame
	void Update () {
		float height = Player.transform.localPosition.y;
		if(height< startHeight && height > endHeight){
			float a = (startHeight-height)/(startHeight-endHeight);
			Color col = gradient.Evaluate(a);
			cam.backgroundColor= col;
			RenderSettings.fogColor = col;
		}
	}
}
