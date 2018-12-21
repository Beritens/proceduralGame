using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class materials : MonoBehaviour {

	public List<Material> materialList = new List<Material>();
	public List<float> materialStorage;
	public int selected = -1;
	/// <summary>
	/// Start is called on the frame when a script is enabled just before
	/// any of the Update methods is called the first time.
	/// </summary>
	void Start()
	{
		materialStorage = new List<float>(new float[materialList.Count]);
	}
}
