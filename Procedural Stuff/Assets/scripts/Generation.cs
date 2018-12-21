using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralNoiseProject;
using System.Linq;

public class voxGeneration{
	
	//FractalNoise fractal;
	int cS;
	float resolution;
    FastNoise noise = new FastNoise();
    FastNoise MountainNoise = new FastNoise();
	
	public voxGeneration(/* FractalNoise fractal,*/ int cS,float resolution, int seed, float frequency){
		//this.fractal = fractal;
		this.cS = cS;
		this.resolution = resolution;
        SetNoise(seed, frequency);
	}
    public void SetNoise(int seed, float frequency){
        noise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
        noise.SetFractalOctaves(2);
        noise.SetSeed(seed);
        noise.SetFrequency(frequency);

        //mountain
        MountainNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
        MountainNoise.SetFractalOctaves(2);
        MountainNoise.SetFrequency(0.06f);
    }
    
	
	public Voxel[] Voxels(/*, float _scale*/ Vector3Int _offset, List<orePoint> ores){
            //The size of voxel array.
            Vector3 v = _offset;
            Vector3 newOffset = v*resolution;
            Voxel[] voxs = new Voxel[cS*cS*cS];

            //Fill voxels with values. Im using perlin noise but any method to create voxels will work.
            for (int x = 0; x < cS; x++)
            {
                for (int y = 0; y < cS; y++)
                {
                    for (int z = 0; z < cS; z++)
                    {

                        int idx = x + y * cS + z * cS * cS;
                        voxs[idx] = Voxel(x,y,z,_offset,newOffset, ores);
                        
                        
                    }
                }
            }
            
            return voxs;
        }
        
        
        
        
        Voxel Voxel(int x, int y, int z, Vector3Int offset, Vector3 newOffset, List<orePoint> ores){
            //different scale
            // if(y + Offset.y < -50){
            //     //Debug.Log((y + _offset.y+ 50)*0.1f);
            //     scaly = scaly+(-(y + newOffset.y) - 50)*0.001f;
            // }
            
            float fx = (x+newOffset.x) / (2f);
            float fy = (y+newOffset.y) / (2f);
            float fz = (z+newOffset.z) / (2f);

            //float vox = fractal.Sample3D(fx, fy, fz);
            float vox = noise.GetNoise(fx,fy,fz);
            //Debug.Log(vox);
            int mat = 0;
            bool matchanged = false;
            Vector3 truepos = new Vector3(x/resolution+offset.x,y/resolution+offset.y,z/resolution+offset.z);
            for(int i = 0; i< ores.Count; i++){
                if(Vector3.Distance(ores[i].position,truepos)< ores[i].radius){
                    mat = ores[i].material;
                    matchanged = true;
                }
            }
            // if(biomes.GetBiomData(new Vector3(x,y,z)/resolution+offset).biom[0] != 4){
            //     vox = Mathf.Clamp(vox+0.5f,-1,1);
            // }

            //vox-=0.2f; //less caves
            float posy = truepos.y;
            if(posy > 5){
                if(!matchanged)
                    mat =1;
                float iks = posy -5;

                float mH = 20f;
                float height = (MountainNoise.GetNoise(fx,fz)+1f)*mH;
                float p = iks/(mH*2);
                vox = vox*(1-p) + ((iks-height)/mH)*p; //mountains

                //vox = /* Mathf.Clamp(*/vox+(Mathf.Pow(iks,2)*0.01f)/* ,-1f,1f)*/;
             }
            
            //vox = (posy-(((x+newOffset.x)*0.5f *((z+newOffset.z)*0.5f))))/20f;
            //vox = (posy-(x+newOffset.x))/20;
            //vox = posy-5;
            vox = Mathf.Clamp(vox,-0.3f,0.3f);
            
            if(!matchanged){
                float f = noise.GetNoise(fx,fz);
                if(f>0f){
                    mat += 2;
                }
            }
            

            // if(posy > 40){
            //     mat = 4;
            // }
                
            
            //vox = Mathf.Sign(vox)*0.2f;//retro voxel style
            return new Voxel(vox,mat);
        }
}
public class meshGeneration{
    static public GameObject genMesh(List<Vector3> verts, List<List<int>> subIndices, List<Material> m_materials, Transform parent){
		Mesh mesh = new Mesh();
		mesh.SetVertices(verts);
		int submeshIndex = 0;
		List<Material> mats = new List<Material>();
		for(int i = 0; i<subIndices.Count; i++){
			
			if(subIndices[i].Count == 0){
				continue;
			}
			else{
				mesh.subMeshCount++;
				mesh.SetTriangles(subIndices[i], submeshIndex);
				mats.Add(m_materials[i]);
				submeshIndex ++;
			}
			
		}
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		GameObject go = new GameObject("Mesh");
		go.transform.parent = parent;
		MeshFilter mF = go.AddComponent<MeshFilter>();
		go.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		go.GetComponent<Renderer>().materials = mats.ToArray();
		mF.mesh = mesh;
        go.AddComponent<MeshCollider>();
		return go;
	}
    
}
