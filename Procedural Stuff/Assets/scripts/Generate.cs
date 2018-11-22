using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using MarchingCubesProject;
using ProceduralNoiseProject;

//namespace MarchingCubesProject
//{

    public enum MARCHING_MODE {  CUBES, TETRAHEDRON };

    public class Generate : MonoBehaviour
    {
        public Camera cam;
        public Transform Container;
        public Transform Player;
        public bool random;
        public List<Material> m_materials;
        

        public MARCHING_MODE mode = MARCHING_MODE.CUBES;

        public int seed = 0;
        public int chunkSize = 32;
        public float scale;
        [Range(-1,1)]
        public float surface = 0f;
        public int voxelsPerChunk = 20;
        float resolution = 1f;
        public int GenerateDistance=2;
        public int overlap = 1;
        public float delDistance = 60f;
        public Dictionary<Vector3Int,Voxel[]> voxels = new Dictionary<Vector3Int, Voxel[]>();


        //List<GameObject> meshes = new List<GameObject>();
        Dictionary<Vector3Int,GameObject> meshes = new Dictionary<Vector3Int, GameObject>();
        FractalNoise fractal;
        List<Vector3Int> generatedChunks = new List<Vector3Int>();
        Vector3Int lastPlayerChunk;
        List<Action> FunctionsToRunInMainThread = new List<Action>();
        List<Action> FunctionsToRunInMainThread2 = new List<Action>();
        
        
       // [Space(10)]
       // [Header("biomes stuff")]
        //public Biomes biomes;
        //public Color[] colors;
        [Space(10)]
        [Header("sculpt stuff")]
        public sculpting sculp;
        voxGeneration generation;
        
    
        void Start()
        {
            if(random){
                seed = UnityEngine.Random.Range(-1000000,1000000);
                
            }
            resolution = (float)voxelsPerChunk/chunkSize;
            //biomes.changeSeed(seed);
            //UnityEngine.Random.InitState(seed+1);
            //print(UnityEngine.Random.value > 0.5f);
            //INoise perlin = new PerlinNoise(seed, 2/scale/resolution);
            float freq =  2/scale/resolution;
            //fractal = new FractalNoise(perlin, 3, 1.0f);
            //fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
            cS = voxelsPerChunk+overlap;
            sculp = new sculpting(chunkSize,voxelsPerChunk,overlap,0.3f);
            generation = new voxGeneration(cS, resolution,seed, freq);

                
        }
        int cS; //chunkSize(in voxels) + overlap

        void GenerateTerrain(Vector3Int terrainOffset, bool scu)
        {
            Marching marching = null;
            if(mode == MARCHING_MODE.TETRAHEDRON)
                marching = new MarchingTertrahedron();
            else
                marching = new MarchingCubes();
            
            marching.Surface = surface;  
            
            
            //float s = (int)((float)scale*resolution);
            
            

            //Set the mode used to create the mesh.
            //Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface.
            
            //Surface is the value that represents the surface of mesh
            //For example the perlin noise has a range of -1 to 1 so the mid point is where we want the surface to cut through.
            //The target value does not have to be the mid point it can be any value with in the range.
            

            //The size of voxel array.
            Voxel[] chunkVoxels;
            if(!voxels.ContainsKey(terrainOffset)){
                
                chunkVoxels = generation.Voxels(/*,s*/terrainOffset);
                voxels.Add(terrainOffset,chunkVoxels);
            }
            else{
                chunkVoxels = voxels[terrainOffset];
            }
            float[] chunkvoxs = new float[chunkVoxels.Length];
            
            for (int i = 0; i < chunkVoxels.Length; i++)
            {
                chunkvoxs[i]=chunkVoxels[i].value;
                
            }
            

            List<Vector3> verts = new List<Vector3>();
            List<int> indices = new List<int>();

            //The mesh produced is not optimal. There is one vert for each index.
            //Would need to weld vertices for better quality mesh.
            marching.Generate(chunkvoxs, cS, cS, cS, verts, indices);
            //indices.Reverse();
            
            List<List<int>> subIndices=new List<List<int>>();
            for(int i = 0; i< m_materials.Count; i++){
                subIndices.Add(new List<int>());
            }
            for(int i = 0; i< indices.Count; i = i+3){
                //bool normal = true;
                Vector3 triPos = (verts[indices[i]]+verts[indices[i+1]]+verts[indices[i+2]])/3;
                Vector3Int voxeloV = Vector3Int.RoundToInt(triPos);
                int idx = voxeloV.x + voxeloV.y*cS + voxeloV.z*cS*cS;
                subIndices[chunkVoxels[idx].material].Add(indices[i]);
                subIndices[chunkVoxels[idx].material].Add(indices[i+1]);
                subIndices[chunkVoxels[idx].material].Add(indices[i+2]);
                
            }
            for(int i = 0; i< verts.Count; i++){
                verts[i]= verts[i]/resolution;
                
                //Indices.Add(i);
            }
            
            Action generateMesh;
            if (verts.Count == 0){
                generateMesh = () => {
                    if(meshes.ContainsKey(terrainOffset)){
                        Destroy(meshes[terrainOffset]);
                    }
                    
                };
            }
            else{
                generateMesh = () => {

                    GameObject go = meshGeneration.genMesh(verts,subIndices,m_materials,transform);
                    go.transform.localPosition = terrainOffset;
                    
                    if(meshes.ContainsKey(terrainOffset)){
                        Destroy(meshes[terrainOffset]);
                        meshes[terrainOffset]=go;
                        
                    }
                    else{
                        meshes.Add(terrainOffset,go);
                    }
                    
                };
            }
            
            if(scu){
                
                FunctionsToRunInMainThread2.Add(generateMesh);
            }
            else{
                FunctionsToRunInMainThread.Add(generateMesh);
            }
            
            

        }
        List<Vector3Int> currentChunks = new List<Vector3Int>();
        List<Vector3Int> oldChunks;
        void Update()
        {
            while(FunctionsToRunInMainThread.Count > 0){
                Action func = FunctionsToRunInMainThread[0];
                FunctionsToRunInMainThread.RemoveAt(0);
                if(func != null){
                    func();
                }
                
            }
            
            Vector3Int playerChunk = Chunk(Player.localPosition);
            
            if(oldChunks != null){
                for(int i = 0; i< oldChunks.Count; i++){
                    try{
                        generatedChunks.Remove(oldChunks[i]);
                    }
                    catch (Exception er) {
                        Debug.Log(er);
                    } 
                    
                    
                }
                oldChunks = null;
                
                for(int i = meshes.Count-1; i >= 0; i--){
                    Vector3Int me = meshes.Keys.ElementAt(i);
                    Vector3 Cpos = transform.TransformPoint(me);
                    if(Vector3.Distance(Cpos,Player.position) > delDistance){
                        GameObject m = meshes.Values.ElementAt(i);
                        meshes.Remove(me);
                        Destroy(m);
                        
                    }
                }
                
            }
            
            //offset = offset+Vector3.right;
            // if(Input.GetKeyDown("g")){
            //     //Regenerate();
            // }
            sculpt();
            if(lastPlayerChunk != null){
                if(lastPlayerChunk != playerChunk){
                    resetPos = true;
                    
                    //print("hello");
                    /*if(t != null){
                        Debug.Log("hello");
                        t.Abort();
                    }*/
                    Vector3 lPP = Player.localPosition;
                    Thread t = new Thread(() => GenerateAllChunks(lPP));
                    t.Start();
                    //GenerateAllChunks(lPP);
                    

                }
            }
            
            
            lastPlayerChunk = playerChunk;
            

        }
        Thread Sthread;
        void sculpt(){
            if(Cursor.lockState == CursorLockMode.None){
                return;
            }
            if((Input.GetButton("Fire1") || Input.GetButton("Fire2")) && (Sthread == null || !Sthread.IsAlive)){
				int multi = 1;
				if(Input.GetButton("Fire2"))
					multi = -1;
				RaycastHit hit;
        		Ray ray = new Ray(cam.transform.position, cam.transform.forward);
				
				if(Physics.Raycast(ray, out hit,30f)){
					if(hit.transform.parent == transform){
						Vector3 pos = transform.InverseTransformPoint(hit.point)-hit.normal*multi;
                        Vector3 playerPos = Player.localPosition;

                        MeshCollider collider = hit.collider as MeshCollider;
                        // Remember to handle case where collider is null because you hit a non-mesh primitive...

                        Mesh mesh = collider.sharedMesh;
                        int limit = hit.triangleIndex * 3;
                        int submesh = 0;
                        if(multi == -1){
                            for(submesh = 0; submesh < mesh.subMeshCount; submesh++)
                            {
                                int numIndices = mesh.GetTriangles(submesh).Length;
                                if(numIndices > limit)
                                    break;

                                limit -= numIndices; 
                            }
                            Material material = collider.GetComponent<MeshRenderer>().sharedMaterials[submesh];
                            submesh = m_materials.IndexOf(material);
                        }
                        
                        Sthread = new Thread(() => GTL(sculp.sculpt(ref voxels,multi,pos,playerPos,submesh)));
                        
                        Sthread.Start();	
                              
					}
				}
			}
        }

        public void GTL(List<Vector3Int> chunks){
            for(int i = 0; i< chunks.Count; i++){
                GenerateTerrain(chunks[i],true);

            }
            List<Action> newActions = new List<Action>();
            newActions = FunctionsToRunInMainThread2;
            FunctionsToRunInMainThread2 = new List<Action>();
            Action action = () => {
                while(newActions.Count > 0){
                    Action func = newActions[0];
                    newActions.RemoveAt(0);
                    func();
                    
                }
            };
            FunctionsToRunInMainThread.Add(action);
            
        }
        
        bool resetPos = false;
        void FixedUpdate()
        {
            if(resetPos){
                Container.position = Player.localPosition * -1;
            }
            
        }
        //chunk: x=1 y=1 z=1
        public Vector3Int Chunk(Vector3 pos){
            return new Vector3Int(Mathf.FloorToInt(pos.x/(chunkSize/*-overlap/*2*/)),Mathf.FloorToInt(pos.y/(chunkSize/*-overlap/*2*/)),Mathf.FloorToInt(pos.z/(chunkSize/*-overlap/*2*/)));
        }
        
        void GenerateAllChunks(Vector3 PlayerPos){
            Vector3 playerChunk  = Chunk(PlayerPos);
            for(int x = (int)playerChunk.x-GenerateDistance;x<= (int)playerChunk.x+GenerateDistance;x++){
                for(int y = (int)playerChunk.y-GenerateDistance;y<= (int)playerChunk.y+GenerateDistance;y++){
                    for(int z = (int)playerChunk.z-GenerateDistance;z<= (int)playerChunk.z+GenerateDistance;z++){
                        currentChunks.Add(new Vector3Int(x,y,z));
                        if(!generatedChunks.Contains(new Vector3Int(x,y,z))){
                            generatedChunks.Add(new Vector3Int(x,y,z));
                            GenerateTerrain(new Vector3Int(x*(chunkSize/*-overlap/*2*/),y*(chunkSize/*-overlap/*2*/),z*(chunkSize/*-overlap/*2*/)),false);
                            
                            //Thread t = new Thread(() => GenerateTerrain(new Vector3(x*(chunkSize-overlap*2),y*(chunkSize-overlap*2),z*(chunkSize-overlap*2))));
                            //t.Start();
                            //lastPlayerChunk = playerChunk;
                        }
                        
                    }
                }
            }
            oldChunks = generatedChunks.Except(currentChunks).ToList();
            for(int i = oldChunks.Count-1;i>= 0; i--){
                Vector3 pos = new Vector3(oldChunks[i].x*(chunkSize/*-overlap/*2*/),oldChunks[i].y*(chunkSize/*-overlap/*2*/),oldChunks[i].z*(chunkSize/*-overlap/*2*/));
                if(Vector3.Distance(PlayerPos,pos)<delDistance){
                    oldChunks.Remove(oldChunks[i]);
                }
            }
            currentChunks = new List<Vector3Int>();
        }
        public void Regenerate(){
            foreach(Transform child in transform){

                    Destroy(child.gameObject);
            }
            meshes= new Dictionary<Vector3Int, GameObject>();
            voxels = new Dictionary<Vector3Int, Voxel[]>();
            if(random){
                seed = UnityEngine.Random.Range(-1000000,1000000);
                INoise perlin = new PerlinNoise(seed, 2/scale/resolution);
                //fractal = new FractalNoise(perlin, 3, 1.0f);
                fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
                print("hello");
            }
            generatedChunks = new List<Vector3Int>();
            GenerateAllChunks(Player.localPosition);
        }
        #region changeStuff
        public void Changeseed(string _seed){
            random = false;
            if(_seed == ""){
                random = true;
                seed = UnityEngine.Random.Range(-10000000,10000000);
                return;
            }
            seed = int.Parse(_seed);
            INoise perlin = new PerlinNoise(seed, 2/scale/resolution);
            //fractal = new FractalNoise(perlin, 3, 1.0f);
            fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
            //biomes.seed = seed;
        }
        public void changeScale(float newScale){
            scale = newScale;
            INoise perlin = new PerlinNoise(seed, 2/scale/resolution);
            //fractal = new FractalNoise(perlin, 3, 1.0f);
            fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
        }
        public void changeResolution(float newResolution){
            voxelsPerChunk= (int)newResolution;
            resolution = (float)voxelsPerChunk/chunkSize;
            cS = voxelsPerChunk+overlap;
            sculp = new sculpting(chunkSize,voxelsPerChunk,overlap,0.4f);
        }
        #endregion
        [Space(10)]
        [Header("fractal stuff")]
        public int fractalOctaves = 2;
        public float fractalfrequency = 1f;
        public float fractalamplitude = 1f;


    }
    // public struct Voxel{
    //     public float value;
    //     public int material;
    //     public Voxel(float value, int material){
    //         this.value = value;
    //         this.material = material;
    //     }
    // }
    

///}
