using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour {

	public static ProjectilePool singleton;

	public Transform projectileStore;
	public GameObject laserProjectilePrefab;
	public List<LaserProjectile> laserProjectiles = new List<LaserProjectile>();
	public int nextFreeLaserIndex;
	public int activeLaserCount;
	public int laserStoreSize;
	public int laserGrowThreshold;

	void Awake() {
		singleton = this;
	}

	// Use this for initialization
	void Start () {
		CreateBaseProjectiles();
	}

	void CreateBaseProjectiles(){
		CreateLaserProjectiles (15);
		// allow 5 projectiles spare
		laserGrowThreshold = 10;
	}

	// Update is called once per frame
	void Update () {
	
	}

	#region LASER PROJECTILES
	void CreateLaserProjectiles (int amount){
		for (int i = 0; i < amount; i++) {
			GameObject newProj = (GameObject)Instantiate (laserProjectilePrefab, projectileStore.position, projectileStore.rotation);
			laserProjectiles.Add(newProj.GetComponent<LaserProjectile> ());
			newProj.SetActive (false);
		}
		laserStoreSize += amount;
		laserGrowThreshold += amount;
	}

	public void SpawnLaserProjectile(Vector3 _startPos, Vector3 _endPos){// increase the active counter by 1
		// TODO possibly abstract the firing out to here...
		activeLaserCount++;
		// get the next free laser in the pool
		laserProjectiles[nextFreeLaserIndex].thisGameObject.SetActive(true);
		laserProjectiles [nextFreeLaserIndex].FireProjectile (_startPos, _endPos);
		// now work out where the next free laser is...
		while (laserProjectiles[nextFreeLaserIndex].thisGameObject.activeSelf == true) {
			// advance the next free zombie
			nextFreeLaserIndex++;
			// loop back over the list
			if (nextFreeLaserIndex >= laserProjectiles.Count) {
				// back to the start, we've gone over the end
				nextFreeLaserIndex = 0;
			}
		}

		// check if we need to add more lasers :/
		if (activeLaserCount > laserGrowThreshold) {
			CreateLaserProjectiles (10);
		}
	}

	public void DestroyLaserProjectile () {
		singleton.activeLaserCount--;
	}
	#endregion
}
