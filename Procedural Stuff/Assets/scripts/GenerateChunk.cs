using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarchingCubesProject;
using ProceduralNoiseProject;
using System;

public class GenerateChunk : MonoBehaviour {

    int cS;
    float resolution;
    float surface;
    List<Material> m_materials = new List<Material>();
    MARCHING_MODE mode;
    FractalNoise fractal;
    public GenerateChunk(int cS, float resolution, float surface, List<Material> materials, MARCHING_MODE mode, FractalNoise fractal){
        this.cS = cS;
        this.resolution = resolution;
        this.surface = surface;
        this.m_materials = materials;
        this.mode= mode;
        this.fractal = fractal;
    }
	public chunk GenerateNewChunk(Vector3Int terrainOffset)
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
        
        chunkVoxels = Voxels(/*,s*/terrainOffset);
        //voxels.Add(terrainOffset,chunkVoxels);
        
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
        
        
        if (verts.Count == 0) return new chunk(null, null);
        Action<Dictionary<Vector3Int,GameObject>> generateMesh = meshes => {
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
            go.transform.parent = transform;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<Renderer>().materials = mats.ToArray();
            go.GetComponent<MeshFilter>().mesh = mesh;
            
            //if(splitVerts.Count > 30)
            go.AddComponent<MeshCollider>();
                
                
            go.transform.localPosition = new Vector3(/*-w / 2 / resolution+*/terrainOffset.x, /*-h / 2 / resolution+*/terrainOffset.y,/* -l / 2 / resolution+*/terrainOffset.z);
            if(meshes.ContainsKey(terrainOffset)){
                Destroy(meshes[terrainOffset]);
                meshes[terrainOffset]=go;
                
            }
            else{
                meshes.Add(terrainOffset,go);
            }
            
            
        };
        
        
        return new chunk(chunkVoxels, generateMesh);
    }
    public Action<Dictionary<Vector3Int,GameObject>> regenerateChunk(Vector3Int terrainOffset, Voxel[] voxels)
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
        
        chunkVoxels = voxels;
        //voxels.Add(terrainOffset,chunkVoxels);
        
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
        
        
        if (verts.Count == 0) return null;
        Action<Dictionary<Vector3Int,GameObject>> generateMesh = meshes => {
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
            go.transform.parent = transform;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<Renderer>().materials = mats.ToArray();
            go.GetComponent<MeshFilter>().mesh = mesh;
            
            //if(splitVerts.Count > 30)
            go.AddComponent<MeshCollider>();
                
                
            go.transform.localPosition = new Vector3(/*-w / 2 / resolution+*/terrainOffset.x, /*-h / 2 / resolution+*/terrainOffset.y,/* -l / 2 / resolution+*/terrainOffset.z);
            if(meshes.ContainsKey(terrainOffset)){
                Destroy(meshes[terrainOffset]);
                meshes[terrainOffset]=go;
                
            }
            else{
                meshes.Add(terrainOffset,go);
            }
            
            
        };
        
        
        return generateMesh;
    }
        
        
        
        Voxel[] Voxels(/*, float _scale*/ Vector3Int _offset){
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
public struct chunk{
    public Voxel[] voxels;
    public Action<Dictionary<Vector3Int,GameObject>> action;
    public chunk(Voxel[] voxels, Action<Dictionary<Vector3Int,GameObject>> action){
        this.voxels = voxels;
        this.action = action;
    }
}