using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReflectiveSurface : MonoBehaviour, ILightInteractable
{
    // Start is called before the first frame update
    public void lightHit(LightInputInfo inputInfo, out LightOutputInfo outputInfo){
        Ray reflectedRay = new Ray();

        outputInfo.rays = new List<Ray>();
        outputInfo.hiers = new List<Stack<GameObject>>();

        //reflection
        reflectedRay.origin = inputInfo.hitInfo.point;
        reflectedRay.direction = Vector3.Reflect(inputInfo.dir, inputInfo.hitInfo.normal);
        
        outputInfo.rays.Add(reflectedRay);
        outputInfo.hiers.Add(inputInfo.currentHier);

    }
}
