using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralNoiseProject;
public class Biomes : MonoBehaviour {
	public TileData testy;
	public int seed = 0;
	bool gotSeed = false;
	FractalNoise fractalT;
	FractalNoise fractalH;
	public float scaleT = 20f;
	public int fractalOctavesT = 1;
	public float fractalfrequencyT = 1f;
	public float fractalamplitudeT = 1f;
	public float scaleH = 20f;
	public int fractalOctavesH = 1;
	public float fractalfrequencyH = 1f;
	public float fractalamplitudeH = 1f;
	public Vector3 test;
	public int HseedOffset = 289;
	ValueNoise voronoiT;
	ValueNoise voronoiH;

	// Use this for initialization
	void Start () {
		//changeSeed(seed);
		
	}
	public void changeSeed(int newseed){
		seed = newseed;
		INoise perlin = new PerlinNoise(seed, 2f);
            //fractal = new FractalNoise(perlin, 3, 1.0f);
        fractalT = new FractalNoise(perlin, fractalOctavesT, fractalfrequencyT, fractalamplitudeT);
		voronoiT = new ValueNoise(seed, fractalOctavesT);
		perlin = new PerlinNoise(seed + HseedOffset, 2f);
		voronoiH = new ValueNoise(seed + HseedOffset, fractalOctavesT);
		//print("hi");
		fractalH = new FractalNoise(perlin, fractalOctavesH, fractalfrequencyH, fractalamplitudeH);
	}
	public biomData GetBiomData(Vector3 position){
		int[] biom;
		float[] percenatge;
		float Tx = position.x/scaleT;
		float Ty = position.y/scaleT;
		float Tz = position.z/scaleT;

		float Hx = position.x/scaleH;
		float Hy = position.y/scaleH;
		float Hz = position.z/scaleH;
		float t= voronoiT.Sample3D(Tx,Ty,Tz)/2f+0.5f;
		float h= voronoiH.Sample3D(Hx,Hy,Hz)/2f+0.5f;
		//print(voronoiT.Sample3D(Tx,Ty,Tz));

		//Debug.Log(t + " " + h);
		biom = new int[1];
		int temp = Mathf.FloorToInt(t*testy.rows.Length);
		int hum= Mathf.FloorToInt(h*testy.rows[temp].row.Length);
		//Debug.Log(t*testy.rows.Length + " " + h*testy.rows[temp].row.Length);
		biom[0]= testy.rows[temp].row[hum];
		percenatge = new float[1]{1};
		return new biomData(biom,percenatge);
	}
	[ContextMenu ("new Biom")]
	public void hi(){
		//print(GetBiomData(test).biom[0]);
	}
	
	
}

public class biomData{
	public int[] biom;
	public float[] percentage;
	public biomData(int[] _biom, float[] _percentage){
		biom = _biom;
		percentage = _percentage;
	} 
}
