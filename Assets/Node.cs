using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Node
{
	public float distToPlayer = 0;
	public Vector3 position = Vector3.zero;
	public Vector3 normal = Vector3.zero;
	public LinkedList<NavConnectionData> connections;
	//public BaseOctreeNode octet;
	public Node targetNode;
	public bool hasPlayerConnection = false;
	public bool isChecked = false;
	public NodeClass nodeClass = NodeClass.none;
	public NodeType nodeType = NodeType.none;
	public bool isDead = false;

	public Node (Vector3 pos, Vector3 nrml)
	{
		position = pos;
		normal = nrml;
		connections = new LinkedList<NavConnectionData> ();
		isDead = false;
	}

	public void KillNode ()
	{
		connections.Clear ();
		targetNode = null;
		isDead = true;
	}

	public bool AddConnection (Node newCon)
	{
		// check if we already have this connection, we don't want to double up
		foreach (NavConnectionData data in connections) {
			if (data.node == newCon) {
				return false;
			}
		}

		// copy connection array
		NavConnectionData newData = new NavConnectionData (newCon);
		newData.UpdateDistanceFromNode (this);
		connections.AddLast (newData);
		return true;
	}

	public void RemoveConnection (Node oldCon)
	{
		if (connections.Count == 0) {
			return;
		}
		bool didFindNode = false;
		NavConnectionData foundData = new NavConnectionData ();

		// find the connection
		foreach (NavConnectionData con in connections) {
			if (con.node == oldCon) {
				foundData = con;
				didFindNode = true;
			}
		}
		// remove the connection
		if (didFindNode == true) {
			connections.Remove (foundData);
		}

	}
	
}
