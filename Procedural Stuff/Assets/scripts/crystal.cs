using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class crystal : MonoBehaviour {

    public float MaxHealth = 50f;
    float health = 50f;
    public GameObject ParticlePrefab;
    public float minSize = 0.5f;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        health = MaxHealth; 
    }
    void OnCollisionEnter(Collision other)
    {
        GameObject.Instantiate(ParticlePrefab,transform.position,Quaternion.identity,transform.parent);
        health -= other.relativeVelocity.magnitude;
        if(health <= 0){
            Destroy(gameObject);
            return;
        }
        float scale= (health/MaxHealth).Remap(0,1,minSize,1);
        transform.localScale = Vector3.one * scale;
        
        

    }
}
public static class ExtensionMethods {
 
public static float Remap (this float value, float from1, float to1, float from2, float to2) {
    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
}
   
}
