using UnityEngine;
using System.Collections;

public struct NodeConnection {
	byte distance;
	Node destination;
	
	public NodeConnection(Node toNode){
		distance = 0;
		destination = toNode;
	}
}
