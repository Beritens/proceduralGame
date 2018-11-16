using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarchingCubesProject;
using System.Threading;

public class sculpting{

	Camera cam;
    int chunkSize;
    int voxelsPerChunk;
    float resolution;
    int overlap;
    int cS;
    float max;
    public sculpting(Camera cam, int chunkSize, int voxelsPerChunk, int overlap, float max){
        this.cam = cam;
        this.chunkSize = chunkSize;
        this.voxelsPerChunk = voxelsPerChunk;
        this.overlap = overlap;
        this.resolution = (float)voxelsPerChunk/chunkSize;
        this.cS = (int)((float)chunkSize*resolution)+overlap;
        this.max = max;
    }
    public List<Vector3Int> sculpt(ref Dictionary<Vector3Int,Voxel[]> voxels, int multi,Vector3 pos, Vector3 playerPos, int material){
        
        
                Vector3Int chunk= Chunk(pos)*(chunkSize/*-overlap/*2*/);
                if(voxels.ContainsKey(chunk)){
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
                                
                                
                                //float factor = Mathf.Clamp((distanceplayer-0.5f)/3f,0,1);
                                
                                float change = Mathf.Max(0.02f*(-0.05f*Mathf.Pow(distancevox,2)+1f),0)*multi;
                                if( multi == -1){
                                    float distanceplayer = Vector3.Distance(new Vector3(x,y,z)/resolution+chunk,playerPos);
                                    if(distanceplayer<2f){
                                        change = 0;
                                    }
                                    
                                }
                                
                                if(!(x<0 || y<0 || z<0 || x>=voxelsPerChunk || y>=voxelsPerChunk || z>=voxelsPerChunk)){
                                    int idx = x+ y*cS + z*cS*cS;
                                    
                                    Voxel original = voxels[chunk][idx];
                                    int mat = original.material;
                                    float newMaxL = Mathf.Min(-max,original.value);
                                    float newMaxH = Mathf.Max(max,original.value);
                                    //Debug.Log(newMax);
                                    float vox = Mathf.Clamp(original.value+change,-1,1);

                                    vox = Mathf.Clamp(vox,newMaxL,newMaxH);
                                    if(original.value > 0 && multi == -1){
                                        mat = material;
                                    }
                                    
                                    //clamp -1 and 1 maybe
                                    //Debug.Log(Mathf.Max(0.05f*(-0.1f*Mathf.Pow(distancevox,2)+1f)+100f,0));
                                    voxels[chunk][idx] = new Voxel(vox,mat);
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
                                            SchangeVoxels(ref voxels,change,chunk,thisChunks[i],x,y,z,material);
                                            if(!chunks.Contains(thisChunks[i])){
                                                chunks.Add(thisChunks[i]);
                                            }
                                        }
                                        

                                    }
                                }
                                else{
                                    List<Vector3Int> thisChunks = getChunksfromVoxel(x,y,z,chunk);
                                    for(int i = 0; i< thisChunks.Count;i++){
                                        SchangeVoxels(ref voxels,change,chunk,thisChunks[i],x,y,z,material);
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
                        
                    
                    //Thread t = new Thread(() => GTL(chunks));
                    //t.Start();
                    //cansculpt = false;
                    return chunks;
                }
                return null;
                
            
    }
    

    // void GTL(List<Vector3Int> chunks){
    //     for(int i = 0; i< chunks.Count; i++){
    //         GenerateTerrain(chunks[i]);
    //     }
    // }
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
    void SchangeVoxels(ref Dictionary<Vector3Int,Voxel[]>voxels,float change, Vector3Int chunk, Vector3Int thisChunk, int x, int y, int z, int material){
        if(voxels.ContainsKey(thisChunk)){
            
            Vector3Int dif = chunk-thisChunk;
            int newidx = (x+(int)(dif.x*resolution))+ (y+(int)(dif.y*resolution))*cS + (z+(int)(dif.z*resolution))*cS*cS;
            
            //Debug.Log(x+(int)(dif.x*resolution) + " " + (z+(int)(dif.z*resolution)));
            // if(x+(int)(dif.x*resolution) == x)
            //     Debug.Log(x+(int)(dif.x*resolution) + " " +y+(int)(dif.y*resolution)+ " " + (z+(int)(dif.z*resolution)));
            Voxel original = voxels[thisChunk][newidx];
            int mat = original.material;
            float newMaxL = Mathf.Min(-max,original.value);
            float newMaxH = Mathf.Max(max,original.value);
            float vox=  Mathf.Clamp(original.value+change,-1,1);
            vox = Mathf.Clamp(vox,newMaxL,newMaxH);
            if(original.value > 0 && Mathf.Sign(change) == -1){
                mat = material;
            }
            
            voxels[thisChunk][newidx] = new Voxel(vox,mat);
            
        }  
    }
    public Vector3Int Chunk(Vector3 pos){
        return new Vector3Int(Mathf.FloorToInt(pos.x/(chunkSize/*-overlap/*2*/)),Mathf.FloorToInt(pos.y/(chunkSize/*-overlap/*2*/)),Mathf.FloorToInt(pos.z/(chunkSize/*-overlap/*2*/)));
    }
}
