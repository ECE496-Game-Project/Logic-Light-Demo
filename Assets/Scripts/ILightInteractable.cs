using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LightInputInfo{
    Stack<GameObject> currentHier;

    bool hitFromInside;

    Vector3 dir;
    
    Vector3 hitPos;

}

public struct LightOutputInfo{
    List<Stack<GameObject>> hiers;

    List<Ray> rays;
    
}

//class with this interface will interact with light
public interface ILightInteractable
{
    void interact(LightInputInfo inputInfo, out LightOutputInfo outputInfo);
}
