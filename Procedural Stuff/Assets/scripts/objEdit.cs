using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;
using ProceduralNoiseProject;
using UnityEditor;
using System.IO;

namespace MarchingCubesProject
{

    //public enum MARCHING_MODE {  CUBES, TETRAHEDRON };

    public class objEdit : MonoBehaviour
    {
		

        public List<Material> m_materials;
		//public VoxelObject voxelObject;
        public MARCHING_MODE mode = MARCHING_MODE.CUBES;

        public int seed = 0;
		public Camera cam;

        List<GameObject> meshes = new List<GameObject>();
		public Voxel[] voxels = null;
		public int width = 32;
		public int height = 32;
		public int length = 32;
		public int scale = 1;
		Marching marching = null;
		List<Action> actions = new List<Action>();
		public TextAsset textAsset;
		/// <summary>
		/// Start is called on the frame when a script is enabled just before
		/// any of the Update methods is called the first time.
		/// </summary>
		void Start()
		{
			voxels = null;
			Generate();
		}
		
        void Generate()
        {

            //INoise perlin = new PerlinNoise(seed, 2.0f);
            //FractalNoise fractal = new FractalNoise(perlin, 3, 1.0f);

            //Set the mode used to create the mesh.
            //Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface.
            
            

            //The size of voxel array.
            if(mode == MARCHING_MODE.TETRAHEDRON)
                marching = new MarchingTertrahedron();
            else
                marching = new MarchingCubes();

            //Surface is the value that represents the surface of mesh
            //For example the perlin noise has a range of -1 to 1 so the mid point is where we want the surface to cut through.
            //The target value does not have to be the mid point it can be any value with in the range.
            marching.Surface = 0.0f;

			if(voxels == null){
				voxels = new Voxel[width * height * length];

				//Fill voxels with values. Im using perlin noise but any method to create voxels will work.
				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						for (int z = 0; z < length; z++)
						{
							float voxel = 0;
							float distance = Vector3.Distance(new Vector3(x+0.5f,y+0.5f,z+0.5f),new Vector3((float)width/2,(float)height/2,(float)length/2));
							voxel = -((0.7f/distance)*2-1);
							float fx = x;
							float fy = y;
							float fz = z;

							int idx = x + y * width + z * width * height;
							voxels[idx] = new Voxel(voxel, 0);
						}
					}
				}
			}
			
            

            List<Vector3> verts = new List<Vector3>();
            List<int> indices = new List<int>();
			float[] chunkVox = new float[voxels.Length];
			for(int i = 0; i< voxels.Length; i++){
				chunkVox[i] = voxels[i].value;
			}

            //The mesh produced is not optimal. There is one vert for each index.
            //Would need to weld vertices for better quality mesh.
            marching.Generate(chunkVox, width, height, length, verts, indices);

            //A mesh in unity can only be made up of 65000 verts.
            //Need to split the verts between multiple meshes.

            int maxVertsPerMesh = 60000; //must be divisible by 3, ie 3 verts == 1 triangle
            //int numMeshes = verts.Count / maxVertsPerMesh + 1;
			if(verts.Count == 0 || verts.Count > maxVertsPerMesh){
				return;
			}

            // for (int i = 0; i < numMeshes; i++)
            // {
			// 	print(numMeshes);
            //     List<Vector3> splitVerts = new List<Vector3>();
            //     List<int> splitIndices = new List<int>();

            //     for (int j = 0; j < maxVertsPerMesh; j++)
            //     {
            //         int idx = i * maxVertsPerMesh + j;

            //         if (idx < verts.Count)
            //         {
            //             splitVerts.Add(verts[idx]);
            //             splitIndices.Add(j);
            //         }
            //     }
				// List<int> splitIndices = new List<int>();
				List<List<int>> subIndices = new List<List<int>>();
				subIndices.Add(indices);
				for(int i = 0; i< verts.Count; i++){
					verts[i] *=scale;
				}

                

				Action createMesh = () =>{
					foreach(Transform child in transform){
						Destroy(child.gameObject);
					}
					GameObject go =meshGeneration.genMesh(verts,subIndices,m_materials,transform);
					// Mesh mesh = new Mesh();
					// mesh.SetVertices(verts);
					// mesh.SetTriangles(indices, 0);
					// mesh.RecalculateBounds();
					// mesh.RecalculateNormals();

					// GameObject go = new GameObject("Mesh");
					// go.transform.parent = transform;
					// go.AddComponent<MeshFilter>();
					// go.AddComponent<MeshRenderer>();
					// go.GetComponent<Renderer>().material = m_material;
					// go.GetComponent<MeshFilter>().mesh = mesh;
					// go.AddComponent<MeshCollider>();

					meshes.Add(go);
				};
				actions.Add(createMesh);
           // }

        }
		Thread t;

        void Update()
        {
			while(actions.Count > 0){
				Action func = actions[0];
				actions.RemoveAt(0);
				func();
			}
            //transform.Rotate(Vector3.up, 10.0f * Time.deltaTime);
			if((Input.GetButton("Fire1") || Input.GetButton("Fire2")) && (t == null || !t.IsAlive)){
				float multi = 1;
				if(Input.GetButton("Fire2"))
					multi = -1;
				RaycastHit hit;
        		Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width/2,Screen.height/2));
				
				if(Physics.Raycast(ray, out hit)){
					if(hit.transform.parent != null && hit.transform.parent.tag == "holder"){
						Vector3 pos = hit.point/scale;
						//pos = pos- hit.normal;
						int _x = Mathf.FloorToInt(pos.x);
						int _y = Mathf.FloorToInt(pos.y);
						int _z = Mathf.FloorToInt(pos.z);
						//int idx = x+ y*width + z*width*height;
						for(int x = _x-4; x<= _x+4; x++){
							for(int y = _y-4; y<= _y+4; y++){
								for(int z = _z-4; z<= _z+4; z++){
									int idx = x+ y*width + z*width*height;
									if(x>0 && y> 0 && z > 0 && x<width-1 && y < height-1 && z < length-1){
										float distancevox = Vector3.Distance(pos,new Vector3(x,y,z));
										voxels[idx] = new Voxel(Mathf.Clamp(voxels[idx].value-Mathf.Max(0.02f*(-0.05f*Mathf.Pow(distancevox,2)+1f),0)*multi,-1,1),0);
									}
								}
							}	
						}
					
						/*if(voxels.Length > idx && idx>= 0)
							voxels[idx] -= 1f;	*/			
							
						
						t = new Thread(Generate);
						t.Start();
					}
				}
			}
			if(Input.GetKeyDown("g")){
				//ScriptableObject.CreateInstance("VoxelObject");
				//t = new Thread(Generate);
				//t.Start();
				// voxelObject.voxels = voxels;
				// voxelObject.vSize = new Vector3Int(width, height, length);
				// EditorUtility.SetDirty(voxelObject);
				string text = JsonUtility.ToJson(new VoxelObj(new Vector3Int(width, height, length),voxels));
				File.WriteAllText(AssetDatabase.GetAssetPath(textAsset), text);
 				EditorUtility.SetDirty(textAsset);
				Debug.Log(text);
			}
			if(Input.GetKeyDown("h")){
				//ScriptableObject.CreateInstance("VoxelObject");
				VoxelObj vO = JsonUtility.FromJson<VoxelObj>(File.ReadAllText(AssetDatabase.GetAssetPath(textAsset)));
				voxels = vO.voxels;
				t = new Thread(Generate);
				t.Start();
				
			}
        }

    }

}