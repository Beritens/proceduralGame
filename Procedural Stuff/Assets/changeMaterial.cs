using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class changeMaterial : MonoBehaviour {
	public Color color1; 
	public Color color2;
	public Transform target; 
	public Material material;
	public bool random;
	/// <summary>
	/// Start is called on the frame when a script is enabled just before
	/// any of the Update methods is called the first time.
	/// </summary>
	void Start()
	{
		if(random){
			color1 = Random.ColorHSV();
			color2 = Random.ColorHSV();
		}
		
	}
	
	// Update is called once per frame
	void Update () {
		if(target.position.x> -10 && target.position.x < 10){
			float lerp = (target.localPosition.x+10)/20f;
			Debug.Log(target.localPosition.x);
			material.color = Color.Lerp(color1,color2,lerp);
		}
		// else{
		// 	material.color = color2;
		// }
	}
}
