﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class restartLevel : MonoBehaviour {
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown("r")){
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
	}
}
