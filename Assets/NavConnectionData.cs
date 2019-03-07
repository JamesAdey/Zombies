using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NavConnectionData
{
	public NavConnectionData (Node _node)
	{
		node = _node;
		distance = 0;
	}

	public Node node;
	public float distance;

	public void UpdateDistanceFromNode (Node nd)
	{
		distance = Vector3.Distance (nd.position, node.position);
	}

	public static bool operator == (NavConnectionData a, NavConnectionData b)
	{
		return a.node == b.node;
	}

	public static bool operator != (NavConnectionData a, NavConnectionData b)
	{
		return a.node != b.node;
	}

	public static implicit operator Node (NavConnectionData d)
	{
		return d.node;
	}
}
