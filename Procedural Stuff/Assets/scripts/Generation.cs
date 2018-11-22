using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralNoiseProject;
using System.Linq;

public class voxGeneration : MonoBehaviour {
	
	//FractalNoise fractal;
	int cS;
	float resolution;
    FastNoise noise = new FastNoise();
	
	public voxGeneration(/* FractalNoise fractal,*/ int cS,float resolution, int seed, float frequency){
		//this.fractal = fractal;
		this.cS = cS;
		this.resolution = resolution;
        SetNoise(seed, frequency);
	}
    public void SetNoise(int seed, float frequency){
        noise.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
        noise.SetFractalOctaves(2);
        noise.SetSeed(seed);
        noise.SetFrequency(frequency);
    }

	
	public Voxel[] Voxels(/*, float _scale*/ Vector3Int _offset){
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
                        voxs[idx] = Voxel(x,y,z,_offset,newOffset);
                        
                        
                    }
                }
            }
            
            return voxs;
        }
        
        
        
        
        Voxel Voxel(int x, int y, int z, Vector3Int offset, Vector3 newOffset){
            Vector3 scaly = Vector3.one;
            //different scale
            // if(y + Offset.y < -50){
            //     //Debug.Log((y + _offset.y+ 50)*0.1f);
            //     scaly = scaly+(-(y + newOffset.y) - 50)*0.001f;
            // }
            
            float fx = (x+newOffset.x) / (2f * scaly.x);
            float fy = (y+newOffset.y) / (2f * scaly.y);
            float fz = (z+newOffset.z) / (2f * scaly.z);

            //float vox = fractal.Sample3D(fx, fy, fz);
            float vox = noise.GetNoise(fx,fy,fz);
            //Debug.Log(vox);
            int mat = 0;
            // if(biomes.GetBiomData(new Vector3(x,y,z)/resolution+offset).biom[0] != 4){
            //     vox = Mathf.Clamp(vox+0.5f,-1,1);
            // }

            //vox= Mathf.Clamp(vox+0.5f,-1,1); //less caves
            float posy = y/resolution + offset.y;
            if(posy > 5){
                // voxels[idx] = Mathf.Clamp(voxels[idx]-(y +newOffset.y -5)*0.1f,-1,1);
                // float iks = y +newOffset.y -5;
                // voxels[idx] = Mathf.Clamp(voxels[idx]-(0.1f*Mathf.Pow(2,iks)),-1,1);
                //bool b = fractal.Sample2D(x/1f,z/1f)>0;
                mat =1;
                float iks = posy -5;
                vox = Mathf.Clamp(vox+(Mathf.Pow(iks,2)*0.01f),-1f,1f);
                // if(posy > 7){
                //     mat = 2;
                // }
                //vox = 1;
             }
            float f = noise.GetNoise(fx,fz);
            //vox = Mathf.Clamp(vox,-0.3f,0.3f);
            if(f>0f){
                mat += 2;
            }
                
            
            //mat = f >0 ? mat+2:mat;
            //vox = Mathf.Sign(vox)*0.1f;//retro voxel style
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
		go.AddComponent<MeshFilter>();
		go.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		go.GetComponent<Renderer>().materials = mats.ToArray();
		go.GetComponent<MeshFilter>().mesh = mesh;
		
		//if(verts .Count > 100)
        go.AddComponent<MeshCollider>();
		return go;
	}
}
