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
    
    public const int MAX_INTERACTION = 3;
    public const float FUDGE_FACTOR = 1e-4f;

    

    public Light(Vector3 pos, Vector3 dir, Material lightMaterial){
        this.lightObject = new GameObject("Light");
        this.laser = (this.lightObject.AddComponent(typeof(LineRenderer))) as LineRenderer;
        this.laser.startWidth = 0.005f;
        this.laser.endWidth = 0.005f;
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
        addTreeToList2(lightPath.root, points);

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
    void addTreeToList2(Node node, List<Vector3> points){
        points.Add(node.pos);

        for (int i = 0; i < node.children.Count; i++){
            addTreeToList2(node.children[i], points);
            points.Add(node.pos);
        }

    }

    void castRay(Vector3 pos, Vector3 dir, Stack<GameObject> hierarchy){
        this.lightPath = new Tree(pos);
        //lightPath.root = castRayHelper(pos, dir, hierarchy, 0, 0);
        lightPath.root = castRayHelper2(pos, dir, hierarchy, 0);
    }

    Node castRayHelper2(Vector3 pos, Vector3 dir, Stack<GameObject> hierarchy, int depth){
        if (depth > MAX_INTERACTION){
            return null;
        }else if(hierarchy.Count == 0){
            return null;
        }

        Node newNode = new Node();
        RaycastHit hit;
        Utility.hitCategory hitInfo = rayDetection2(pos, dir, hierarchy, out hit);
        
        if (hitInfo == Utility.hitCategory.noHit){
            newNode.pos = pos + MAX_DISTANCE * dir;
            return newNode;
        }

        newNode.pos = hit.point;
        LightInputInfo inputInfo = new LightInputInfo(hierarchy, hitInfo, dir, hit);
        LightOutputInfo outputInfo = new LightOutputInfo();
        GameObject hitObject = hit.collider.gameObject;
        ILightInteractable interactable = hitObject.GetComponent<ILightInteractable>();

        if (interactable == null){
            return newNode;
        }
        
        interactable.lightHit(inputInfo, out outputInfo);
        if (outputInfo.rays.Count >= 2){
            Debug.Log(dir + " " + outputInfo.rays[1].direction);
        }
        

        for (int i = 0; i < outputInfo.rays.Count; i++){
            if (outputInfo.rays[i].origin != newNode.pos){
                Debug.LogWarning("The light path is not continous");
            }
            Node child = castRayHelper2(outputInfo.rays[i].origin, outputInfo.rays[i].direction, outputInfo.hiers[i], depth+1);
            
            if (child != null) newNode.children.Add(child);
            
        }
        return newNode;   
    }


    // return whether the ray hits a collider
    Utility.hitCategory rayDetection2(Vector3 pos, Vector3 dir, Stack<GameObject> hierarchy, out RaycastHit hit){

        LayerMask mask = 1 << DETECTION_LAYER;
        pos = pos + FUDGE_FACTOR * dir;
        Ray ray = new Ray(pos, dir);
        
        
        // check if the ray hit an game object(collider) in the current medium
        if (Physics.Raycast(ray, out hit, MAX_DISTANCE, mask)){
            //Debug.Log("hit an object");
            //Debug.Log(hit.point);
            //check if it is in the current medium
            if (Utility.isInside(hierarchy.Peek(), hit.point)){
                return Utility.hitCategory.hitFromOutside;
            }
            
        }

        // check if the ray exit the current medium, we need to reverse the
        // ray because raycast can not hit the boundary from the inside
        Ray reversedRay = new Ray(ray.GetPoint(MAX_DISTANCE), -1 * dir);

        GameObject current = hierarchy.Peek();

        bool isHit = current.GetComponent<Collider>().Raycast(reversedRay, out hit, MAX_DISTANCE);

        if (isHit){
            //Debug.Log(pos);
            //Debug.Log(hit.point);
            
            return Utility.hitCategory.hitFromInside;
            //Debug.Log(n1.ToString() + ", " + n2.ToString());
        }
        return Utility.hitCategory.noHit;
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

        Utility.cloneStack<GameObject>(hierarchy, refractionHier);
        
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
        
        bool success = Utility.refract(dir, hit.normal, n1, n2, out refract_dir);
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
            if (Utility.isInside(hierarchy.Peek(), hit.point)){
                n1 = Utility.getRefractiveIndex(hierarchy.Peek());
                n2 = Utility.getRefractiveIndex(hit.collider.gameObject);
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
        int oldLayer = Utility.switchObjLayer(current, Utility.TEMPORARY_LAYER);
        mask =  1 << Utility.TEMPORARY_LAYER;
        bool isHit = Physics.Raycast(reversedRay, out hit, MAX_DISTANCE, mask);
        Utility.switchObjLayer(current, oldLayer);

        if (isHit){
            //Debug.Log(pos);
            //Debug.Log(hit.point);
            
            GameObject obj = hierarchy.Pop();
            result--;
            n1 = Utility.getRefractiveIndex(obj);
            n2 = (hierarchy.Count > 0)? Utility.getRefractiveIndex(hierarchy.Peek()):0;
            //Debug.Log(n1.ToString() + ", " + n2.ToString());
        }
        return result;
        
        
    }

    

    // might cause problem due to multi-threading
    
    
 
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

    
}