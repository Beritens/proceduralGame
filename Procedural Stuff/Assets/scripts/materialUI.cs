using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class materialUI : MonoBehaviour {
	public materials materials;
	public List<Text> numbers;
	//public int selected= 0;
	public RectTransform selecter;

	public void UpdateValues(){
		for(int i =0; i < numbers.Count; i++){
			numbers[i].text = materials.materialStorage[i].ToString("0.#");
		}
	}
	/// <summary>
	/// Update is called every frame, if the MonoBehaviour is enabled.
	/// </summary>
	void Update()
	{
		if(Input.GetAxis("Mouse ScrollWheel") != 0){
			int switchAmount = (int)Mathf.Sign(Input.GetAxis("Mouse ScrollWheel"));  
			materials.selected = materials.selected-switchAmount;
			if (materials.selected>= materials.materialList.Count){
				materials.selected = -1+ (materials.selected-materials.materialList.Count);
			}
			else if(materials.selected < -1){
				materials.selected = materials.materialList.Count - (-1-materials.selected);
			}
			selecter.localPosition = Vector2.right * ((materials.selected+1)*60);

		}
	}
}
