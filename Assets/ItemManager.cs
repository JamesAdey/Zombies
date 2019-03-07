using UnityEngine;
using System.Collections;

public class ItemManager : MonoBehaviour {

	public ItemManager singleton;

	public GameObject[] pickupPrefabs;

	void Awake () {
		singleton = this;
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
