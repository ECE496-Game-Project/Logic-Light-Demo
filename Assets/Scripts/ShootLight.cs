using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootLight : MonoBehaviour
{
    Light lightBeam;
    public Material lightMaterial;
    void Awake(){
        lightBeam = null;
    }

    // Update is called once per frame
    void Update()
    { 
        
        if(lightBeam != null){
            lightBeam.destroyGameObj();
        }
        lightBeam = new Light(gameObject.transform.position, gameObject.transform.forward, lightMaterial);
        
    }
}
