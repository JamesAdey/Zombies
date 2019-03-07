using UnityEngine;
using System.Collections;

public class ZombieManager : MonoBehaviour {

	public static ZombieManager singleton;

	public ZombieStats[] zombieStats;
	public Material[] zombieMaterials;
	public Vector3[] zombieScales;


	void Awake () {
		singleton = this;
	}

	public static ZombieStats GetStatsForType(int _typ){
		return singleton.zombieStats [_typ];
	}

	public static Material GetMaterialForType(int _typ){
		return singleton.zombieMaterials [_typ];
	}

	public static Vector3 GetScaleForType(int _typ){
		return singleton.zombieScales [_typ];
	}
}
