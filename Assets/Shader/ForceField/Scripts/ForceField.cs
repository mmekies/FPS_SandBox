using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ForceField : MonoBehaviour
{

public GameObject [] Pylons;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Pylons.Length; i++)
        {
            if(Pylons[i]==null){
                Debug.Log("ForceField Down");
                //Remove Wires From Floor Path
                Destroy(gameObject);
            }
            
        }
        
    }
}
