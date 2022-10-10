using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log(LayerMask.NameToLayer("Test Layer"));
        gameObject.layer = 31;
        Physics.queriesHitBackfaces= true;
    }

    // Update is called once per frame
    void Update()
    {
        Collider[] c = Physics.OverlapSphere(gameObject.transform.position, 0f);
        Debug.Log(c.Length);
    }
}
