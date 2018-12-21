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
        //public List<Material> m_materials;
        public materials materials;
        

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
        public float sculptStrength = 1f;
        public materialUI materialUI;
        sculpting sculp;
        voxGeneration generation;
        // [Space(10)]
        // [Header("object stuff")]
        // public float spawnProbability;
        // public spawnObject spawnObject;
        [Space(10)]
        [Header("ores")]
        public ore[] oreTemplates;
        List<orePoint> ores = new List<orePoint>();
        List<orePoint> activeOres = new List<orePoint>();
        
    
        void Start()
        {
            for(int i = 0; i< 10; i++){
                
                ore oreTemp = oreTemplates[UnityEngine.Random.Range(0,oreTemplates.Length)];
                
                orePoint oreP = new orePoint(oreTemp.material,oreTemp.radius,new Vector3(UnityEngine.Random.Range(-40,40),UnityEngine.Random.Range(-40,0),UnityEngine.Random.Range(-40,40)));
                ores.Add(oreP);
            }
            
            if(random){
                seed = UnityEngine.Random.Range(-1000000,1000000);
                
            }
            resolution = (float)voxelsPerChunk/chunkSize;
            //biomes.changeSeed(seed);
            float freq =  2/scale/resolution;
            cS = voxelsPerChunk+overlap;
            sculp = new sculpting(chunkSize,voxelsPerChunk,overlap,0.3f, materials);
            generation = new voxGeneration(cS, resolution,seed, freq);

                
        }
        int cS; //chunkSize(in voxels) + overlap
        Thread Sthread;

        void GenerateTerrain(Vector3Int terrainOffset, bool scu)
        {
            Marching marching = null;
            if(mode == MARCHING_MODE.TETRAHEDRON)
                marching = new MarchingTertrahedron();
            else
                marching = new MarchingCubes();
            
            marching.Surface = surface;  

            // bool newChunk = false;
            Voxel[] chunkVoxels;
            if(!voxels.ContainsKey(terrainOffset)){
                //newChunk = true;
                chunkVoxels = generation.Voxels(/*,s*/terrainOffset,activeOres);
                voxels.Add(terrainOffset,chunkVoxels);
            }
            else{
                chunkVoxels = voxels[terrainOffset];
                //chunkVoxels = new Voxel[cS*cS*cS];
                // for(int i = 0; i<chunkVoxels.Length; i++){
                //     chunkVoxels[i] = voxels[terrainOffset][i];
                // }
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
            indices = null;
            //indices.Reverse();
            
            List<List<int>> subIndices=new List<List<int>>();
            for(int i = 0; i< materials.materialList.Count; i++){
                subIndices.Add(new List<int>());
            }
            //System.Random random = new System.Random();
            //spawnObject.spawn(terrainOffset,terrainOffset,-1);
            bool canCC = false;
            for(int i = 0; i< verts.Count; i = i+3){
                //bool normal = true;
                
                Vector3 triPos = (verts[i]+verts[i+1]+verts[i+2])/3;
                Vector3Int voxeloV = Vector3Int.RoundToInt(triPos);
                int idx = voxeloV.x + voxeloV.y*cS + voxeloV.z*cS*cS;
                int mat =chunkVoxels[idx].material; 
                if(!canCC){
                    int zeroDiffs = 0;
                    if(triPos.x == verts[i].x)
                        zeroDiffs += 1;
                    if(triPos.y == verts[i].y)
                        zeroDiffs += 1;
                    if(triPos.z == verts[i].z)
                        zeroDiffs += 1;
                    if(zeroDiffs<2){
                        canCC = true;
                    }
                }
                subIndices[mat].Add(i+2);
                subIndices[mat].Add(i+1);
                subIndices[mat].Add(i);
                //System.Random random = new System.Random();
                
                // if(newChunk && random.NextDouble() < spawnProbability){
                //  spawnObject.spawn(triPos+terrainOffset,terrainOffset,-1);
                // }// spawn objects
                
            }
            for(int i = 0; i< verts.Count; i++){
                verts[i]= verts[i]/resolution;
                
                //Indices.Add(i);
            }
            
            Action generateMesh;
            if (verts.Count == 0 || !canCC){
                generateMesh = () => {
                    if(meshes.ContainsKey(terrainOffset)){
                        Destroy(meshes[terrainOffset]);
                    }
                    
                };
            }
            else{
                generateMesh = () => {

                    GameObject go = meshGeneration.genMesh(verts,subIndices,materials.materialList,transform);
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
                    generatedChunks.Remove(oldChunks[i]);
                    
                    
                    
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
                    // for(int i = activeOres.Count-1; i>=0;i--){
                    //     Vector3Int OChunk = Chunk(activeOres[i].position);
                    //     float dis = Vector3.Distance(OChunk,playerChunk);
                    //     if(dis>=5){
                    //         activeOres.RemoveAt(i);
                    //     }
                    //     else if(dis<2){
                            
                    //         ores.Remove(activeOres[i]);
                    //         activeOres.RemoveAt(i);
                    //     }
                    // }
                    for (int i = 0; i < ores.Count; i++)
                    {
                        Vector3Int OChunk =Chunk(ores[i].position);
                        float dist = Vector3.Distance(playerChunk,OChunk);
                        if(activeOres.Contains(ores[i])){
                            if(dist>=5){
                                activeOres.Remove(ores[i]);
                            }
                            else if(dist<2){
                                activeOres.Remove(ores[i]);
                                ores.RemoveAt(i);
                                
                            }
                        }
                        else
                        {
                            if(dist<4){
                                activeOres.Add(ores[i]);
                            }
                        }

                        

                    }
                    //print("hello");
                    /*if(t != null){
                        Debug.Log("hello");
                        t.Abort();
                    }*/
                    Vector3 lPP = Player.localPosition;
                    Thread t = new Thread(() => GenerateAllChunks(lPP));
                    t.Start();
                    // GenerateAllChunks(lPP);
                    
                }
                
            }
            
            
            lastPlayerChunk = playerChunk;
            

        }
        
        float plusStrength = 0;
        
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
                        int mat = 0;
                        if(multi == -1){
                            if(materials.selected == -1){
                                for( mat = 0; mat < mesh.subMeshCount; mat++)
                                {
                                    int numIndices = mesh.GetTriangles(mat).Length;
                                    if(numIndices > limit)
                                        break;

                                    limit -= numIndices; 
                                }
                                Material material = collider.GetComponent<MeshRenderer>().sharedMaterials[mat];
                                mat = materials.materialList.IndexOf(material);
                            }
                            else{
                                mat= materials.selected;
                            }
                            
                        }
                        float t = Time.deltaTime+plusStrength;
                        
                        Sthread = new Thread(() => GTL(sculp.sculpt(ref voxels,multi,pos,playerPos,mat,t*sculptStrength)));
                        
                        Sthread.Start();
                        plusStrength = 0;
                              
					}
				}
			}
            else if(Sthread != null && Sthread.IsAlive){
                plusStrength += Time.deltaTime;
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
                materialUI.UpdateValues();
            };
            FunctionsToRunInMainThread.Add(action);
            
        }
        
        bool resetPos = false;
        void FixedUpdate()
        {
            if(resetPos){
                Container.position = Player.localPosition * -1;
                resetPos = false;
            }
            
        }
        //chunk: x=1 y=1 z=1
        public Vector3Int Chunk(Vector3 pos){
            //return new Vector3Int(Mathf.FloorToInt(pos.x/chunkSize),Mathf.FloorToInt(pos.y/chunkSize),Mathf.FloorToInt(pos.z/chunkSize));
            return Vector3Int.FloorToInt(pos/chunkSize);
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
                //fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
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
        }
        public void changeResolution(float newResolution){
            voxelsPerChunk= (int)newResolution;
            resolution = (float)voxelsPerChunk/chunkSize;
            cS = voxelsPerChunk+overlap;
            sculp = new sculpting(chunkSize,voxelsPerChunk,overlap,0.3f, Player.GetComponent<materials>());
        }
        #endregion
        


    }
    public struct orePoint{
        public int material;
        public float radius;
        public Vector3 position;
        public orePoint(int material, float radius, Vector3 position){
            this.material = material;
            this.radius = radius;
            this.position = position;
        }
    }
    [System.Serializable]
    public struct ore{
        public string name;
        public int material;
        public float radius;
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
