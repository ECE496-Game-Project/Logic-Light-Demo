using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node{
    public Vector3 pos;
    public Node reflection;
    public Node refraction;

    public Node(Vector3 pos, Node reflection, Node refraction){
        this.pos = pos;
        this.reflection = reflection;
        this.refraction = refraction;
    }
    public Node(){
        this.pos = Vector3.zero;
        this.reflection = null;
        this.refraction = null;
    }

    public Node(Vector3 pos){
        this.pos = pos;
        this.reflection = null;
        this.refraction = null;
    }

    public void setPosition(Vector3 pos){
        this.pos = pos;
    }

    public void setChildren(Node reflection, Node refraction){
        this.reflection = reflection;
        this.refraction = refraction;
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
