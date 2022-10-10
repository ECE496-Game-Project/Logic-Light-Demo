using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Light
{
    
    Tree lightPath;
    public GameObject lightObject;
    LineRenderer laser;

    public const float MAX_DISTANCE = 30;
    public const int DETECTION_LAYER = 0;
    public const int TEMPORARY_LAYER = 31;
    public const int MAX_INTERACTION = 2;
    public const float FUDGE_FACTOR = 1e-4f;

    public Light(Vector3 pos, Vector3 dir, Material lightMaterial){
        this.lightObject = new GameObject("Light");
        this.laser = (this.lightObject.AddComponent(typeof(LineRenderer))) as LineRenderer;
        this.laser.startWidth = 0.01f;
        this.laser.endWidth = 0.01f;
        this.laser.material = lightMaterial;
        this.laser.startColor = Color.green;
        this.laser.endColor = Color.green;
        this.lightPath = null;
        Stack<GameObject> hierarchy = findHierarchy(pos);
        castRay(pos, dir, hierarchy);
        drawRay();
    }
    
    

    public void destroyGameObj(){
        UnityEngine.Object.Destroy(this.lightObject);
        this.lightObject = null;
    }

    void drawRay(){
        List<Vector3> points = new List<Vector3>();

        points.Add(lightPath.startPos);
        addTreeToList(lightPath.root, points);

        this.laser.positionCount = points.Count;
        this.laser.SetPositions(points.ToArray());
    }

    void addTreeToList(Node node, List<Vector3> points){
        points.Add(node.pos);

        if (node.reflection != null){
            addTreeToList(node.reflection, points);
            points.Add(node.pos);
        }

        if (node.refraction != null){
            addTreeToList(node.refraction, points);
            points.Add(node.pos);
        }
    }
    
    void castRay(Vector3 pos, Vector3 dir, Stack<GameObject> hierarchy){
        this.lightPath = new Tree(pos);
        lightPath.root = castRayHelper(pos, dir, hierarchy, 0, 0);
    }

    Node castRayHelper(Vector3 pos, Vector3 dir, Stack<GameObject> hierarchy, int numReflection, int numRefraction){
        
        if (numReflection + numRefraction > MAX_INTERACTION){
            return null;
        }else if(hierarchy.Count == 0){
            return null;
        }

        Node newNode = new Node();
        double n1, n2;
        RaycastHit hit;

        //the heirarchy of the reflection and refraction
        //TO-DO: just use one stack
        Stack<GameObject> reflectionHier = hierarchy;
        Stack<GameObject> refractionHier = new Stack<GameObject>();
        cloneStack<GameObject>(hierarchy, refractionHier);
        
        int isHit = rayDetection(pos, dir, refractionHier, out hit, out n1, out n2);
        //Debug.Log(n1.ToString() + ", " + n2.ToString());
        /*if (numReflection == 0 || numRefraction ==0){
            printStack(reflectionHier);
            printStack(refractionHier);
        }*/

        // if the ray did not hit any objects
        if (isHit == 0){
            newNode.pos = pos + MAX_DISTANCE * dir;
            newNode.reflection = null;
            newNode.refraction = null;
            return newNode;
        }

        // if the ray hits a boundary of an object

        newNode.pos = hit.point;

        // reflection
        Vector3 reflect_dir = Vector3.Reflect(dir,  hit.normal);

        //Debug.Log(dir);
        //Debug.Log(hit.normal);
        //Debug.Log(reflect_dir);

        newNode.reflection = castRayHelper(hit.point, reflect_dir, reflectionHier, numReflection + 1, numRefraction);

        // reflection end
        
         
        
        // refraction

        // if the ray hits the boundary of the outermost medium, no refraction
        if (refractionHier.Count == 0){
            newNode.refraction = null;
            return newNode;
        }

        Vector3 refract_dir;

        // Debug.Log(dir);
        //Debug.Log(hit.normal);
        
        bool success = refract(dir, hit.normal, n1, n2, out refract_dir);
        // Debug.Log(refract_dir);
        if (success){
            newNode.refraction = castRayHelper(hit.point, refract_dir, refractionHier, numReflection, numRefraction + 1);
            //newNode.refraction = castRayHelper(hit.point, refract_dir, refractionHier, 0, numRefraction + 1);
            
        }
        // refraction end


        return newNode;
    }


    // return how many elemnt is added/removed to the stack, 
    // postive means elements has been added to the stack
    // negative means elements has been removed from the stack
    // 0 means no change to the stack which means the ray did not hit any collider
    // after this function the return stack is the refraction hierarchy
    // this does not work when multiple medium surface overlap each other
    // TODO: might now work when two boundary is touching each other from the inside
    int rayDetection(Vector3 pos, Vector3 dir, Stack<GameObject> hierarchy, out RaycastHit hit, out double n1, out double n2){

        LayerMask mask = 1 << DETECTION_LAYER;
        pos = pos + FUDGE_FACTOR * dir;
        Ray ray = new Ray(pos, dir);
        int result = 0;
        n1 = 0;
        n2 = 0;
        
        
        // check if the ray hit an game object(collider) in the current medium
        if (Physics.Raycast(ray, out hit, MAX_DISTANCE, mask)){
            //Debug.Log("hit an object");
            //Debug.Log(hit.point);
            //check if it is in the current medium
            if (isInside(hierarchy.Peek(), hit.point)){
                n1 = getRefractiveIndex(hierarchy.Peek());
                n2 = getRefractiveIndex(hit.collider.gameObject);
                hierarchy.Push(hit.collider.gameObject);
                result++;
                return result;
            }
            
        }

        // check if the ray exit the current medium, we need to reverse the
        // ray because raycast can not hit the boundary from the inside
        Ray reversedRay = new Ray(ray.GetPoint(MAX_DISTANCE), -1 * dir);

        GameObject current = hierarchy.Peek();

        // put the current medium object into a special layer for faster
        // raycast detection
        int oldLayer = switchObjLayer(current, TEMPORARY_LAYER);
        mask =  1 << TEMPORARY_LAYER;
        bool isHit = Physics.Raycast(reversedRay, out hit, MAX_DISTANCE, mask);
        switchObjLayer(current, oldLayer);

        if (isHit){
            //Debug.Log(pos);
            //Debug.Log(hit.point);
            
            GameObject obj = hierarchy.Pop();
            result--;
            n1 = getRefractiveIndex(obj);
            n2 = (hierarchy.Count > 0)? getRefractiveIndex(hierarchy.Peek()):0;
            //Debug.Log(n1.ToString() + ", " + n2.ToString());
        }
        return result;
        
        
    }

    static int switchObjLayer(GameObject GO, int layer){
        int old = GO.layer;
        GO.layer = layer;
        return old;
    }

    // might cause problem due to multi-threading
    static bool isInside(GameObject GO, Vector3 pos){
        int oldLayer = switchObjLayer(GO, TEMPORARY_LAYER);
        LayerMask mask =  1 << TEMPORARY_LAYER;
        Collider[] c = Physics.OverlapSphere(pos, 0f, mask);
        switchObjLayer(GO, oldLayer);

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

    bool refract(Vector3 inRay, Vector3 n, double n1, double n2, out Vector3 outRay){

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
    
    static bool onlyReflection(Vector3 inRay, Vector3 normal, double n1, double n2){
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

    static double getRefractiveIndex(GameObject obj){
        Medium m = obj.GetComponent<Medium>();
        return m.refractiveIndex;
    }

    static void cloneStack<myType>(Stack<myType> from, Stack<myType> to){
        myType[] temp = new myType[from.Count];
        from.CopyTo(temp, 0);

        for (int i = temp.Length - 1; i >= 0; i--){
                to.Push(temp[i]);
        }
    }
 
    public static List<Tuple<Collider, Vector3>> crateTupleList(Collider[] colliders, Vector3 referencePoint){
        List<Tuple<Collider, Vector3>> result = new List<Tuple<Collider, Vector3>>();
        foreach(Collider c in colliders){
            result.Add(new Tuple<Collider, Vector3>(c, referencePoint));
        }
        return result;
    }

    public static int compareTuple(Tuple<Collider, Vector3> t1, Tuple<Collider, Vector3> t2){    
        //Debug.Log(t1.Item1.gameObject.name + " " + findShortestDistance(t1.Item1, t1.Item2).ToString());
        //Debug.Log(t2.Item1.gameObject.name + " " + findShortestDistance(t2.Item1, t2.Item2).ToString());
        if (findShortestDistance(t1.Item1, t1.Item2) < findShortestDistance(t2.Item1, t2.Item2)){
            return -1;
        }else{
            return 1;
        }
    }

    public static double findShortestDistance(Collider c, Vector3 referencePoint){

        Vector3 min, max;
        min = c.bounds.min;
        max = c.bounds.max;

        double deltaX, deltaY, deltaZ;
        //x
        deltaX = (Math.Abs(referencePoint.x - min.x) < Math.Abs(max.x - referencePoint.x))? Math.Abs(referencePoint.x - min.x): Math.Abs(max.x - referencePoint.x);
        //y
        deltaY = (Math.Abs(referencePoint.y - min.y) < Math.Abs(max.y - referencePoint.y))? Math.Abs(referencePoint.y - min.y): Math.Abs(max.y - referencePoint.y);

        //z
        deltaZ = (Math.Abs(referencePoint.z - min.z) < Math.Abs(max.z - referencePoint.z))? Math.Abs(referencePoint.z - min.z): Math.Abs(max.z - referencePoint.z);

        if (deltaX < deltaY && deltaX < deltaZ) return deltaX;
        else if(deltaY < deltaZ) return deltaY;
        else return deltaZ;
    }

    public static Stack<GameObject> findHierarchy(Vector3 pos){
        
        LayerMask mask = 1 << DETECTION_LAYER;
        Collider[] colliders = Physics.OverlapSphere(pos, 0, mask);
        List<Tuple<Collider, Vector3>> list = crateTupleList(colliders, pos);
        list.Sort(compareTuple);
        Stack<GameObject> result = new Stack<GameObject>(list.Count);

        for (int i = list.Count - 1; i >= 0; i--){
            result.Push(list[i].Item1.gameObject);
        }

        return result;
    }

    public static void printStack(Stack<GameObject> stack){
        string output = "";
        foreach(GameObject obj in stack){
            output += obj.name + " ";
        }

        Debug.Log(output);
    }
}