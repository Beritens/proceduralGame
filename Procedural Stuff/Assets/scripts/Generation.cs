using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralNoiseProject;

public class Generation : MonoBehaviour {
	
	List<Material> m_materials;
	Transform parent;
	FractalNoise fractal;
	int cS;
	float resolution;
	
	public Generation(List<Material> materials,Transform parent, FractalNoise fractal, int cS,float resolution){
		m_materials = materials;
		this.parent = parent;
		this.fractal = fractal;
		this.cS = cS;
		this.resolution = resolution;
	}

	public GameObject genMesh(List<Vector3> verts, List<List<int>> subIndices ){
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
		
		//if(splitVerts.Count > 30)
		go.AddComponent<MeshCollider>();
		return go;
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

            float vox = fractal.Sample3D(fx, fy, fz);
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
                vox = Mathf.Clamp(vox+(Mathf.Pow(iks,2)*0.01f),-1f,0.2f);
                // if(posy > 7){
                //     mat = 2;
                // }
                //vox = 1;
             }
            float f = fractal.Sample2D(fx,fz);
            if(f>0){
                mat += 2;
            }
                
            
            //mat = f >0 ? mat+2:mat;
            //return Mathf.Sign(vox)/10f;//retro voxel style
            return new Voxel(vox,mat);
        }
}
