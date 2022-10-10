using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medium : MonoBehaviour
{
    public double refractiveIndex;

    void Awake(){
        Debug.Assert(refractiveIndex >= 1, "The refractive index should not be less than 1");
    }
}
