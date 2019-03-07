using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NavGraphController : MonoBehaviour
{
	static NavGraphController singleton;
	public Vector3 positiveBoundary;
	//public Vector3 negativeBoundary;
	
	Node[] nodeGraph = new Node[0];
	NodeGridSegment[,] nodeGrid;
	
	// PLAYER VARIABLES
	public Vector3[] playerScaledPos = new Vector3[5];
	public Vector3 playerPos;
	public Transform player;
	public BaseOctreeNode basePlayerOctet;
	
	// NODE GRAPH CREATION
	public int nodeBuildInterval = 2;
	public Vector3 minNodeBoundary = new Vector3 (0, 1, 0);
	public Vector3 maxNodeBoundary = new Vector3 (200, 1, 200);
	public LayerMask nodeBuildLayers;
	public Vector3 nodeBuildPos;
	bool creatingNodeGrid = false;
	bool optimisingNavGraph = false;
	public static bool graphDone = false;
	public float distanceToConnect = 2;
	public float nodeRadius = 1;
	public float nodeWalkWidth = 0.5f;
	public float floorWidth = 0.5f;
	List<Node> newNodeList = new List<Node> ();
	// OCTREE SEARCHING VARIABLES
	Vector3[] scaledPos = new Vector3[5];
	int octetSize;
	OctreeNode nextOctreeNode;

	
	// NODE GRAPH UPDATING
	public bool navGraphUpdated = false;
	public bool isDoneUpdating = false;
	public bool waitForNextUpdate = false;
	public bool showNavGraph = false;
	public bool showZombieNav = false;
	public List<Node> nodesToUpdate = new List<Node> ();
	int targetNodeIndex = 0;
	public int numOfTimesCalled = 0;
	short minNodesToPlayer = 0;
	Node currentNode;
	Node startNode;
	List<Node> nextNodesToUpdate = new List<Node> ();
	
	// OCTREE BUILDING VARIABLES
	// OCTREE for searching
	// 5 Layers of searching
	// 128x128
	// 64x64
	// 32x32
	// 16x16
	// 8x8
	public List<OctreeNode> octree = new List<OctreeNode> ();
	public List<BaseOctreeNode> baseOctreeNodes = new List<BaseOctreeNode> ();
	List<Node> newNodes = new List<Node> ();
	Vector3[] baseOctreeBuildPos;
	Vector3 octreeBuildPos;
	
	Vector3 baseBuildPos;
	Vector3 nextBuildPos;
	byte nodeSize = 128;
	Vector3 buildPos;
	
	Vector3[] octreeSize;

	#region STATIC INTERFACE FUNCTIONS

	public static Node GetNearestNode (Vector3 _position)
	{
		if (graphDone == true) {
			return singleton.__FindNearestNodeToPosition (_position);
		} else {
			return null;
		}
	}

	static BaseOctreeNode __baseOctet;
	static float __dist;
	static float __newDist;
	static int __startIndex;
	static int __i;

	Node __FindNearestNodeToPosition (Vector3 _pos)
	{
		__baseOctet = GetBaseOctetNearestToPosition (_pos);

		// set the first node in the octet as the nearest node
		__dist = (_pos - __baseOctet.nodes [0].position).sqrMagnitude;
		__newDist = __dist;
		__startIndex = 0;
		// search this octet for the nearest node
		for (int __i = 1; __i < __baseOctet.nodes.Count; __i++) {
			// get distance to player
			__newDist = (_pos - __baseOctet.nodes [__i].position).sqrMagnitude;
			// check if its shorter than the last distance
			if (__newDist < __dist && Physics.Linecast (_pos, __baseOctet.nodes [__i].position, nodeBuildLayers) == false) {
				// record this as the target node
				__startIndex = __i;
			}
			// TODO
			// sort into ascending order
			// raycast each one individually to determine if one can be reached
			// set the start node to the closest
		}
		return __baseOctet.nodes [__startIndex];
	}

	#endregion

	void Awake ()
	{ 
		singleton = this;
	}

	// Use this for initialization
	void Start ()
	{
		graphDone = false;
		CreateOctree ();
		StartCoroutine (CreateNodeGraph ());
		
	}

	void OnDrawGizmos ()
	{
		if (Application.isPlaying == true) {
			if (showZombieNav == true) {
				Gizmos.color = Color.magenta;
				for (int i = 0; i < nodeGraph.Length; i++) {
					if (nodeGraph [i].targetNode != null) {
						Gizmos.DrawLine (nodeGraph [i].position, nodeGraph [i].targetNode.position);
					}
				}
				if (startNode != null) {
					Gizmos.color = Color.white;
					Gizmos.DrawWireSphere (startNode.position, nodeRadius);
				}
			}
			if (showNavGraph == true) {
				Gizmos.color = Color.white;
				for (int i = 0; i < newNodes.Count; i++) {
					Gizmos.DrawWireCube (newNodes [i].position, Vector3.one);
				}
				/*for (int i = 0; i < newNodeList.Count; i++) {
					Gizmos.DrawWireCube (newNodeList [i].position, Vector3.one);
				}*/
				for (int i = 0; i < nodeGraph.Length; i++) {
					if (nodeGraph [i].nodeClass == NodeClass.required) {
						Gizmos.color = Color.magenta;
					} else if (nodeGraph [i].nodeClass == NodeClass.important) {
						Gizmos.color = Color.cyan;
					} else {
						Gizmos.color = Color.white;
					}
					Gizmos.DrawWireSphere (nodeGraph [i].position, nodeRadius);
				}
				if (currentNode != null) {
					Gizmos.color = Color.red;
					Gizmos.DrawWireSphere (currentNode.position, nodeRadius);
				}
			}
					
			if (basePlayerOctet != null) {
				Gizmos.color = Color.white;
				Gizmos.DrawWireCube (basePlayerOctet.position, octreeSize [4]);
				if (showNavGraph == true) {
					for (int i = 0; i < basePlayerOctet.nodes.Count; i++) {
						Gizmos.color = Color.green;
						Gizmos.DrawLine (basePlayerOctet.position, basePlayerOctet.nodes [i].position);
						Gizmos.color = Color.white;
						Gizmos.DrawWireCube (basePlayerOctet.nodes [i].position, Vector3.one);
						foreach (NavConnectionData data in basePlayerOctet.nodes[i].connections) {
							Gizmos.color = Color.red;
							Gizmos.DrawLine (basePlayerOctet.nodes [i].position, data.node.position);
						}
					}
				}
			}
		}
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube (positiveBoundary * 0.5f, positiveBoundary);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube ((maxNodeBoundary + minNodeBoundary) * 0.5f, maxNodeBoundary - minNodeBoundary);
	}

	void OnDrawGizmosSelected ()
	{
		
		// draw a node at each position;
		if (Application.isPlaying == true) {
			/*for (int n=0; n<nodeGraph.Length; n++) {
				Gizmos.DrawWireCube (nodeGraph [n].position, Vector3.one);
			}*/
						
			// draw the level 1 boxes
		
		
			/*
		for(int a=0;a<octree.Count;a++){
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(octree[a].position,octreeSize[0]);
			for(int b=7;b<octree[a].childNodes.Length;b++){
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireCube(octree[a].childNodes[b].position,octreeSize[1]);
				for(int c=7;c<octree[a].childNodes[b].childNodes.Length;c++){
					Gizmos.color = Color.green;
					Gizmos.DrawWireCube(octree[a].childNodes[b].childNodes[c].position,octreeSize[2]);
					for(int d=7;d<octree[a].childNodes[b].childNodes[c].childNodes.Length;d++){
						Gizmos.color = Color.yellow;
						Gizmos.DrawWireCube(octree[a].childNodes[b].childNodes[c].childNodes[d].position,octreeSize[3]);	
						for(int e=7;e<octree[a].childNodes[b].childNodes[c].childNodes[d].childNodes.Length;e++){
							Gizmos.color = Color.blue;
							Gizmos.DrawWireCube(octree[a].childNodes[b].childNodes[c].childNodes[d].childNodes[e].position,octreeSize[4]);	
						}	
					}		
				}	
			}
		}*/
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (navGraphUpdated == true) {
			navGraphUpdated = false;
			StartCoroutine (RecalculateNavGraph ());
			
		}
		if (player != null) {
			basePlayerOctet = GetBaseOctetNearestToPosition (player.position);
		}
	}

	
	
	IEnumerator CreateNodeGraph ()
	{
		// simple node graph creator
		
		//nodeBuildInterval = 5;
		
		// creating the grid will take time, so coroutine it
		creatingNodeGrid = true;
		StartCoroutine (CreateNodeGrid ());
		while (creatingNodeGrid == true) {
			yield return new WaitForEndOfFrame ();
		}

		// set the neighbours
		SetNodeNeighbours ();
		yield return new WaitForEndOfFrame ();

		// optimise the graph!
		optimisingNavGraph = true;
		StartCoroutine (OptimiseNavGraph ());
		// this could take ages, so coroutine it.
		while (optimisingNavGraph == true) {
			yield return new WaitForEndOfFrame ();
		}
		AssignNodesToOctree ();
		// now create octree neighbours
		SetOctreeNeighbours ();
		// collect the garbage
		yield return new WaitForEndOfFrame ();
		System.GC.Collect ();
		graphDone = true;
		navGraphUpdated = true;
		// TODO move this from here...
		ZombieSpawner.singleton.hasZombiesRemaining = true;
	}

	void AssignNodesToOctree ()
	{
		// Strangely, this turned out to be very simple
		BaseOctreeNode baseNode;
		for (int n = 0; n < nodeGraph.Length; n++) {
			baseNode = GetBaseOctetNearestToPosition (nodeGraph [n].position);
			baseNode.nodes.Add (nodeGraph [n]);
		}	
	}



	static OctreeSortNode[] sortedNodes;

	static void DoSort ()
	{
		int currentPos = 0;
		OctreeSortNode firstNumber;
		OctreeSortNode secondNumber;
		bool passDone = false;
		if (sortedNodes.Length > 2) {
			// compare first and second
			currentPos = 1;
			for (int i = 1; i < sortedNodes.Length; i++) {
				currentPos = i;
				passDone = false;
				//compare first and second
				while (passDone == false) {
					secondNumber = sortedNodes [currentPos];
					firstNumber = sortedNodes [currentPos - 1];
					//totalChecks ++;
					if (firstNumber.distance > secondNumber.distance) {
						// swap the numbers
						sortedNodes [currentPos] = firstNumber;
						sortedNodes [currentPos - 1] = secondNumber;

						// reduce the current position by 1
						currentPos--;
						// check if the current position is at the end of the list
						//totalSwaps++;
						if (currentPos == 0) {
							passDone = true;
						}
					} else {
						passDone = true;
					}
				}
			}
		}
	}

	void SetOctreeNeighbours ()
	{

		foreach (BaseOctreeNode oct in baseOctreeNodes) {
			if (oct.nodes.Count == 0) {
				// connect this octree to the nearest visible node.
				sortedNodes = new OctreeSortNode[nodeGraph.Length];

				for (int i = 0; i < sortedNodes.Length; i++) {
					sortedNodes [i] = new OctreeSortNode ();
					sortedNodes [i].node = nodeGraph [i];
					sortedNodes [i].distance = (nodeGraph [i].position - oct.position).sqrMagnitude;
				}
				// sort the octree nodes based on distance
				DoSort ();
				// now keep checking until we find the nearest one within line of sight
				for (int i = 0; i < sortedNodes.Length; i++) {
					if (Physics.Linecast (oct.position, sortedNodes [i].node.position) == false) {
						oct.nodes.Add (sortedNodes [i].node);
						// stop loop
						i = sortedNodes.Length;
					}
				}
				// clear the sorted nodes
				foreach (OctreeSortNode nd in sortedNodes) {
					nd.ClearSortNode ();
				}
			}
		}
		/*
		// octree width is 8
		for (float i = minNodeBoundary.x +4; i < maxNodeBoundary.x; i+=8) {
			for(float j=minNodeBoundary.y+4; j<minNodeBoundary.y;j+=8){
				for(float k=minNodeBoundary.z+4; k<maxNodeBoundary.z;k+=8){
					// work out a point that should be in this octree, hopefully at the center
					Vector3 octreePos = new Vector3 (i, j, k);
					// get the octree node nearest this position
					BaseOctreeNode baseNode = GetBaseOctetNearestToPosition (octreePos);
					// now work out all the octree nodes next to it
					// its a 3x3x3 cube... with no center node.
					for(int a = -1;a<2;a++){
						for(int b = -1;b<2;b++){
							for(int c = -1;c<2;c++){
								if (a != 0 && b != 0 && c != 0) {
									Vector3 newPos = octreePos + new Vector3 (a * 8, b * 8, c * 8);
									BaseOctreeNode connectedNode = GetBaseOctetNearestToPosition (newPos);
									// check if the node is null
									if (connectedNode != null) {
										// connect this up
										baseNode.connectedAreas.Add (connectedNode);
									}
								}
							}
						}
					}
				}
			}
		}
		*/
	}


	
	OctreeNode GetNearestOctreeLayer (OctreeNode[] octree, Vector3 searchPos)
	{
		for (byte n = 0; n < octree.Length; n++) {
			if (octree [n].position.Equals (searchPos) == true) {
				return octree [n];
			}		
		}
		return null;
	}

	
	
	
	void CreateOctree ()
	{
	
		// set the base build positions for the octree segements
		baseOctreeBuildPos = new Vector3[8];
		baseOctreeBuildPos [0] = new Vector3 (0, 0, 0);
		baseOctreeBuildPos [1] = new Vector3 (0, 0, 1);
		baseOctreeBuildPos [2] = new Vector3 (1, 0, 0);
		baseOctreeBuildPos [3] = new Vector3 (1, 0, 1);
		baseOctreeBuildPos [4] = new Vector3 (0, 1, 0);
		baseOctreeBuildPos [5] = new Vector3 (0, 1, 1);
		baseOctreeBuildPos [6] = new Vector3 (1, 1, 0);
		baseOctreeBuildPos [7] = new Vector3 (1, 1, 1);
		
		// fill in the octree size vectors
		octreeSize = new Vector3[5];
		octreeSize [0] = Vector3.one * 128;
		octreeSize [1] = Vector3.one * 64;
		octreeSize [2] = Vector3.one * 32;
		octreeSize [3] = Vector3.one * 16;
		octreeSize [4] = Vector3.one * 8;
		// create this layer out until it fills the boundaries
		// positive big boxes 128x128
	
		// set the starting point
		nodeSize = 128;
		baseBuildPos = Vector3.one * nodeSize * 0.5f;
		buildPos = baseBuildPos;
		nextBuildPos = baseBuildPos;
		
		
		// build the first x, which is also first y and first z... :/
		// then fill the rest of that row up with z
		// then move up the y and fill that y coord with z
		
		
		for (int i = 0; nextBuildPos.x < positiveBoundary.x; i++) {
			
			
			buildPos.x = baseBuildPos.x + (i * nodeSize);
			buildPos.y = baseBuildPos.y;
			buildPos.z = baseBuildPos.z;
			nextBuildPos = buildPos + baseBuildPos;
			octree.Add (new OctreeNode (nodeSize, buildPos));
			
			for (int k = 1; nextBuildPos.z < positiveBoundary.z; k++) {
				buildPos.z = baseBuildPos.z + (k * nodeSize);
				nextBuildPos = buildPos + baseBuildPos;
				octree.Add (new OctreeNode (nodeSize, buildPos));
			}
			buildPos.z = baseBuildPos.z;
			
			for (int j = 1; nextBuildPos.y < positiveBoundary.y; j++) {
				
				buildPos.y = baseBuildPos.y + (j * nodeSize);
				buildPos.z = baseBuildPos.z;
				nextBuildPos = buildPos + baseBuildPos;
				octree.Add (new OctreeNode (nodeSize, buildPos));
				
				for (int k = 1; nextBuildPos.z < positiveBoundary.z; k++) {
					buildPos.z = baseBuildPos.z + (k * nodeSize);
					nextBuildPos = buildPos + baseBuildPos;
					octree.Add (new OctreeNode (nodeSize, buildPos));
				}
				buildPos.z = baseBuildPos.z;
			}
			
			
		}
		
		// now create the smaller boxes 64x64
		for (int a = 0; a < octree.Count; a++) {
			// create space for all of the new subgraphs
			octree [a].childNodes = new OctreeNode[8];
			// change the new node size
			nodeSize = 64;
			// reset the build position
			baseBuildPos = octree [a].position - (Vector3.one * nodeSize * 0.5f);
			// repeat the same process as above, fill in all da boxes
			for (int n = 0; n < 8; n++) {
				octreeBuildPos = baseBuildPos + (baseOctreeBuildPos [n] * nodeSize);
				octree [a].childNodes [n] = new OctreeNode (nodeSize, octreeBuildPos);
			}
			
			// now create the smaller boxes at 32x32
			for (int b = 0; b < octree [a].childNodes.Length; b++) {
				// create space for all of teh new subgraphs
				octree [a].childNodes [b].childNodes = new OctreeNode[8];
				// change the new node size
				nodeSize = 32;
				// reset the build position
				baseBuildPos = octree [a].childNodes [b].position - (Vector3.one * nodeSize * 0.5f);
				// repeat the same process as above, fill in all da boxes
				for (int n = 0; n < 8; n++) {
					octreeBuildPos = baseBuildPos + (baseOctreeBuildPos [n] * nodeSize);
					octree [a].childNodes [b].childNodes [n] = new OctreeNode (nodeSize, octreeBuildPos);
				}
				
				// now create the smaller boxes at 16x16
				for (int c = 0; c < octree [a].childNodes [b].childNodes.Length; c++) {
					// create space for all of teh new subgraphs
					octree [a].childNodes [b].childNodes [c].childNodes = new OctreeNode[8];
					// change the new node size
					nodeSize = 16;
					// reset the build position
					baseBuildPos = octree [a].childNodes [b].childNodes [c].position - (Vector3.one * nodeSize * 0.5f);
					// repeat the same process as above, fill in all da boxes
					for (int n = 0; n < 8; n++) {
						octreeBuildPos = baseBuildPos + (baseOctreeBuildPos [n] * nodeSize);
						octree [a].childNodes [b].childNodes [c].childNodes [n] = new OctreeNode (nodeSize, octreeBuildPos);
					}
					// now create the smaller boxes at 8x8
					for (int d = 0; d < octree [a].childNodes [b].childNodes [c].childNodes.Length; d++) {
						// create space for all of teh new subgraphs
						octree [a].childNodes [b].childNodes [c].childNodes [d].childNodes = new OctreeNode[8];
						// change the new node size
						nodeSize = 8;
						// reset the build position
						baseBuildPos = octree [a].childNodes [b].childNodes [c].childNodes [d].position - (Vector3.one * nodeSize * 0.5f);
						// repeat the same process as above, fill in all da boxes
						for (int n = 0; n < 8; n++) {
							octreeBuildPos = baseBuildPos + (baseOctreeBuildPos [n] * nodeSize);
							octree [a].childNodes [b].childNodes [c].childNodes [d].childNodes [n] = new BaseOctreeNode (nodeSize, octreeBuildPos);
							baseOctreeNodes.Add ((BaseOctreeNode)octree [a].childNodes [b].childNodes [c].childNodes [d].childNodes [n]);
						}
					}
				}	
			}
		}

		// TODO connect up the octree segments since its now possible to be in a location where no nodes exist.

		
	}

	IEnumerator CreateNodeGrid ()
	{
		// create the grid to hold the nodes, this is so that the neighbours can be found.
		// TODO
		// possibly remove the generic node array, and use this instead, it may be a better one...
		// calculate the size of the new Grid;
		int gridSizeX = (int)((maxNodeBoundary - minNodeBoundary).x / nodeBuildInterval);
		int gridSizeZ = (int)((maxNodeBoundary - minNodeBoundary).z / nodeBuildInterval);
		// since nodes are created at the very boundary of the grid, we actually have 1 extra row.
		// e.g. 40/5 = 8
		// 5,10,15,20,25,30,35,40
		// but 0 is in there too...
		// so that makes 9 numbers... :/
		nodeGrid = new NodeGridSegment[gridSizeX + 1, gridSizeZ + 1];
		// poulate the grid segment array
		for (int i = 0; i < nodeGrid.GetLength (0); i++) {
			for (int k = 0; k < nodeGrid.GetLength (1); k++) {
				nodeGrid [i, k] = new NodeGridSegment ();
			}
		}
		

		// set the minimum build positions
		nodeBuildPos.x = minNodeBoundary.x;
		nodeBuildPos.z = minNodeBoundary.z;
		RaycastHit[] rayHits;
		
		for (int i = 0; nodeBuildPos.x <= maxNodeBoundary.x; i++) {
			nodeBuildPos.z = minNodeBoundary.z;
			for (int k = 0; nodeBuildPos.z <= maxNodeBoundary.z; k++) {
				// now we are in the right place, raycast down from the top of the world and get all the points that it hits
				Vector3 rayStartPos = nodeBuildPos;
				rayStartPos.y = maxNodeBoundary.y;
				float rayDist = (maxNodeBoundary.y - minNodeBoundary.y) + 1;
				// raycast all the way down, to see what we hit
				//rayHits = Physics.RaycastAll(rayStartPos,Vector3.down,rayDist,nodeBuildLayers);
				rayHits = GetAllRaycastHits (rayStartPos, Vector3.down, rayDist, nodeBuildLayers);
				//Debug.Log ("num hits:" + rayHits.Length);
				//yield return new WaitForEndOfFrame();
				// iterate over any hits, and build nodes there
				for (int n = 0; n < rayHits.Length; n++) {
					// we want the nodes out of the floor, so build them 1 above the surface
					// check the position of the node, to see if it is valid before building
					if (IsValidNodePos (rayHits [n].point + Vector3.up, nodeRadius)) {
						Node newNode = new Node (rayHits [n].point + Vector3.up, rayHits [n].normal);
						newNodes.Add (newNode);
						nodeGrid [i, k].nodeList.Add (newNode);
					}
				}
				/*Node newNode = new Node(nodeBuildPos);
				newNodes.Add (newNode);
				nodeGrid[i,k].nodeList.Add(newNode);*/	
				// increase the build position
				nodeBuildPos.z += nodeBuildInterval;
			}
			yield return new WaitForEndOfFrame ();
			nodeBuildPos.x += nodeBuildInterval;
		}
		
		nodeGraph = newNodes.ToArray ();
		// clear the new node list
		newNodes.Clear ();
		print ("number of nodes = " + nodeGraph.Length);
		creatingNodeGrid = false;
	}

	bool IsValidNodePos (Vector3 pos, float _nodeRadius)
	{
		// check for anything near the node, that may stop it from being built
		if (Physics.CheckSphere (pos, _nodeRadius)) {
			return false;
		} else {
			return true;
		}
	}

	void SetNodeNeighbours ()
	{
		// iterate over everything in the 2D node array
		for (int i = 0; i < nodeGrid.GetLength (0); i++) {
			for (int k = 0; k < nodeGrid.GetLength (1); k++) {
				// iterate over everything in each list
				//print (nodeGrid[i,k].nodeList.Count);
				//print (i+","+k);
				for (int n = 0; n < nodeGrid [i, k].nodeList.Count; n++) {
					// get next neighbour position
					// could be done with 8...
					// 4 main neighbours, up, down, left, right
					// assuming that array starts top left...
					Node currentNode = nodeGrid [i, k].nodeList [n];
					//print ("current node is "+currentNode);
					// now we get all of the nodes at each of the neighbour lists
					// check each node at our neighbours 1 by 1
					List<Node> conNodes;
					
					// UP CHECK
					Vector2 upNeighbourPos = new Vector2 (i, k - 1);
					// check if it is out of bounds
					if (CheckNodeOutOfGridBounds (upNeighbourPos) == false) {
						// get the nodes at that position
						conNodes = nodeGrid [(int)upNeighbourPos.x, (int)upNeighbourPos.y].nodeList;
						for (int a = 0; a < conNodes.Count; a++) {
							if (ExtensionMethods.IsWithinRange (currentNode.position.y, conNodes [a].position.y, distanceToConnect)) {
								if (Physics.Linecast (currentNode.position, conNodes [a].position, nodeBuildLayers) == false) {
									currentNode.AddConnection (conNodes [a]);
								}
							}
						}
					}
					
					// DOWN CHECK
					Vector2 downNeighbourPos = new Vector2 (i, k + 1);
					// check if it is out of bounds
					if (CheckNodeOutOfGridBounds (downNeighbourPos) == false) {
						// get the nodes at that position
						conNodes = nodeGrid [(int)downNeighbourPos.x, (int)downNeighbourPos.y].nodeList;
						for (int a = 0; a < conNodes.Count; a++) {
							if (ExtensionMethods.IsWithinRange (currentNode.position.y, conNodes [a].position.y, distanceToConnect)) {
								if (Physics.Linecast (currentNode.position, conNodes [a].position, nodeBuildLayers) == false) {
									currentNode.AddConnection (conNodes [a]);
								}
							}
						}
					}
					
					// LEFT CHECK
					Vector2 leftNeighbourPos = new Vector2 (i - 1, k);
					// check if it is out of bounds
					if (CheckNodeOutOfGridBounds (leftNeighbourPos) == false) {
						// get the nodes at that position
						conNodes = nodeGrid [(int)leftNeighbourPos.x, (int)leftNeighbourPos.y].nodeList;
						for (int a = 0; a < conNodes.Count; a++) {
							if (ExtensionMethods.IsWithinRange (currentNode.position.y, conNodes [a].position.y, distanceToConnect)) {
								if (Physics.Linecast (currentNode.position, conNodes [a].position, nodeBuildLayers) == false) {
									currentNode.AddConnection (conNodes [a]);
								}
							}
						}
					}
					
					// RIGHT CHECK
					Vector2 rightNeighbourPos = new Vector2 (i + 1, k);
					// check if it is out of bounds
					if (CheckNodeOutOfGridBounds (rightNeighbourPos) == false) {
						// get the nodes at that position
						conNodes = nodeGrid [(int)rightNeighbourPos.x, (int)rightNeighbourPos.y].nodeList;
						for (int a = 0; a < conNodes.Count; a++) {
							if (ExtensionMethods.IsWithinRange (currentNode.position.y, conNodes [a].position.y, distanceToConnect)) {
								if (Physics.Linecast (currentNode.position, conNodes [a].position, nodeBuildLayers) == false) {
									currentNode.AddConnection (conNodes [a]);
								}
							}
						}
					}
					//print (currentNode.connections.Length);
				}
			}
		}		
	}


	IEnumerator OptimiseNavGraph ()
	{
		// variables used
		List<Node> checkNodes = new List<Node> ();
		#region STAGE 1 OPTIMISATION
		// REMOVING REDUNDANT AREA NODES

		newNodeList.AddRange (nodeGraph);
		// first stage, mark all the nodes with a classification based on the number of connections they have
		for (int i = 0; i < nodeGraph.Length; i++) {
			// get the current node
			currentNode = nodeGraph [i];
			// check for anything that isn't coplanar. since we should probably keep these so that we don't lose much geometry.
			if (IsCoplanar (ref currentNode) == false) {
				currentNode.nodeClass = NodeClass.required;
			} else {
				if (currentNode.connections.Count < 2) {
					currentNode.nodeClass = NodeClass.required;
				} else if (currentNode.connections.Count < 4) {
					if (IsCollinear (ref currentNode) == false) {
						// detect the corners...
						if (currentNode.connections.Count == 2) {
							currentNode.nodeClass = NodeClass.required;
							currentNode.nodeType = NodeType.corner;
						} else {
							currentNode.nodeClass = NodeClass.important;
						}
					} else {
						currentNode.nodeClass = NodeClass.none;
						// add this node to a list of nodes that can be collapsed
						checkNodes.Add (currentNode);
					}
				} else {
					currentNode.nodeClass = NodeClass.none;
					// add this node to a list of nodes that can be collapsed
					checkNodes.Add (currentNode);
				}
			}
		}
		#region old code
		/*
		for (int i = 0; i < nodeGraph.Length; i++) {
			// get the current node
			currentNode = nodeGraph [i];

			// check their neighbours, and find the close ones.
			Node tempNode = new Node(currentNode.position,currentNode.normal);
			foreach(Node neighbour in currentNode.connections){
				// check the distance to the neighbour
				float dst = (distanceToConnect*distanceToConnect) +0.1f- (neighbour.node.position-currentNode.position).sqrMagnitude;
				// only add this connection if the neighbour is close enough
				if(dst > 0){
					tempNode.AddConnection(neighbour);
				}
			}
			// check how many connections the temp node has...
			// must keep a node if...
			// - no close connections
			// all the connections aren't coplanar
			if(currentNode.connections.Length == 0 || IsCoplanar(ref currentNode) == false){
				currentNode.nodeClass = NodeClass.required;
			}
			// if it has only 1 close connection, it is important
			else if(currentNode.connections.Length == 1){
				currentNode.nodeClass = NodeClass.important;
			}
			// corner checking
			else if(currentNode.connections.Length == 2){
				// if its not collinear, then we need this node
				if(IsCollinear(ref currentNode) == false){
					currentNode.nodeClass = NodeClass.required;
				}
				// we don't need a collinear node
				else{
					currentNode.nodeClass = NodeClass.none;
					// add this node to a list of nodes that can be collapsed
					checkNodes.Add (currentNode);
				}
			}
			// 3 or more connections, these won't be collinear, so try and collapse this node.
			else{
				currentNode.nodeClass = NodeClass.none;
				// add this node to a list of nodes that can be collapsed
				checkNodes.Add (currentNode);
			}
			// set the temporary node for garbage collection
			//tempNode.KillNode();
		}
		*/
		#endregion
		yield return new WaitForEndOfFrame ();
		int count = 0;
		LinkedList<Node> nodesToKill = new LinkedList<Node> ();
		// collapse all these nodes
		for (int i = 0; i < checkNodes.Count; i++) {
			// get the current node
			currentNode = checkNodes [i];
			// do we keep this node?
			bool keepNode = false;
			// check all connection visibility
			foreach (NavConnectionData neighbour in currentNode.connections) {
				foreach (NavConnectionData rayNeighbour in currentNode.connections) {
					if (neighbour != rayNeighbour) {
						//print (neighbour+" at position "+neighbour.node.position);
						//print (rayNeighbour+" at position "+rayNeighbour.node.position);
						/*Ray ray = new Ray(neighbour.node.position,rayNeighbour.node.position-neighbour.node.position);
						float rayDist = (rayNeighbour.node.position-neighbour.node.position).magnitude;

						if (Physics.SphereCast (ray,nodeWalkWidth,rayDist,nodeBuildLayers)) {*/
						if (Physics.Linecast (neighbour.node.position, rayNeighbour.node.position, nodeBuildLayers)) {
							Debug.DrawRay (neighbour.node.position, rayNeighbour.node.position - neighbour.node.position, Color.red, 1);
							// one of our neighbours can't see one of our other neighbours, we need this node to maintain coverage of the map.
							currentNode.nodeClass = NodeClass.required;
							// stop the loops
							keepNode = true;
						}
						if (keepNode == true) {
							break;
						}
					}
				}

				if (keepNode == true) {
					break;
				}
				//yield return new WaitForEndOfFrame ();

			}
			//yield return new WaitForEndOfFrame ();

			// check if we need to keep the node. if so... move it off the "kill list"
			//if(keepNode == true){
			//	checkNodes.Remove(currentNode);
			//}
			// check over the kill list, and kill these nodes
			//for(int i = 0;i<checkNodes.Count;i++){
			// all these nodes need collapsing
			//currentNode = checkNodes[i];
			//Debug.Log("collapsing node");
			if (keepNode == false) {
				nodesToKill.AddLast (currentNode);
				#if true
				foreach (NavConnectionData neighbour in currentNode.connections) {
					// remove the current node

					neighbour.node.RemoveConnection (currentNode);

					// THIS CAUSES REALLY SLOW STUFF ON LARGE MAPS
					// add all the new neighbours
					foreach (NavConnectionData newNeighbour in currentNode.connections) {
						// dont add ourselves
						if (neighbour != newNeighbour) {
							// add new connection
							neighbour.node.AddConnection (newNeighbour.node);
						}
					}
					//yield return new WaitForEndOfFrame ();

				}
				// remove the current node from the list
				currentNode.KillNode ();
				newNodeList.Remove (currentNode);
				#endif
				//yield return new WaitForEndOfFrame ();
				//Debug.Log (i);
			}
			count++;
			if (count > 10) {
				yield return new WaitForEndOfFrame ();
				count = 0;
			}
		}

		count = 0;
		// REMOVE ALL THE MARKED FOR DEATH NODES
		foreach (Node node in nodesToKill) {
			newNodeList.Remove (node);
			foreach (NavConnectionData neighbour in currentNode.connections) {
				// remove the current node
				neighbour.node.RemoveConnection (currentNode);
			}
			node.KillNode ();
			count++;
			if (count > 20) {
				yield return new WaitForEndOfFrame ();
				count = 0;
			}

		}

		nodeGraph = newNodeList.ToArray ();
		Debug.Log ("1st stage optimisation complete!");
		Debug.Log ("number of nodes = " + nodeGraph.Length);
		#endregion
		#if false
		#region STAGE 1 RECONNECT
		// RECONNECT ALL NODES
		Debug.Log ("reconnecting after 1st stage optimisation..");
		for (int i = 0; i < nodeGraph.Length; i++) {
			Node node = nodeGraph [i];
			for (int j = 1; j < nodeGraph.Length; j++) {
				if (CheckWalkable (node, nodeGraph [j])) {
					node.AddConnection (nodeGraph [j]);
					nodeGraph [j].AddConnection (node);
				}
			}
			yield return new WaitForEndOfFrame ();
		}

		// CLEAN UP UNNECESSARY CONNECTIONS
		for (int i = 0; i < nodeGraph.Length; i++) {
			Node node = nodeGraph [i];
			for (int j = 0; j < node.connections.Length; j++) {

			}
		}
		Debug.Log ("reconnection complete!");
		#endregion
		#endif
		// clear the variables
		checkNodes.Clear ();
		#region STAGE 2 OPTIMISATION


		// REMOVE REDUNDANT WALL NODES

		// go back over everything we've got, and check if we have created any more required nodes...
		for (int i = 0; i < nodeGraph.Length; i++) {
			currentNode = nodeGraph [i];
			// do we keep this node?
			bool keepNode = false;
			// check all connection visibility
			foreach (NavConnectionData neighbour in currentNode.connections) {
				foreach (NavConnectionData rayNeighbour in currentNode.connections) {
					if (neighbour != rayNeighbour) {
						//print (neighbour+" at position "+neighbour.node.position);
						//print (rayNeighbour+" at position "+rayNeighbour.node.position);
						if (Physics.Linecast (neighbour.node.position, rayNeighbour.node.position, nodeBuildLayers)) {
							// one of our neighbours can't see one of our other neighbours, we need this node to maintain coverage of the map.
							currentNode.nodeClass = NodeClass.required;
							Debug.DrawLine (neighbour.node.position, rayNeighbour.node.position, Color.red, 3);
							Debug.DrawLine (currentNode.position, rayNeighbour.node.position, Color.green, 3);
							Debug.DrawLine (currentNode.position, neighbour.node.position, Color.green, 3);
							// stop the loops
							keepNode = true;
						}
						if (keepNode == true) {
							break;
						}
					}
				}

				if (keepNode == true) {
					break;
				}
				//yield return new WaitForEndOfFrame ();
			}
		}
		yield return new WaitForEndOfFrame ();
		// collect all the "important" nodes
		for (int i = 0; i < nodeGraph.Length; i++) {
			// get the current node
			currentNode = nodeGraph [i];

			if (nodeGraph [i].nodeClass == NodeClass.important) {
				// check their neighbours, and find the close ones.
				Node tempNode = new Node (currentNode.position, currentNode.normal);
				foreach (NavConnectionData neighbour in currentNode.connections) {
					// check the distance to the neighbour
					float dst = (distanceToConnect * distanceToConnect) + 0.1f - (neighbour.node.position - currentNode.position).sqrMagnitude;
					// only add this connection if the neighbour is close enough
					if (dst > 0) {
						tempNode.AddConnection (neighbour.node);
					}
				}
				// check how many connections the temp node has...
				// if it has no close connections, then this node MUST be kept
				if (tempNode.connections.Count == 0) {
					currentNode.nodeClass = NodeClass.required;
				}
				// if it has only 1 close connection, it is important
				else if (tempNode.connections.Count == 1) {
					currentNode.nodeClass = NodeClass.important;
				} else {
					// now determine if the near nodes are collinear...
					if (IsCollinear (ref tempNode)) {
						// this node is now "marked for death"
						currentNode.nodeClass = NodeClass.none;
						// we want to try and shorten down collinear nodes
						checkNodes.Add (currentNode);
					}
					// check if this node only has 2 close connections...
					else if (tempNode.connections.Count == 2) {
						// only 2 close connections? AND its not collinear?
						// i spy a corner...
						currentNode.nodeClass = NodeClass.required;
					}
				}
				// set the temporary node for garbage collection
				tempNode.KillNode ();
			}
		}

		// now we have all the nodes along walls, let's try and remove them.

		// collapse all these nodes
		for (int i = 0; i < checkNodes.Count; i++) {
			// get the current node
			currentNode = checkNodes [i];
			// do we keep this node?
			bool keepNode = false;
			// check all connection visibility
			foreach (NavConnectionData neighbour in currentNode.connections) {
				foreach (NavConnectionData rayNeighbour in currentNode.connections) {
					if (neighbour != rayNeighbour) {
						//print (neighbour+" at position "+neighbour.node.position);
						//print (rayNeighbour+" at position "+rayneighbour.node.position);
						if (Physics.Linecast (neighbour.node.position, rayNeighbour.node.position, nodeBuildLayers)) {
							// one of our neighbours can't see one of our other neighbours, we need this node to maintain coverage of the map.
							currentNode.nodeClass = NodeClass.required;
							// stop the loops
							keepNode = true;
						}
						if (keepNode == true) {
							break;
						}
					}
				}

				if (keepNode == true) {
					break;
				}
				//yield return new WaitForEndOfFrame ();

			}
			yield return new WaitForEndOfFrame ();

			// now collapse the node if we don't need to keep it
			if (keepNode == false) {
				//Debug.Log("collapsing node");
				foreach (NavConnectionData neighbour in currentNode.connections) {
					// remove the current node
					neighbour.node.RemoveConnection (currentNode);
					// add all the new neighbours
					foreach (NavConnectionData newNeighbour in currentNode.connections) {
						// dont add ourselves
						if (neighbour != newNeighbour) {
							// add new connection
							neighbour.node.AddConnection (newNeighbour.node);

						}
					}
					//yield return new WaitForEndOfFrame ();
				}
				// remove the current node from the list
				currentNode.KillNode ();
				newNodeList.Remove (currentNode);
			}
		}
		nodeGraph = newNodeList.ToArray ();
		Debug.Log ("stage 2 optimisation completed!");
		Debug.Log ("number of nodes = " + nodeGraph.Length);
		#endregion
		checkNodes.Clear ();
		#region STAGE 3 OPTIMISATION
		// find all the newly required nodes
		// reclassify every node.
		for (int i = 0; i < nodeGraph.Length; i++) {
			// get the current node
			currentNode = nodeGraph [i];

			// check their neighbours, and find the close ones.
			Node tempNode = new Node (currentNode.position, currentNode.normal);
			foreach (NavConnectionData neighbour in currentNode.connections) {
				// check the distance to the neighbour
				float dst = (distanceToConnect * distanceToConnect) + 0.1f - (neighbour.node.position - currentNode.position).sqrMagnitude;
				// only add this connection if the neighbour is close enough
				if (dst > 0) {
					tempNode.AddConnection (neighbour);
				}
			}
			// check how many connections the temp node has...
			// must keep a node if...
			// - no close connections
			// all the connections aren't coplanar
			if (tempNode.connections.Count == 0 || IsCoplanar (ref currentNode) == false) {
				currentNode.nodeClass = NodeClass.required;
			}
			// if it has only 1 close connection, it is important
			else if (tempNode.connections.Count == 1) {
				currentNode.nodeClass = NodeClass.required;
			}
			// corner checking
			else if (tempNode.connections.Count == 2) {
				// if its not collinear, then we need this node
				if (IsCollinear (ref tempNode) == false) {
					currentNode.nodeClass = NodeClass.required;
				}
				// we don't need a collinear node
				else {
					currentNode.nodeClass = NodeClass.none;
					checkNodes.Add (currentNode);
				}
			}
			// 3 or more connections, these won't be collinear, so try and collapse this node.
			else {
				currentNode.nodeClass = NodeClass.none;
				checkNodes.Add (currentNode);
			}
			// set the temporary node for garbage collection
			tempNode.KillNode ();
		}

		// now collapse all the nodes to check

		for (int i = 0; i < checkNodes.Count; i++) {
			// get the current node
			currentNode = checkNodes [i];
			// do we keep this node?
			bool keepNode = false;
			// check all connection visibility
			foreach (NavConnectionData neighbour in currentNode.connections) {
				foreach (NavConnectionData rayNeighbour in currentNode.connections) {
					if (neighbour != rayNeighbour) {
						//print (neighbour+" at position "+neighbour.node.position);
						//print (rayNeighbour+" at position "+rayneighbour.node.position);
						if (Physics.Linecast (neighbour.node.position, rayNeighbour.node.position, nodeBuildLayers)) {
							// one of our neighbours can't see one of our other neighbours, we need this node to maintain coverage of the map.
							currentNode.nodeClass = NodeClass.required;
							// stop the loops
							keepNode = true;
						}
						if (keepNode == true) {
							break;
						}
					}
				}

				if (keepNode == true) {
					break;
				}
				//yield return new WaitForEndOfFrame ();

			}
			yield return new WaitForEndOfFrame ();

			// now collapse the node if we don't need to keep it
			if (keepNode == false) {
				//Debug.Log("collapsing node");
				foreach (Node neighbour in currentNode.connections) {
					// remove the current node
					neighbour.RemoveConnection (currentNode);
					// add all the new neighbours
					foreach (Node newNeighbour in currentNode.connections) {
						// dont add ourselves
						if (neighbour != newNeighbour) {
							// add new connection
							neighbour.AddConnection (newNeighbour);

						}
					}
					//yield return new WaitForEndOfFrame ();
				}
				// remove the current node from the list
				currentNode.KillNode ();
				newNodeList.Remove (currentNode);
			}
		}

		nodeGraph = newNodeList.ToArray ();
		Debug.Log ("stage 3 optimisation completed!");
		Debug.Log ("number of nodes = " + nodeGraph.Length);
		#endregion
		// this is now a workable nav-mesh

		#region STAGE 4 OPTIMISING
		// Remove all connections to dead nodes.
		int counter = 0;
		for (int i = 0; i < nodeGraph.Length; i++) {
			currentNode = nodeGraph [i];
			foreach (Node connection in currentNode.connections) {
				if (connection.isDead == true) {
					currentNode.RemoveConnection (connection);
					i--;
					counter++;
				}
			}
		}
		print ("removed dead connections:" + counter);
		yield return new WaitForEndOfFrame ();
		// now ensure all connections are two way
		bool didConnect = false;
		int connectionsMade = 0;
		foreach (Node nd in nodeGraph) {
			// go over all the connections it's got
			foreach (Node conn in nd.connections) {
				// add ourselves as a connection.
				// it doesn't add duplicates...
				didConnect = conn.AddConnection (nd);
				if (didConnect == true) {
					connectionsMade++;
				}
			}
		}
		print ("made new connections:" + connectionsMade);

		// TODO MERGING NODES THAT ARE NEXT TO EACH OTHER
		// only minor gains will be made here...
		#endregion

		// done optimising!
		optimisingNavGraph = false;

	}

	bool CheckNodeOutOfGridBounds (Vector2 pos)
	{
		// if the length is 40... element 40 does not exist
		if (pos.x < 0 || pos.x >= nodeGrid.GetLength (0)) {
			return true;
		} else if (pos.y < 0 || pos.y >= nodeGrid.GetLength (1)) {
			return true;
		} else {
			return false;
		}
		//print ("checking for position "+pos.ToString());
	}

	IEnumerator RecalculateNavGraph ()
	{
		
		// determine which node has the player
		// check for player nodes
		//yield return new WaitForSeconds(5);
		ClearNavGraph ();
		yield return new WaitForEndOfFrame ();
		//print ("finding player node");
		FindPlayerNode ();
		numOfTimesCalled++;
		//yield return new WaitForSeconds(1);
		waitForNextUpdate = false;
		isDoneUpdating = false;
		// keep updating until no nodes need updating
		while (isDoneUpdating == false) {
			// wait for the next update
			while (waitForNextUpdate == true) { 
				yield return new WaitForEndOfFrame ();
			}
			waitForNextUpdate = true;
			//print("starting coroutine");
			StartCoroutine (UpdateNavGraph ());
			yield return new WaitForEndOfFrame ();
			
			
		}
		yield return new WaitForEndOfFrame ();
		navGraphUpdated = true;
	}

	void ClearNavGraph ()
	{
		for (int i = 0; i < nodeGraph.Length; i++) {
			nodeGraph [i].isChecked = false;
			//nodeGraph[i].targetNode = null;
			nodeGraph [i].hasPlayerConnection = false;
			nodeGraph [i].distToPlayer = 0;
		}
		nodesToUpdate.Clear ();
		nextNodesToUpdate.Clear ();
	}

	void FindPlayerNode ()
	{
		// get the nearest octet
		playerPos = player.position;
		BaseOctreeNode baseOctet = GetBaseOctetNearestToPosition (playerPos);
		
		// set the first node in the octet as the nearest node
		float distToPlayer = (playerPos - baseOctet.nodes [0].position).sqrMagnitude;
		float newDistToPlayer = distToPlayer;
		int startIndex = 0;
		// search this octet for the nearest node
		for (int i = 1; i < baseOctet.nodes.Count; i++) {
			// get distance to player
			newDistToPlayer = (playerPos - baseOctet.nodes [i].position).sqrMagnitude;
			// check if its shorter than the last distance
			if (newDistToPlayer < distToPlayer) {
				// record this as the target node
				startIndex = i;
				distToPlayer = newDistToPlayer;
			}
			// TODO
			// sort into ascending order
			// raycast each one individually to determine if one can be reached
			
			// set the start node to the closest
		}
		
		startNode = baseOctet.nodes [startIndex];
		startNode.distToPlayer = Mathf.Sqrt (distToPlayer);
		startNode.hasPlayerConnection = true;
		startNode.isChecked = true;
		// add the start node to the update
		nextNodesToUpdate.Add (startNode);
		
		
	}

	BaseOctreeNode GetBaseOctetNearestToPosition (Vector3 pos)
	{
		octetSize = 128;
		// find the octree part that the node belongs to
		// create all of the scaled positions
		for (int i = 0; i < 5; i++) {
			scaledPos [i] = ExtensionMethods.RoundVector3NonZeroInt (pos, octetSize) - (octreeSize [i] * 0.5f);
			octetSize = octetSize / 2;
		}
		
		
		// find the first octree position
		nextOctreeNode = octree [0];
		for (int i = 0; i < octree.Count; i++) {
			if (octree [i].position == scaledPos [0]) {
				// we have found an octree to start searching
				nextOctreeNode = octree [i];
				// stop the loop
				i = octree.Count;
			}
		}
		
		// check if the position is out of bounds
		if (nextOctreeNode == null) {
			return null;
		}
		////print ("first octree node is "+nextOctreeNode);
		// iterate over all of the remaining octets until we find the bottom one
		for (int a = 1; a < 5; a++) {
				
			nextOctreeNode = GetNearestOctreeLayer (nextOctreeNode.childNodes, scaledPos [a]);
			if (nextOctreeNode == null) {
				return null;
			}
			////print ("next octree node is "+nextOctreeNode);
			
		}
		// return the base octree node
		return (BaseOctreeNode)nextOctreeNode;
		
	}

	IEnumerator UpdateNavGraph ()
	{
		// get the next nodes to update
		// clear the old nodes
		//print ("clearing arrays");
		//print ("number of nodes to clear "+nodesToUpdate.Count);
		//print ("number of next nodes to clear "+nextNodesToUpdate.Count);
		nodesToUpdate.Clear ();
		// set the new nodes
		nodesToUpdate = new List<Node> (nextNodesToUpdate);
		// clear the next nodes ready for a new set
		nextNodesToUpdate.Clear ();
		yield return new WaitForEndOfFrame ();
		//print ("cleared arrays");
		//print ("number of nodes to update "+nodesToUpdate.Count);
		//print ("number of next nodes to update "+nextNodesToUpdate.Count);
		//yield return new WaitForSeconds(3);
		// BEGIN UPDATE
		// update all the nodes first...
		// then add any non-connected ones to the arrays to check
		for (int i = 0; i < nodesToUpdate.Count; i++) {
			//print ("checking neighbours for player connections");
			// find neighbours with player connections
			// get the current node we are checking
			currentNode = nodesToUpdate [i];
			float minDistToPlayer = float.PositiveInfinity;
			targetNodeIndex = 0;
			bool foundPlayer = false;
			Node targetNode = currentNode.connections.First.Value;
			// iterate over the connections
			foreach (NavConnectionData data in currentNode.connections) {
				// check for a player connection
				if (data.node.hasPlayerConnection == true) {
					// work out distance that we'd have to go to this node to get to our player
					float newDistToPlayer = data.node.distToPlayer + data.distance;
					// check if this node is closer than the other nodes
					if (newDistToPlayer < minDistToPlayer) {
						// set the minimum dist to the player
						currentNode.distToPlayer = newDistToPlayer;
						minDistToPlayer = newDistToPlayer;
						// set the taret node index
						targetNode = data.node;
						// we found our player
						foundPlayer = true;
					}
				}
			}
			
			// set the data back to the current node
			currentNode.targetNode = targetNode;
			if (foundPlayer) {
				currentNode.hasPlayerConnection = true;
			}
		}
		//print ("updated player connections");
		yield return new WaitForEndOfFrame ();
		// now add any remaining neighbours that dont have a player connection
		for (int i = 0; i < nodesToUpdate.Count; i++) {
			currentNode = nodesToUpdate [i];
			// add any remaining neighbour nodes that do not already have a player connection to the list to check next time
			foreach (NavConnectionData conData in currentNode.connections) {
				// check if they are already on the list, since we do NOT want to add them twice
				if (conData.node.isChecked == false) {
					conData.node.isChecked = true;
					// if they arent on the list, then check if they have a player connection
					// if no player connection, add them to the list
					if (conData.node.hasPlayerConnection == false) {
						nextNodesToUpdate.Add (conData.node);
					}
				}
			}
			
		}
		//print ("after algorithm");
		//print ("number of nodes to update "+nodesToUpdate.Count);
		//print ("number of next nodes to update "+nextNodesToUpdate.Count);
		
		// check to stop the algorithm
		if (nextNodesToUpdate.Count == 0) {
			isDoneUpdating = true;
		}
		waitForNextUpdate = false;
	}

	static bool IsCoplanar (ref Node nd)
	{
		// TODO possibly just compare the normals of the nodes...
		// with 1 connections or less, the node must be coplanar
		if (nd.connections.Count < 2) {
			return true;
		}
		// work out the first direction vector, 
		Vector3 v1 = (nd.connections.First.Value.node.position - nd.position);
		// cross this with the normal to find the normal of the plane
		Vector3 v2 = Vector3.Cross (nd.normal, v1);
		// work out the normal vector of the plane
		Vector3 coplanarVect = Vector3.Cross (v1, v2);
		//Debug.DrawRay (nd.position, coplanarVect, Color.red, 2);
		// all other vectors should dot with the coplanar vector at ~= 0
		LinkedListNode<NavConnectionData> nextNode = nd.connections.First.Next;
		while (nextNode.Next != null) {
			v1 = (nextNode.Value.node.position - nd.position);
			float dot = Vector3.Dot (v1, coplanarVect);
			// roughly coplanar... check if not roughly coplanar
			if (dot < -0.05f || dot > 0.05f) {
				// stop the loop, this is not coplanar
				return false;
			}
			nextNode = nextNode.Next;
		}
		// otherwise it must be coplanar
		return true;
	}


	static bool IsCollinear (ref Node nd)
	{
		// always collinear with 1 connection.
		if (nd.connections.Count < 2) {
			return true;
		}
		Vector3 v1 = nd.connections.First.Value.node.position - nd.position;
		LinkedListNode<NavConnectionData> nextNode = nd.connections.First.Next;
		while (nextNode.Next != null) {
			// the second direction vector
			Vector3 v2 = nextNode.Value.node.position - nd.position;
			// dot these, and they should be close to -1...
			float dot = Vector3.Dot (v1, v2);
			if (dot > -0.95f) {
				return false;
			}
			nextNode = nextNode.Next;
		}
		// otherwise must be collinear
		return true;
	}

	RaycastHit[] GetAllRaycastHits (Vector3 _startPos, Vector3 _dir, float _rayDist, LayerMask _layers)
	{
		List<RaycastHit> hits = new List<RaycastHit> ();
		int iterations = 0;
		Vector3 endPos = _startPos + _dir * _rayDist;
		while (_rayDist > 0 && iterations < 10) {
			iterations++;
			RaycastHit hit = new RaycastHit ();
			Physics.Raycast (_startPos, _dir, out hit, _rayDist, _layers);
			//Physics.Linecast(_startPos,endPos,out hit,_layers);
			//Physics.SphereCast (_startPos, 0.1f,_dir, out hit, _rayDist, _layers);
			/*if (iterations > 1) {
				Debug.DrawRay (_startPos, _dir * _rayDist, Color.cyan, 0.33f);
			}*/
			// check if we hit anything
			if (hit.collider != null) {
				_rayDist -= hit.distance;
				_startPos = hit.point + _dir * floorWidth;
				//Debug.Log (_startPos);
				hits.Add (hit);
			} else {
				_rayDist = 0;
			}
			//Debug.Log ("iteration:" + iterations + "_dist:" + _rayDist+"_point:"+hit.point);

		}
		return hits.ToArray ();
	}

	private bool CheckWalkable (Node a, Node b)
	{
		return !Physics.Linecast (a.position, b.position, nodeBuildLayers);
	}
}
