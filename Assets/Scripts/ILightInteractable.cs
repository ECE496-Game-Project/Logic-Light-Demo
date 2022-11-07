using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LightInputInfo{
    public Stack<GameObject> currentHier;

    public Utility.hitCategory hitType;

    public Vector3 dir;
    
    public RaycastHit hitInfo;

    public LightInputInfo(Stack<GameObject> currentHier, Utility.hitCategory hitType, Vector3 dir, RaycastHit hitInfo){
        this.hitType = hitType;
        this.dir = dir;
        this.hitInfo = hitInfo;
        this.currentHier = currentHier;
    }
}

public struct LightOutputInfo{
    public List<Stack<GameObject>> hiers;

    public List<Ray> rays;

    public LightOutputInfo(List<Stack<GameObject>> hiers, List<Ray> rays){
        this.hiers = hiers;
        this.rays = rays;
    }

} 

//class with this interface will interact with light
public interface ILightInteractable
{
    void lightHit(LightInputInfo inputInfo, out LightOutputInfo outputInfo);
}
