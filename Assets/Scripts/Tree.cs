using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node{
    public Vector3 pos;
    //public Node reflection;
    //public Node refraction;
    public List<Node> children;

    public Node(){
        this.pos = Vector3.zero;
        children = new List<Node>();
    }


    public void setPosition(Vector3 pos){
        this.pos = pos;
    }

    public void setChildren(List<Node> children){
        this.children = children;
    }
}

public class Tree
{
    public Node root;
    public Vector3 startPos;
    public Tree(Vector3 pos){
        this.startPos = pos;
        root = null;
    }
}
