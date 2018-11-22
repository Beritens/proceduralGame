using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarchingCubesProject;
using System;
using System.IO;

public class spawnObject : MonoBehaviour {

	/* public*/ List<VoxelObj> objects = new List<VoxelObj>();
	List<Action> actions = new List<Action>();
	public List<Material> materials = new List<Material>();
	public Camera cam;
	Dictionary<Vector3Int,List<VoxelObj>> objDict = new Dictionary<Vector3Int, List<VoxelObj>>();
	Dictionary<Vector3Int,List<GameObject>> meshes = new Dictionary<Vector3Int, List<GameObject>>();
	public TextAsset[] objs;
	
	//index < 0: new object
	/// <summary>
	/// Awake is called when the script instance is being loaded.
	/// </summary>
	void Awake()
	{
		for(int i = 0; i< objs.Length; i++){
			VoxelObj VO=  JsonUtility.FromJson<VoxelObj>(objs[i].text);
			objects.Add(VO);
		}
		
	}
	public void spawn(Vector3 position, Vector3Int chunk, int index){
		VoxelObj obj;
		if(index >= 0){
			
			obj = objDict[chunk][index];
		}
		else{
			VoxelObj temp = objects[UnityEngine.Random.Range(0,objects.Count)];
			//Debug.Log(UnityEngine.Random.Range(0,objects.Count));
			obj = new VoxelObj(temp.vSize,(Voxel[])temp.voxels.Clone());
			//obj.voxels = temp.voxels;
			//obj.vSize = temp.vSize;
		}
		
		Marching marching = new MarchingCubes();
		marching.Surface = 0;
		float[] vox = new float[obj.voxels.Length];
		for(int i = 0; i< obj.voxels.Length; i++){
			vox[i] = obj.voxels[i].value;
		}
		int ind;
		if(objDict.ContainsKey(chunk)){
			if(objDict[chunk].Count > index && index >= 0){
				objDict[chunk][index] = obj;
				ind = index;
			}
			else{
				objDict[chunk].Add(obj);
				ind = objDict[chunk].Count-1;
			}
			
		}
		else{
			objDict.Add(chunk, new List<VoxelObj>());
			objDict[chunk].Add(obj);
			ind = objDict[chunk].Count-1;
		}
		
		List<Vector3> verts = new List<Vector3>();
		List<int> indices = new List<int>();
		marching.Generate(vox,obj.vSize.x,obj.vSize.y,obj.vSize.z,verts,indices);
		//Debug.Log(obj.voxels.Length);
		List<List<int>> subIndices = new List<List<int>>(){indices};
		Action action = () => {
			GameObject g = meshGeneration.genMesh(verts,subIndices, materials,transform);
			objectInfo oI = g.AddComponent<objectInfo>();
			oI.index = ind;
			oI.chunk = chunk;
			
			g.transform.localPosition = position;
			if(!meshes.ContainsKey(chunk)){
				meshes.Add(chunk,new List<GameObject>());
			}
			if(meshes[chunk].Count > ind){
				Destroy(meshes[chunk][ind]);
				meshes[chunk][ind] = g;
			}
			else{
				meshes[chunk].Add(g);
			}
			//Debug.Log("hello");

		};
		actions.Add(action);
	}
	/// <summary>
	/// Update is called every frame, if the MonoBehaviour is enabled.
	/// </summary>
	float t;
	void Update()
	{
		while(actions.Count > 0){
			Action a =actions[0];
			actions.Remove(a);
			a();
		}
		sculptObjects();
		if(Input.GetKeyDown("k")){
			spawn(Vector3.up*Time.time, Vector3Int.zero, -1);
		}
		
	}
	/// <summary>
	/// Start is called on the frame when a script is enabled just before
	/// any of the Update methods is called the first time.
	/// </summary>
	void Start()
	{
		spawn(Vector3.zero, Vector3Int.zero, -1);
		spawn(Vector3.up*5, Vector3Int.zero, -1);
	}
	void sculptObjects(){
            if(Cursor.lockState == CursorLockMode.None){
                return;
            }
            if((Input.GetButton("Fire1"))/*  && (Sthread == null || !Sthread.IsAlive)*/){
				int multi = 1;
				RaycastHit hit;
        		Ray ray = new Ray(cam.transform.position,cam.transform.forward);//cam.ScreenPointToRay(new Vector2(Screen.width/2,Screen.height/2));
				
				if(Physics.Raycast(ray, out hit,30f)){
					objectInfo objInfo = hit.transform.GetComponent<objectInfo>();
					if(objInfo != null && objInfo.enabled){
						Vector3 pos = hit.transform.InverseTransformPoint(hit.point)-hit.normal*multi;
                        //Vector3 playerPos = Player.localPosition;

                        MeshCollider collider = hit.collider as MeshCollider;
                        // Remember to handle case where collider is null because you hit a non-mesh primitive...

                        Mesh mesh = collider.sharedMesh;
                        int limit = hit.triangleIndex * 3;
						
                        //int submesh = 0;
                        // if(multi == -1){
                        //     for(submesh = 0; submesh < mesh.subMeshCount; submesh++)
                        //     {
                        //         int numIndices = mesh.GetTriangles(submesh).Length;
                        //         if(numIndices > limit)
                        //             break;

                        //         limit -= numIndices; 
                        //     }
                        //     Material material = collider.GetComponent<MeshRenderer>().sharedMaterials[submesh];
                        //     submesh = m_materials.IndexOf(material);
                        // }
						//Debug.Log(hit.transform.InverseTransformPoint(pos));
						VoxelObj vObj = objDict[objInfo.chunk][objInfo.index];
						Voxel[] v = vObj.voxels;
                        sculptingObj.sculpt(ref v,pos,1,vObj.vSize);
						objDict[objInfo.chunk][objInfo.index] = new VoxelObj(vObj.vSize,v);
						spawn(hit.transform.localPosition, objInfo.chunk, objInfo.index);
						objInfo.enabled = false;
						// if(objDict[objInfo.chunk][objInfo.index].voxels == objects[0].voxels){
						// 	Debug.Log("fail");
						// }
                        /* Sthread = new Thread(() => GTL(sculp.sculpt(ref voxels,multi,pos,playerPos,submesh)));
                        
                        Sthread.Start();*/	
                              
					}
				}
			}
        }
}
