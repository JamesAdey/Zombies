using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OctreeNode {

	public byte size;
	public Vector3 position;
	public OctreeNode[] childNodes;
	
	public OctreeNode(byte sz, Vector3 pos){
		size = sz;
		position = pos;
	}
	
	public OctreeNode(){
	
	}
	
}

public class BaseOctreeNode : OctreeNode{
	public List<Node> nodes = new List<Node>();
	public List<OctreeNode> connectedAreas = new List<OctreeNode>();
	public BaseOctreeNode(byte sz, Vector3 pos){
		size = sz;
		position = pos;
	}
}
