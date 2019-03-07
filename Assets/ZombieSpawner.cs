using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct ZombieWave {

	public int basicCount;
	public int fastCount;
	public int strongCount;
	public int fatCount;
	public int splitCount;
	public int ghostCount;
	public int boomCount;
	public int spewCount;
}

[System.Serializable]
public class ZombieSpawnPoint {
	public float spawnDelay = 3;
	public Transform spawnTransform;
	public Vector3 spawnSize = Vector3.one;
	public float spawnTimer;

	~ZombieSpawnPoint (){
		spawnTransform = null;
	}
}

[System.Serializable]
public class PoolZombie {
	public bool isAlive = false;
	public Zombie zombie = null;
	public GameObject zombieObj = null;

	public PoolZombie (bool _alive, Zombie _zombie, GameObject _zombieObject){
		isAlive = _alive;
		zombie = _zombie;
		zombieObj = _zombieObject;
	}

	~PoolZombie (){
		zombie = null;
		zombieObj = null;
	}
}

public class ZombieSpawner : MonoBehaviour {

	public static ZombieSpawner singleton;

	#region spawn pool vars
	List<PoolZombie> zombiePool = new List<PoolZombie>();
	public int nextFreeZombieIndex;
	public int activeZombieCount;
	public int storeSize;
	public int growThreshold;
	// TODO consider reshaping them and changing stats, since they all follow the same base?
	#endregion
	// the number of zombies to allow before the game stops spawning to maintain a high framerate.
	public int maxZombies;
	bool hasMaxZombies;
	public int totalMaxZombies = 400;
	public GameObject zombiePrefab;
	public Transform zombieStore;
	public ZombieSpawnPoint[] spawnPoints;
	ZombieSpawnPoint currentSpawnPoint;
	public ZombieWave currentWave = new ZombieWave();
	// spawning variables
	ZombieType spawnType;
	// TODO make this automatic
	public bool hasZombiesRemaining;
	// Framerate monitoring variables
	int frames;
	float timer;
	int fps;
	float lowFpsTimer;

	void Awake () {
		singleton = this;
	}

	void Start () {
		// TODO possibly change this...
		CreateZombies (10);
		// reset the growth threshold after creating them
		growThreshold = 5;
		// TODO change this..
		// for now, the maximum zombie is the same as the total max zombies.
		maxZombies = totalMaxZombies;
	}

	void OnDrawGizmos () {
		// draw all the spawn points
		if (spawnPoints.Length > 0) {
			foreach (ZombieSpawnPoint spawn in spawnPoints) {
				if (spawn != null && spawn.spawnTransform != null) {
					Gizmos.DrawWireCube (spawn.spawnTransform.position, spawn.spawnSize * 2);
				}
			}
		}

	}


	void MonitorFrameRate () {
		frames++;
		timer += Time.unscaledDeltaTime;
		// check 4 times per second
		if (timer > 0.25f) {
			fps = frames * 4;
			timer = 0;
			frames = 0;
		}
		// maintain 60 as the target
		if (fps < 60) {
			lowFpsTimer += Time.unscaledDeltaTime;
			// check if the low fps timer  > 15 seconds
			if (lowFpsTimer > 15) {
				maxZombies = activeZombieCount;
				// we've hit our limit, stop here.
				hasMaxZombies = true;
			}
		} else {
			lowFpsTimer = 0;
		}

	}

	void Update () {
		// monitor the current framerate, and stop spawning zombies if it gets too low :)
		MonitorFrameRate();
		// check for zombies remaining...
		if (hasZombiesRemaining == true) {
			// now check if we can spawn any more..
			if (activeZombieCount < maxZombies) {
				// check for free spawn points
				for (int i = 0; i < spawnPoints.Length; i++) {
					// check if this spawn point is available
					// TODO check the actual space required
					if (spawnPoints [i].spawnTimer > spawnPoints [i].spawnDelay) {
						// reset the timer
						spawnPoints [i].spawnTimer = 0;
						// create a zombie here.
						SpawnZombie (i);
						// now stop this iteration. only spawn 1 at a time so we don't go over the top
						i = spawnPoints.Length;
					} else {
						// not time yet, so increase the timer
						spawnPoints [i].spawnTimer += Time.deltaTime;
					}
				}
			}
		} else {
			// check if we should spawn the next wave
			CheckForNextWave ();
		}
	}

	void SpawnZombie(int spawnPointIndex){
		// grab the current spawn point
		currentSpawnPoint = spawnPoints [spawnPointIndex];
		// decide which kind of zombie to spawn...
		spawnType = (ZombieType)Random.Range (0, 5);
		// check if we can't spawn this zombie... then find one we can!
		if (CheckSpawnType () == false) {
			//TODO work on a system that runs all the way round the available types...
			// then fall through the other classes, filling them out. if they haven't been spawned yet. This actually creates a spawn "order"
			// default to basic zombies unless all other bases have been covered...

			if (currentWave.fastCount > 0) {
				spawnType = ZombieType.fast;
				currentWave.fastCount -= 1;
			} else if (currentWave.strongCount > 0) {
				spawnType = ZombieType.strong;
				currentWave.strongCount -= 1;
			} else if (currentWave.ghostCount > 0) {
				spawnType = ZombieType.ghost;
			} else if (currentWave.boomCount > 0) {
				spawnType = ZombieType.boom;
			} else if (currentWave.spewCount > 0) {
				spawnType = ZombieType.spew;
			}
			else if(currentWave.splitCount > 0){
				spawnType = ZombieType.split;
				currentWave.splitCount -= 1;
			}else if (currentWave.fatCount > 0) {
				spawnType = ZombieType.fat;
				currentWave.fatCount -= 1;
			}
			else if (currentWave.basicCount > 0) {
				spawnType = ZombieType.basic;
				currentWave.basicCount -= 1;
			} else {
				hasZombiesRemaining = false;
			}
		}

		// now create the zombie
		if (hasZombiesRemaining == true) {
			// increase the active counter by 1
			activeZombieCount++;
			// get the next free zombie in the pool.
			zombiePool [nextFreeZombieIndex].isAlive = true;
			zombiePool [nextFreeZombieIndex].zombieObj.SetActive (true);
			// move the zombie to the desired spawnpoint;
			zombiePool [nextFreeZombieIndex].zombie.MoveToSpawnPoint (currentSpawnPoint.spawnTransform, nextFreeZombieIndex);
			// set the desired zombies type
			zombiePool [nextFreeZombieIndex].zombie.SetZombieType (spawnType);
			// now work out where the next free zombie is...
			while (zombiePool [nextFreeZombieIndex].isAlive == true) {
				// advance the next free zombie
				nextFreeZombieIndex++;
				// loop back over the list
				if (nextFreeZombieIndex >= zombiePool.Count) {
					// back to the start, we've gone over the end
					nextFreeZombieIndex = 0;
				}
			}
		}
		

		// check the pool for overflow.
		if (activeZombieCount > growThreshold) {
			// grow the pool by 10, so as not to go over the limit 
			CreateZombies (10);
		}

	}

	public void SplitZombie(Vector3 _pos, Quaternion _rot, Vector3 _fwd, Node targetNode){
		for (int i = -1; i < 2; i+=2) {
			// increase the active counter by 1
			activeZombieCount++;
			// get the next free zombie in the pool.
			zombiePool [nextFreeZombieIndex].isAlive = true;
			zombiePool [nextFreeZombieIndex].zombieObj.SetActive (true);
			// move the zombie to the desired spawnpoint;
			zombiePool [nextFreeZombieIndex].zombie.MoveToSpawnPoint (_pos+((Vector3.up+_fwd)*0.5f*i), _rot, nextFreeZombieIndex);
			// set the desired zombies type
			zombiePool [nextFreeZombieIndex].zombie.SetZombieType (ZombieType.mini);
			// set the target node
			zombiePool [nextFreeZombieIndex].zombie.SetTargetNode (targetNode);
			// now work out where the next free zombie is...
			while (zombiePool [nextFreeZombieIndex].isAlive == true) {
				// advance the next free zombie
				nextFreeZombieIndex++;
				// loop back over the list
				if (nextFreeZombieIndex >= zombiePool.Count) {
					// back to the start, we've gone over the end
					nextFreeZombieIndex = 0;
				}
			}
		}

		// check the pool for overflow.
		if (activeZombieCount > growThreshold) {
			// grow the pool by 5, so as not to go over the limit 
			CreateZombies (5);
		}
	}

	public void KillZombie (int index) {
		zombiePool [index].isAlive = false;
		activeZombieCount--;
	}

	void CreateZombies (int count){
		for(int i=0;i<count;i++){
			GameObject newObj = (GameObject)Instantiate (zombiePrefab, zombieStore.position, zombieStore.rotation);
			Zombie zombieScript = newObj.GetComponent<Zombie> ();
			PoolZombie newZombie = new PoolZombie(false,zombieScript, newObj);
			// add the zombie to the spawn pool
			zombiePool.Add(newZombie);
			// deactivate the zombie
			newObj.SetActive (false);

		}
		// increase the grow threshold.
		growThreshold += count;
	}

	bool CheckSpawnType (){
		switch (spawnType) {
		// fast zombies
		case ZombieType.fast:
			if (currentWave.fastCount > 0) {
				currentWave.fastCount -= 1;
				return true;
			}
			else{
				return false;
			}
			break;
		case ZombieType.strong:
			if (currentWave.strongCount > 0) {
				currentWave.strongCount -= 1;
				return true;
			}
			else{
				return false;
			}
			break;
		case ZombieType.fat:
			if (currentWave.fatCount > 0) {
				currentWave.fatCount -= 1;
				return true;
			}
			else{
				return false;
			}
			break;
		case ZombieType.split:
			if (currentWave.splitCount > 0) {
				currentWave.splitCount -= 1;
				return true;
			}
			else{
				return false;
			}
			break;
		// basic zombies
		default:
			if (currentWave.basicCount > 0) {
				currentWave.basicCount -= 1;
				return true;
			} else {
				return false;
			}
			break;
		}
	}

	void CheckForNextWave () {

	}

}
