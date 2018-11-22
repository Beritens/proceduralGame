using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*[CreateAssetMenu]
public class VoxelObject  : ScriptableObject {
	public Vector3Int vSize;
	public Voxel[] voxels;
	public VoxelObject(Vector3Int vSize, Voxel[] voxels){
		this.vSize = vSize;
		this.voxels = voxels;
	}
	
}*/
public struct VoxelObj{
	public Vector3Int vSize;
	public Voxel[] voxels;
	public VoxelObj(Vector3Int vSize, Voxel[] voxels){
		this.vSize = vSize;
		this.voxels = voxels;
	}
	
}