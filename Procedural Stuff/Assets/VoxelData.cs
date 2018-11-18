using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Voxel{
	public float value;
	public int material;
	public Voxel(float value, int material){
		this.value = value;
		this.material = material;
	}
}