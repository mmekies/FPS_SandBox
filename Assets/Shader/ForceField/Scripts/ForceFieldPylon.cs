using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceFieldPylon : MonoBehaviour
{
    int health;
    // Start is called before the first frame update
    void Start()
    {
        health = 100;
    }

    // Update is called once per frame
    void Update()
    {
        if(health<=0){
            Destroy(gameObject);
            Debug.Log("Pylon Destroyed");
        }
    }

    void OnCollisionEnter(Collision col) {
 
        Debug.Log(col.gameObject.tag);
        if(col.gameObject.tag == "Player");
        health -= 10;
        print ("hit");
        }
        
}
