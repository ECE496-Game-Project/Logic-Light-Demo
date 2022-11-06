using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medium : MonoBehaviour, ILightInteractable
{
    public double refractiveIndex;

    void Awake(){
        Debug.Assert(refractiveIndex >= 1, "The refractive index should not be less than 1");
    }

    public void lightHit(LightInputInfo inputInfo, out LightOutputInfo outputInfo){
        Ray reflectedRay = new Ray();
        Ray refractedRay = new Ray();

        outputInfo.rays = new List<Ray>();
        outputInfo.hiers = new List<Stack<GameObject>>();

        //reflection
        reflectedRay.origin = inputInfo.hitInfo.point;
        reflectedRay.direction = Vector3.Reflect(inputInfo.dir, inputInfo.hitInfo.normal);
        
        outputInfo.rays.Add(reflectedRay);
        outputInfo.hiers.Add(inputInfo.currentHier);


        //refraction
        refractedRay.origin = inputInfo.hitInfo.point;
        
        Stack<GameObject> refractiveHier = new Stack<GameObject>();

        Utility.cloneStack<GameObject>(inputInfo.currentHier, refractiveHier);

        if (inputInfo.currentHier.Count == 0){
            Debug.LogError("The light hierarchy is is empty");
        }

        double n1 = Utility.getRefractiveIndex(inputInfo.currentHier.Peek());
        double n2 = 1;

        switch(inputInfo.hitType){
            case Utility.hitCategory.hitFromInside:
                if (refractiveHier.Count == 1) return;
                refractiveHier.Pop();
                n2 = Utility.getRefractiveIndex(refractiveHier.Peek());
                break;
            
            case Utility.hitCategory.hitFromOutside:
                n2 = this.refractiveIndex;
                refractiveHier.Push(this.gameObject);
                break;
            default:
                Debug.LogError("You did not hit me, but you tell me you hit me???");
                break;
        }

        Vector3 outRay;
        if (!Utility.refract(inputInfo.dir, inputInfo.hitInfo.normal, n1, n2, out outRay)){
            return;
        }

        refractedRay.direction = outRay;

        outputInfo.hiers.Add(refractiveHier);
        outputInfo.rays.Add(refractedRay);


    }
    
}
