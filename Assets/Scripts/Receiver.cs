using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Receiver : MonoBehaviour, ILightInteractable
{
    // Start is called before the first frame update
    public bool isHit = false;
    void Update(){
        isHit = false;
    }

    public void lightHit(LightInputInfo inputInfo, out LightOutputInfo outputInfo){
        outputInfo.rays = new List<Ray>();
        outputInfo.hiers = new List<Stack<GameObject>>();
        isHit = true;
    }
}
