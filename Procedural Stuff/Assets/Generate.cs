using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;

using ProceduralNoiseProject;

namespace MarchingCubesProject
{

    public enum MARCHING_MODE {  CUBES, TETRAHEDRON };

    public class Generate : MonoBehaviour
    {
        public Camera cam;
        public Transform Container;
        public Transform Player;
        public bool random;
        public Material m_material;
        

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
        Dictionary<Vector3Int,float[]> voxels = new Dictionary<Vector3Int, float[]>();


        //List<GameObject> meshes = new List<GameObject>();
        Dictionary<Vector3Int,GameObject> meshes = new Dictionary<Vector3Int, GameObject>();
        FractalNoise fractal;
        List<Vector3Int> generatedChunks = new List<Vector3Int>();
        Vector3Int lastPlayerChunk;
        List<Action> FunctionsToRunInMainThread = new List<Action>();
        
        
        [Space(10)]
        [Header("ore stuff")]
        public GameObject ore;
        public float probability = 1f;
       // [Space(10)]
       // [Header("biomes stuff")]
        //public Biomes biomes;
        //public Color[] colors;
        [Space(10)]
        [Header("sculpt stuff")]
        bool cansculpt = true;
        
    
        void Start()
        {
            if(random){
                seed = UnityEngine.Random.Range(-1000000,1000000);
                
            }
            resolution = (float)voxelsPerChunk/chunkSize;
            //biomes.changeSeed(seed);
            //UnityEngine.Random.InitState(seed+1);
            //print(UnityEngine.Random.value > 0.5f);
            INoise perlin = new PerlinNoise(seed, 2/scale/resolution);
            //fractal = new FractalNoise(perlin, 3, 1.0f);
            fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
            cS = (int)((float)chunkSize*resolution)+overlap;
            
            

                
        }
        int cS;

        void GenerateTerrain(Vector3Int terrainOffset)
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
            float[] chunkVoxels;
            if(!voxels.ContainsKey(terrainOffset)){
                
                chunkVoxels = Voxels(cS/*,s*/,terrainOffset);
                voxels.Add(terrainOffset,chunkVoxels);
            }
            else{
                chunkVoxels = voxels[terrainOffset];
            }
            

            

            List<Vector3> verts = new List<Vector3>();
            List<int> indices = new List<int>();



            //The mesh produced is not optimal. There is one vert for each index.
            //Would need to weld vertices for better quality mesh.
            marching.Generate(chunkVoxels, cS, cS, cS, verts, indices);
            /*if(resolution != 1){
                for(int i = 0; i< verts.Count; i++){
                    verts[i] /= resolution;
                }
            }*/
            //A mesh in unity can only be made up of 65000 verts.
            //Need to split the verts between multiple meshes.

            int maxVertsPerMesh = 30000; //must be divisible by 3, ie 3 verts == 1 triangle
            int numMeshes = verts.Count / maxVertsPerMesh + 1;

            for (int i = 0; i < numMeshes; i++)
            {

                List<Vector3> splitVerts = new List<Vector3>();
                List<int> splitIndices = new List<int>();
                List<Vector3> ores = new List<Vector3>();
                for (int j = 0; j < maxVertsPerMesh; j++)
                {
                    int idx = i * maxVertsPerMesh + j;

                    if (idx < verts.Count)
                    {

                        splitVerts.Add(verts[idx]/resolution);
                        splitIndices.Add(j);
                    }
                }
                
                if (splitVerts.Count == 0) continue;
                Action generateMesh = () => {
                    /*UnityEngine.Random.InitState((int)(terrainOffset.x+terrainOffset.y+terrainOffset.z+seed));
                    if(UnityEngine.Random.value <= probability && ore != null){
                        int verty = UnityEngine.Random.Range(0,splitVerts.Count-1);
                        //Debug.Log(splitVerts[verty]+terrainOffset);
                        Vector3 orePos = splitVerts[verty]+terrainOffset+Container.position;
                        GameObject.Instantiate(ore,orePos,Quaternion.identity,Container);
                    }*/
                    Mesh mesh = new Mesh();
                    mesh.SetVertices(splitVerts);
                    mesh.SetTriangles(splitIndices, 0);
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    GameObject go = new GameObject("Mesh");
                    go.transform.parent = transform;
                    go.AddComponent<MeshFilter>();
                    MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    go.GetComponent<Renderer>().material = m_material;
                    go.GetComponent<MeshFilter>().mesh = mesh;
                    //if(splitVerts.Count > 30)
                        go.AddComponent<MeshCollider>();
                        
                        
                    go.transform.localPosition = new Vector3(/*-w / 2 / resolution+*/terrainOffset.x, /*-h / 2 / resolution+*/terrainOffset.y,/* -l / 2 / resolution+*/terrainOffset.z);
                    if(meshes.ContainsKey(terrainOffset)){
                        Destroy(meshes[terrainOffset]);
                        meshes[terrainOffset]=go;
                        cansculpt = true;
                    }
                    else{
                        meshes.Add(terrainOffset,go);
                    }
                    
                };
                
                QueueMainThreadFunction(generateMesh);
            }

        }

        
        float[] Voxels(int _chunkSize/*, float _scale*/, Vector3Int _offset){
            //The size of voxel array.
            Vector3 v = _offset;
            Vector3 newOffset = v*resolution;
            float[] voxels = new float[_chunkSize * _chunkSize * _chunkSize];

            //Fill voxels with values. Im using perlin noise but any method to create voxels will work.
            for (int x = 0; x < _chunkSize; x++)
            {
                for (int y = 0; y < _chunkSize; y++)
                {
                    for (int z = 0; z < _chunkSize; z++)
                    {

                        int idx = x + y * _chunkSize + z * _chunkSize * _chunkSize;

                        voxels[idx] = Voxel(x,y,z,_offset,newOffset);

                        
                        //bedrock
                       /* else if(y + newOffset.y < -15){
                            
                            //Debug.Log("hi");
                            voxels[idx] = Mathf.Clamp(voxels[idx]-(y + newOffset.y +15)*0.05f,-1,1);
                            //voxels[idx] = 1;
                        }*/
                        /*if(x== 0 || y == 0 || z== 0 || x == _chunkSize-1 || y== _chunkSize-1 || z == _chunkSize -1){
                            
                            voxels[idx] = surface;
                        }*/
                        //voxels[idx]= Mathf.Clamp(voxels[idx]-(0.5f*(float)y/_chunkSize),-1,1);
                        
                        
                    }
                }
            }
            
            return voxels;
        }
        float Voxel(int x, int y, int z, Vector3Int offset, Vector3 newOffset){
            Vector3 scaly = Vector3.one;
            //different scale
            /*if(y + Offset.y < -50){
                //Debug.Log((y + _offset.y+ 50)*0.1f);
                scaly = scaly+(-(y + newOffset.y) - 50)*0.001f;
            }*/
            
            float fx = (x+newOffset.x) / (2f * scaly.x);
            float fy = (y+newOffset.y) / (2f * scaly.y);
            float fz = (z+newOffset.z) / (2f * scaly.z);

            float vox = fractal.Sample3D(fx, fy, fz);
            /*if(biomes.GetBiomData(new Vector3(x,y,z)/resolution+offset).biom[0] != 4){
                vox = Mathf.Clamp(vox+0.5f,-1,1);
            }*/

            //vox= Mathf.Clamp(vox+0.5f,-1,1); //less caves
            if(y/resolution + offset.y > 5){
                //voxels[idx] = Mathf.Clamp(voxels[idx]-(y +newOffset.y -5)*0.1f,-1,1);
                //float iks = y +newOffset.y -5;
                //voxels[idx] = Mathf.Clamp(voxels[idx]-(0.1f*Mathf.Pow(2,iks)),-1,1);
                float iks = y/resolution + offset.y -5;
                vox = Mathf.Clamp(vox-(Mathf.Pow(iks,2)*0.001f),-1,1);
                //voxels[idx] = 1;
             }
            return vox;
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
            if(Input.GetKeyDown("g")){
                Regenerate();
            }
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
                    

                }
            }
            sculpt();
            
            lastPlayerChunk = playerChunk;
            

        }
        void sculpt(){
            if(Cursor.lockState == CursorLockMode.None){
                return;
            }
            if((Input.GetButton("Fire1") || Input.GetButton("Fire2")) && cansculpt){
				float multi = 1;
				if(Input.GetButton("Fire2"))
					multi = -1;
				RaycastHit hit;
        		Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width/2,Screen.height/2));
				
				if(Physics.Raycast(ray, out hit)){
					if(hit.transform.parent.tag == "holder"){
						Vector3 pos = transform.InverseTransformPoint(hit.point)-hit.normal*multi;
                        Vector3Int chunk= Chunk(pos)*(chunkSize/*-overlap/*2*/);
                        List<Vector3Int> chunks = new List<Vector3Int>();
                        chunks.Add(chunk);
                        //Debug.Log(chunk);
						//pos = pos- hit.normal;
						int _x = Mathf.FloorToInt((pos.x-chunk.x)*resolution);
                        
						int _y = Mathf.FloorToInt((pos.y-chunk.y)*resolution);
						int _z = Mathf.FloorToInt((pos.z-chunk.z)*resolution);
						//int idx = x+ y*chunkSize + z*chunkSize*chunkSize;
						for(int x = _x-2; x<= _x+2; x++){
							for(int y = _y-2; y<= _y+2; y++){
								for(int z = _z-2; z<= _z+2; z++){
									
                                    float distancevox = Vector3.Distance(pos-chunk,new Vector3(x,y,z)/resolution);
                                    float change = Mathf.Max(0.02f*(-0.1f*Mathf.Pow(distancevox,2)+1f),0)*multi;
                                    
									if(!(x<0 || y<0 || z<0 || x>=voxelsPerChunk || y>=voxelsPerChunk || z>=voxelsPerChunk)){
                                        int idx = x+ y*cS + z*cS*cS;
										float vox = Mathf.Clamp(voxels[chunk][idx]-change,-1,1);
                                        //Debug.Log(Mathf.Max(0.05f*(-0.1f*Mathf.Pow(distancevox,2)+1f)+100f,0));
										voxels[chunk][idx] = vox;
                                        bool ox = x< overlap;
                                        bool oy = y< overlap;
                                        bool oz = z< overlap;
                                        if(ox || oy || oz){
                                            List<Vector3Int> thisChunks = new List<Vector3Int>();
                                            if(ox){
                                                thisChunks.Add(chunk - Vector3Int.right* (chunkSize));
                                                if(oy){
                                                    thisChunks.Add(chunk - new Vector3Int(1,1,0)* (chunkSize));
                                                    if(oz){
                                                        thisChunks.Add(chunk - new Vector3Int(1,1,1)* (chunkSize));
                                                    }   
                                                }  
                                                if(oz){
                                                    thisChunks.Add(chunk - new Vector3Int(1,0,1)* (chunkSize));
                                                }    
                                            }
                                             if(oy){
                                                thisChunks.Add(chunk - Vector3Int.up* (chunkSize));
                                                if(oz){
                                                    thisChunks.Add(chunk - new Vector3Int(0,1,1)* (chunkSize));
                                                }
                                            }
                                             if(oz){
                                                thisChunks.Add(chunk - new Vector3Int(0,0,1)* (chunkSize));
                                            }
                                            
                                            for(int i = 0; i< thisChunks.Count;i++){
                                                SchangeVoxels(change,chunk,thisChunks[i],x,y,z);
                                                if(!chunks.Contains(thisChunks[i])){
                                                    chunks.Add(thisChunks[i]);
                                                }
                                            }
                                            

                                        }
									}
                                    else{
                                        List<Vector3Int> thisChunks = getChunksfromVoxel(x,y,z,chunk);
                                        for(int i = 0; i< thisChunks.Count;i++){
                                            SchangeVoxels(change,chunk,thisChunks[i],x,y,z);
                                            if(!chunks.Contains(thisChunks[i])){
                                                chunks.Add(thisChunks[i]);
                                            }
                                        }
                                    }
								}
							}	
						}
					
						/*if(voxels.chunkSize > idx && idx>= 0)
							voxels[idx] -= 1f;	*/			
							
						
						Thread t = new Thread(() => GTL(chunks));
						t.Start();
						cansculpt = false;
					}
				}
			}
        }

        void GTL(List<Vector3Int> chunks){
            for(int i = 0; i< chunks.Count; i++){
                GenerateTerrain(chunks[i]);
            }
        }
        List<Vector3Int> getChunksfromVoxel(int x, int y, int z, Vector3Int chunk){
            List<Vector3Int> chunks = new List<Vector3Int>();
            Vector3Int chunky = new Vector3Int(Mathf.FloorToInt((chunk.x+(float)x/resolution)/chunkSize+0.001f)*chunkSize,Mathf.FloorToInt((chunk.y+(float)y/resolution)/chunkSize+0.001f)*chunkSize,Mathf.FloorToInt((chunk.z+(float)z/resolution)/chunkSize+0.001f)*chunkSize);
            chunks.Add(chunky);
            Vector3Int dif = chunk-chunky;
            bool ox= (x+(int)(dif.x*resolution))<overlap;
            bool oy= (y+(int)(dif.y*resolution))<overlap;
            bool oz= (z+(int)(dif.z*resolution))<overlap;
            //Debug.Log(x + " " + y + " "+z);
            // if(x== 16){
            //     //print(chunky + " "+ chunk.x+" "+ x+" "+chunkSize+" "+(-20f+16f/0.8f)/20f+ " "+ (chunk.x+(float)x/resolution)/chunkSize);
            //     //print("why?  "+ (-20f+16f/0.8f)/20f + " -20f+16f/0.8f= " + (-20f+16f/0.8f));
            // }
            if(ox){
                chunks.Add(chunky-Vector3Int.right*chunkSize);
                
                if(oy){
                    chunks.Add(chunky-new Vector3Int(1,1,0)*chunkSize);
                    if(oz){
                        chunks.Add(chunky-new Vector3Int(1,1,1)*chunkSize);
                    }
                }
                if(oz){
                    chunks.Add(chunky-new Vector3Int(1,0,1)*chunkSize);
                }
            }
            if(oy){
                chunks.Add(chunky-Vector3Int.up*chunkSize);
                if(oz){
                    chunks.Add(chunky-new Vector3Int(0,1,1)*chunkSize);
                }
            }
            if(oz){
                chunks.Add(chunky-new Vector3Int(0,0,1)*chunkSize);
            }
            

            return chunks;
        }
        void SchangeVoxels(float change, Vector3Int chunk, Vector3Int thisChunk, int x, int y, int z){
            if(voxels.ContainsKey(thisChunk)){
                Vector3Int dif = chunk-thisChunk;
                int newidx = (x+(int)(dif.x*resolution))+ (y+(int)(dif.y*resolution))*cS + (z+(int)(dif.z*resolution))*cS*cS;
                //Debug.Log(x+(int)(dif.x*resolution) + " " + (z+(int)(dif.z*resolution)));
                // if(x+(int)(dif.x*resolution) == x)
                //     Debug.Log(x+(int)(dif.x*resolution) + " " +y+(int)(dif.y*resolution)+ " " + (z+(int)(dif.z*resolution)));
                voxels[thisChunk][newidx]=  Mathf.Clamp(voxels[thisChunk][newidx]-change,-1,1);
                
            }  
        }
        bool resetPos = false;
        void FixedUpdate()
        {
            if(resetPos){
                Container.position = Player.localPosition * -1;
            }
        }
        //chunk: x=1 y=1 z=1
        Vector3Int Chunk(Vector3 pos){
            return new Vector3Int(Mathf.FloorToInt(pos.x/(chunkSize/*-overlap/*2*/)),Mathf.FloorToInt(pos.y/(chunkSize/*-overlap/*2*/)),Mathf.FloorToInt(pos.z/(chunkSize/*-overlap/*2*/)));
        }
        public void QueueMainThreadFunction(Action someFunc){
            FunctionsToRunInMainThread.Add(someFunc);
        }
        void GenerateAllChunks(Vector3 PlayerPos){
            Vector3 playerChunk  = Chunk(PlayerPos);
            for(int x = (int)playerChunk.x-GenerateDistance;x<= (int)playerChunk.x+GenerateDistance;x++){
                for(int y = (int)playerChunk.y-GenerateDistance;y<= (int)playerChunk.y+GenerateDistance;y++){
                    for(int z = (int)playerChunk.z-GenerateDistance;z<= (int)playerChunk.z+GenerateDistance;z++){
                        currentChunks.Add(new Vector3Int(x,y,z));
                        if(!generatedChunks.Contains(new Vector3Int(x,y,z))){
                            generatedChunks.Add(new Vector3Int(x,y,z));
                            GenerateTerrain(new Vector3Int(x*(chunkSize/*-overlap/*2*/),y*(chunkSize/*-overlap/*2*/),z*(chunkSize/*-overlap/*2*/)));
                            
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
            voxels = new Dictionary<Vector3Int, float[]>();
            if(random){
                seed = UnityEngine.Random.Range(-1000000,1000000);
                INoise perlin = new PerlinNoise(seed, 2/scale/resolution);
                //fractal = new FractalNoise(perlin, 3, 1.0f);
                fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
                print("hello");
            }
            //GenerateTerrain(new Vector3(0,0,0));
            //GenerateTerrain(new Vector3(15,0,0));
            //Vector3Int playerChunk = Chunk(Player.localPosition);
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
            cS = (int)((float)chunkSize*resolution)+overlap;
        }
        #endregion
        [Space(10)]
        [Header("fractal stuff")]
        public int fractalOctaves = 2;
        public float fractalfrequency = 1f;
        public float fractalamplitude = 1f;
        /*public void changeSize(string newSize){
            int newSizeInt = int.Parse(newSize);
            int newSizeIntPO = newSizeInt+overlap*2;
            chunkSize = newSizeIntPO;
            chunkSize = newSizeIntPO;
            chunkSize = newSizeIntPO;
            delDistance = (float)newSizeInt*GenerateDistance*2.25f;
        }*/
        


    }
    

}
