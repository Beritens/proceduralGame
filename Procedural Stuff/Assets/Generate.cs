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
        public Transform Container;
        public Transform Player;
        public bool random;
        public Material m_material;
        

        public MARCHING_MODE mode = MARCHING_MODE.CUBES;

        public int seed = 0;
        public int width = 32;
        public int height = 32;
        public int length = 32;
        public float scale;
        [Range(-1,1)]
        public float surface = 0f;
        public float resolution = 1f;
        public int GenerateDistance=2;
        public int overlap = 1;
        public float delDistance = 60f;


        List<GameObject> meshes = new List<GameObject>();
        FractalNoise fractal;
        List<Vector3> generatedChunks = new List<Vector3>();
        Vector3 lastPlayerChunk;
        List<Action> FunctionsToRunInMainThread = new List<Action>();
        
        
        [Space(10)]
        [Header("ore stuff")]
        public GameObject ore;
        public float probability = 1f;
       // [Space(10)]
       // [Header("biomes stuff")]
        //public Biomes biomes;
        //public Color[] colors;
        
    
        void Start()
        {
            if(random){
                seed = UnityEngine.Random.Range(-1000000,1000000);
                
            }
            //biomes.changeSeed(seed);
            //UnityEngine.Random.InitState(seed+1);
            //print(UnityEngine.Random.value > 0.5f);
            INoise perlin = new PerlinNoise(seed, 2/scale/resolution);
            //fractal = new FractalNoise(perlin, 3, 1.0f);
            fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
            
            
            

                
        }

        void GenerateTerrain(Vector3 terrainOffset)
        {
            Marching marching = null;
            if(mode == MARCHING_MODE.TETRAHEDRON)
                marching = new MarchingTertrahedron();
            else
                marching = new MarchingCubes();
            
            marching.Surface = surface;  
            
            int w = (int)((float)width*resolution);
            int h = (int)((float)height*resolution);
            int l = (int)((float)length*resolution);
            //float s = (int)((float)scale*resolution);
            
            

            //Set the mode used to create the mesh.
            //Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface.
            
            //Surface is the value that represents the surface of mesh
            //For example the perlin noise has a range of -1 to 1 so the mid point is where we want the surface to cut through.
            //The target value does not have to be the mid point it can be any value with in the range.
            

            //The size of voxel array.
            

            float[] voxels = Voxels(w,h,l/*,s*/,terrainOffset);

            

            List<Vector3> verts = new List<Vector3>();
            List<int> indices = new List<int>();



            //The mesh produced is not optimal. There is one vert for each index.
            //Would need to weld vertices for better quality mesh.
            marching.Generate(voxels, w, h, l, verts, indices);
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
                    if(splitVerts.Count > 30)
                        go.AddComponent<MeshCollider>();
                        
                        
                    go.transform.localPosition = new Vector3(/*-w / 2 / resolution+*/terrainOffset.x, /*-h / 2 / resolution+*/terrainOffset.y,/* -l / 2 / resolution+*/terrainOffset.z);

                    meshes.Add(go);
                };
                
                QueueMainThreadFunction(generateMesh);
            }

        }

        
        float[] Voxels(int _width, int _height, int _length/*, float _scale*/, Vector3 _offset){
            //The size of voxel array.
            
            Vector3 newOffset = _offset*resolution;
            float[] voxels = new float[_width * _height * _length];

            //Fill voxels with values. Im using perlin noise but any method to create voxels will work.
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _length; z++)
                    {

                        int idx = x + y * _width + z * _width * _height;

                        voxels[idx] = Voxel(x,y,z,_offset,newOffset);

                        
                        //bedrock
                       /* else if(y + newOffset.y < -15){
                            
                            //Debug.Log("hi");
                            voxels[idx] = Mathf.Clamp(voxels[idx]-(y + newOffset.y +15)*0.05f,-1,1);
                            //voxels[idx] = 1;
                        }*/
                        /*if(x== 0 || y == 0 || z== 0 || x == _width-1 || y== _height-1 || z == _length -1){
                            
                            voxels[idx] = surface;
                        }*/
                        //voxels[idx]= Mathf.Clamp(voxels[idx]-(0.5f*(float)y/_height),-1,1);
                        
                        
                    }
                }
            }
            
            return voxels;
        }
        float Voxel(int x, int y, int z, Vector3 offset, Vector3 newOffset){
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
            /*if(y/resolution + offset.y > 5){
                //voxels[idx] = Mathf.Clamp(voxels[idx]-(y +newOffset.y -5)*0.1f,-1,1);
                //float iks = y +newOffset.y -5;
                //voxels[idx] = Mathf.Clamp(voxels[idx]-(0.1f*Mathf.Pow(2,iks)),-1,1);
                float iks = y/resolution + offset.y -5;
                vox = Mathf.Clamp(vox-(Mathf.Pow(iks,2)*0.001f),-1,1);
                //voxels[idx] = 1;
             }*/
            return vox;
        }
        List<Vector3> currentChunks = new List<Vector3>();
        List<Vector3> oldChunks;
        void Update()
        {
            while(FunctionsToRunInMainThread.Count > 0){
                Action func = FunctionsToRunInMainThread[0];
                FunctionsToRunInMainThread.RemoveAt(0);
                if(func != null){
                    func();
                }
                
            }
            Vector3 playerChunk = Chunk(Player.localPosition);
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
                    Vector3 Cpos = meshes[i].transform.position;
                    if(Vector3.Distance(Cpos,Player.position) > delDistance){
                        GameObject m = meshes[i];
                        meshes.Remove(meshes[i]);
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
            lastPlayerChunk = playerChunk;
            

        }
        bool resetPos = false;
        void FixedUpdate()
        {
            if(resetPos){
                Container.position = Player.localPosition * -1;
            }
        }
        Vector3 Chunk(Vector3 pos){
            return new Vector3(Mathf.FloorToInt(pos.x/(width-overlap*2)),Mathf.FloorToInt(pos.y/(height-overlap*2)),Mathf.FloorToInt(pos.z/(length-overlap*2)));
        }
        public void QueueMainThreadFunction(Action someFunc){
            FunctionsToRunInMainThread.Add(someFunc);
        }
        void GenerateAllChunks(Vector3 PlayerPos){
            Vector3 playerChunk  = Chunk(PlayerPos);
            for(int x = (int)playerChunk.x-GenerateDistance;x<= (int)playerChunk.x+GenerateDistance;x++){
                for(int y = (int)playerChunk.y-GenerateDistance;y<= (int)playerChunk.y+GenerateDistance;y++){
                    for(int z = (int)playerChunk.z-GenerateDistance;z<= (int)playerChunk.z+GenerateDistance;z++){
                        currentChunks.Add(new Vector3(x,y,z));
                        if(!generatedChunks.Contains(new Vector3(x,y,z))){
                            generatedChunks.Add(new Vector3(x,y,z));
                            GenerateTerrain(new Vector3(x*(width-overlap*2),y*(height-overlap*2),z*(length-overlap*2)));
                            
                            //Thread t = new Thread(() => GenerateTerrain(new Vector3(x*(width-overlap*2),y*(height-overlap*2),z*(length-overlap*2))));
                            //t.Start();
                            //lastPlayerChunk = playerChunk;
                        }
                        
                    }
                }
            }
            oldChunks = generatedChunks.Except(currentChunks).ToList();
            for(int i = oldChunks.Count-1;i>= 0; i--){
                Vector3 pos = new Vector3(oldChunks[i].x*(width-overlap*2),oldChunks[i].y*(height-overlap*2),oldChunks[i].z*(length-overlap*2));
                if(Vector3.Distance(PlayerPos,pos)<delDistance){
                    oldChunks.Remove(oldChunks[i]);
                }
            }
            currentChunks = new List<Vector3>();
        }
        public void Regenerate(){
            foreach(Transform child in transform){
                    Destroy(child.gameObject);
                }
                meshes= new List<GameObject>();
                if(random){
                    seed = UnityEngine.Random.Range(-1000000,1000000);
                }
                //GenerateTerrain(new Vector3(0,0,0));
                //GenerateTerrain(new Vector3(15,0,0));
                Vector3 playerChunk = Chunk(Player.localPosition);
                
                for(int x = (int)playerChunk.x-GenerateDistance;x<= (int)playerChunk.x+GenerateDistance;x++){
                    for(int y = (int)playerChunk.y-GenerateDistance;y<= (int)playerChunk.y+GenerateDistance;y++){
                        for(int z = (int)playerChunk.z-GenerateDistance;z<= (int)playerChunk.z+GenerateDistance;z++){
                            generatedChunks.Add(new Vector3(x,y,z));
                            GenerateTerrain(new Vector3(x*(width-overlap*2),y*(height-2),z*(length-overlap*2)));
                            lastPlayerChunk = playerChunk;
                        }
                    }
                }
        }
        public void Changeseed(string _seed){
            random = false;
            if(_seed == ""){
                random = true;
                seed = UnityEngine.Random.Range(-10000000,10000000);
                return;
            }
            seed = int.Parse(_seed);
            //biomes.seed = seed;
        }
        public void changeScale(float newScale){
            scale = newScale;
            INoise perlin = new PerlinNoise(seed, 2/scale/resolution);
            //fractal = new FractalNoise(perlin, 3, 1.0f);
            fractal = new FractalNoise(perlin, fractalOctaves, fractalfrequency, fractalamplitude);
        }
        public void changeResolution(float newResolution){
            resolution= newResolution;
        }
        [Space(10)]
        [Header("fractal stuff")]
        public int fractalOctaves = 2;
        public float fractalfrequency = 1f;
        public float fractalamplitude = 1f;
        /*public void changeSize(string newSize){
            int newSizeInt = int.Parse(newSize);
            int newSizeIntPO = newSizeInt+overlap*2;
            height = newSizeIntPO;
            width = newSizeIntPO;
            length = newSizeIntPO;
            delDistance = (float)newSizeInt*GenerateDistance*2.25f;
        }*/
        


    }
    

}
