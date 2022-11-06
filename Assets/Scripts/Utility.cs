using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility
{
    public const int TEMPORARY_LAYER = 31;
    public enum hitCategory{
        noHit,
        hitFromInside,
        hitFromOutside
    }
    public static void cloneStack<myType>(Stack<myType> from, Stack<myType> to){
        myType[] temp = new myType[from.Count];
        from.CopyTo(temp, 0);

        for (int i = temp.Length - 1; i >= 0; i--){
                to.Push(temp[i]);
        }
    }

    public static int switchObjLayer(GameObject GO, int layer){
        int old = GO.layer;
        GO.layer = layer;
        return old;
    }

    // the result is not senstive to whether the direction of normal is from inside or outside of
    // the surface
    public static bool refract(Vector3 inRay, Vector3 n, double n1, double n2, out Vector3 outRay){

        Vector3 normal;
        float thetaIn, thetaOut;
    
        if (Vector3.Dot(-1 * inRay, n) < 0){
            normal = -1 * n;
        }else{
            normal = n;
        }

        if (onlyReflection(inRay, normal, n1, n2)){
            outRay = Vector3.zero;
            //Debug.Log("return true");
            return false;
        }

        //Debug.Log("return false");

        thetaIn = Mathf.Acos(Vector3.Dot(-1 * inRay, normal));
        thetaOut = Mathf.Asin((float)(n1/n2 * Mathf.Sin(thetaIn)));
        //Debug.Log(thetaOut);

        outRay = (float)(n1 / n2) * inRay + (float)(n1 / n2) * Mathf.Cos(thetaIn) * normal - Mathf.Cos(thetaOut) * normal;
        outRay = Vector3.Normalize(outRay);
        return true;
        
    }

    public static bool onlyReflection(Vector3 inRay, Vector3 normal, double n1, double n2){
        if (n1 <= n2) {
            //Debug.Log(n1.ToString() + ", " + n2.ToString());
            return false;
        }

        float criticalAngle = Mathf.Asin((float)(n2/n1));
        float thetaIn = Mathf.Acos(Vector3.Dot(-1 * inRay, normal));
        //Debug.Log(criticalAngle.ToString() + ", " + thetaIn.ToString());

        if (thetaIn <= criticalAngle){
            //Debug.Log(criticalAngle.ToString() + ", " + thetaIn.ToString());
            return false;
        }
        else{
            return true;
        } 
    }

    public static void printStack(Stack<GameObject> stack){
        string output = "";
        foreach(GameObject obj in stack){
            output += obj.name + " ";
        }

        Debug.Log(output);
    }

    public static double getRefractiveIndex(GameObject obj){
        Medium m = obj.GetComponent<Medium>();
        return m.refractiveIndex;
    }

    public static bool isInside(GameObject GO, Vector3 pos){
        int oldLayer = Utility.switchObjLayer(GO, TEMPORARY_LAYER);
        LayerMask mask =  1 << TEMPORARY_LAYER;
        Collider[] c = Physics.OverlapSphere(pos, 0f, mask);
        Utility.switchObjLayer(GO, oldLayer);

        switch (c.Length){
            case 0:
                return false;
            case 1:
                return true;
            default:
                Debug.Assert(false, "[ERROR]: more than 1 object in temporary layer");
                return false;
        }
    }

    
}
