using UnityEngine;
using System.Collections;

public class Zombie : MonoBehaviour
{

	Rigidbody thisRigidbody;
	Transform thisTransform;
	NucleusAnim nucleus;
	public ZombieStats stats;
	public ZombieType zombieType;
	float noTargetTimer = 0;
	float attackTimer = 0;
	static float targetTimeout = 5;
	int zombieIndex;

	ZombieFollowState followState = ZombieFollowState.path;



	// PATHFINDING VARIABLES
	Node targetNode;
	bool canSeePlayer = false;
	public LayerMask zombieSightLayers;
	Vector3 targetPos;
	public Vector3 relativePos;
	public Vector3 angularVel;
	public Vector3 localVel;
	static Vector3 desiredVel;
	static float slowDown = 1;
	static float distToTarget;
	static float maxAcc = 0.25f;
	static float minAcc = -0.25f;

	void Awake ()
	{
		thisRigidbody = this.GetComponent<Rigidbody> ();
		thisTransform = this.transform;
		nucleus = this.GetComponentInChildren<NucleusAnim> ();
		SetZombieType (zombieType);
	}
	// Use this for initialization
	void Start ()
	{
		
	}

	void OnDrawGizmos ()
	{
		if (targetNode != null) {
			Gizmos.color = Color.green;
			Gizmos.DrawLine (thisTransform.position, targetNode.position);
		}
	}

	void Update ()
	{
		UpdateZombie ();
	}

	void UpdateZombie ()
	{
		if (targetNode == null) {
			targetNode = NavGraphController.GetNearestNode (thisTransform.position);
			noTargetTimer = 0;
		}
	}

	public void MoveToSpawnPoint (Transform spawnPoint, int _index)
	{
		thisTransform = this.transform;
		// move us to the spawn point
		thisTransform.position = spawnPoint.position;
		thisTransform.rotation = spawnPoint.rotation;
		// set the zombie index
		zombieIndex = _index;
	}

	public void MoveToSpawnPoint (Vector3 spawnPoint, int _index)
	{
		thisTransform = this.transform;
		// move us to the spawn point
		thisTransform.position = spawnPoint;
		// set the zombie index
		zombieIndex = _index;
	}

	public void MoveToSpawnPoint (Vector3 spawnPoint, Quaternion rot, int _index)
	{
		thisTransform = this.transform;
		// move us to the spawn point
		thisTransform.position = spawnPoint;
		thisTransform.rotation = rot;
		// set the zombie index
		zombieIndex = _index;
	}

	public void SetZombieType (ZombieType type)
	{
		// go query the zombie type manager for our stats
		stats = ZombieManager.GetStatsForType ((int)type);
		zombieType = type;
		// query the zombie manager for the material
		this.GetComponent<MeshRenderer> ().sharedMaterial = ZombieManager.GetMaterialForType ((int)type);
		// get the scale too
		thisTransform.localScale = ZombieManager.GetScaleForType ((int)type);
		// set the nucleus multiplier speed
		nucleus.multiplier = stats.speed;
	}

	// Update is called once per frame
	void FixedUpdate ()
	{
		if (targetNode != null) {
			if (followState == ZombieFollowState.path) {
				// get the relative position of the target
				relativePos = thisTransform.InverseTransformPoint (targetNode.position);
				// calculate the distance to target
				distToTarget = (targetNode.position - thisTransform.position).sqrMagnitude;

				if (relativePos.x > maxAcc) {
					angularVel.y = 2;
				} else if (relativePos.x < minAcc) {
					angularVel.y = -2;
				} else {
					angularVel.y = 0;

				}
				if (relativePos.z > 0) {
					localVel.z = stats.speed;
				} else {
					localVel.z = 0;
				}

				// check if we can see the player
				if (Physics.Linecast (thisTransform.position, PlayerMove.playerTransform.position, zombieSightLayers) == false) {
					followState = ZombieFollowState.player;
					canSeePlayer = true;
				} else {
					// check if we can see the target node
					if (Physics.Linecast (thisTransform.position, targetNode.position, zombieSightLayers) == false) {
						if (targetNode.targetNode != null && Physics.Linecast (thisTransform.position, targetNode.targetNode.position, zombieSightLayers) == false) {
							targetNode = targetNode.targetNode;
						}
						noTargetTimer = 0;
					} else {
						noTargetTimer += Time.deltaTime;
						if (noTargetTimer > targetTimeout) {
							targetNode = null;
						}
					}
				}
			} else {
				// now we're following the player >:)
				// work out the motion
				if (canSeePlayer == true) {
					// set the target position
					targetPos = PlayerMove.playerTransform.position;
				}
				// work out the 
				relativePos = thisTransform.InverseTransformPoint (targetPos);
				distToTarget = (targetPos - thisTransform.position).sqrMagnitude;

				if (relativePos.x > maxAcc) {
					angularVel.y = 2;
				} else if (relativePos.x < minAcc) {
					angularVel.y = -2;
				} else {
					angularVel.y = 0;

				}
				// only go forward if we are behind our target
				if (relativePos.z > 0) {
					localVel.z = stats.speed;
				} else {
					localVel.z = 0;
				}
				// check LOS to player
				if (Physics.Linecast (thisTransform.position, PlayerMove.playerTransform.position) == false) {
					// we have LOS to player
					canSeePlayer = true;
				} else {
					// can't see the player
					canSeePlayer = false;
					// dist to target in this case is the distance to the player.
					if (distToTarget < 1) {
						// stop and get back on track
						followState = ZombieFollowState.path;
						targetNode = null;
					}
				}


			}

			// check for attacks
			if (canSeePlayer == true && distToTarget < stats.attackDist) {
				nucleus.extended = true;
				// run attack timer
				attackTimer += Time.deltaTime;
				if (attackTimer > stats.attackTime) {
					// TODO deal damage...
					PlayerMove.TakeDamage (stats.damage);
					attackTimer = 0;
				}
			} else {
				// we can't see the player, or we're out of attack range
				// so reset our attack status
				attackTimer = 0;
				nucleus.extended = false;
			}
			// set the angular velocity so we can turn
			thisRigidbody.angularVelocity = angularVel;
			// transform the local velocity into a world one that can be applied to the rigidbody
			desiredVel = thisTransform.TransformVector (localVel);

			// ensure that we don't cause ourselves to go over our max speed.
			if (desiredVel.x > 0 && thisRigidbody.velocity.x > desiredVel.x) {
				desiredVel.x = Mathf.Lerp (thisRigidbody.velocity.x, desiredVel.x, slowDown * Time.deltaTime);
			} else if (desiredVel.x < 0 && thisRigidbody.velocity.x < desiredVel.x) {
				desiredVel.x = Mathf.Lerp (thisRigidbody.velocity.x, desiredVel.x, slowDown * Time.deltaTime);
			}

			if (desiredVel.z > 0 && thisRigidbody.velocity.z > desiredVel.z) {
				desiredVel.z = Mathf.Lerp (thisRigidbody.velocity.z, desiredVel.z, slowDown * Time.deltaTime);
			} else if (desiredVel.z < 0 && thisRigidbody.velocity.z < desiredVel.z) {
				desiredVel.z = Mathf.Lerp (thisRigidbody.velocity.z, desiredVel.z, slowDown * Time.deltaTime);
			}
			// don't change anything of the y-velocity, since we want to fall as normal
			desiredVel.y = thisRigidbody.velocity.y;
			// set the velocity back to the rigidbody
			thisRigidbody.velocity = desiredVel;
		}
	}

	void ZombieDeath ()
	{
		// determine if we need to drop an item

		// check the zombie type
		switch (zombieType) {
		case ZombieType.fat:
			// always drop 1... but sometimes drop 2 or 3!?!

			break;
		case ZombieType.boom:
			// explode!
			break;
		case ZombieType.split:
			// don't drop an item, instead spawn 2 more zombies!?!
			ZombieSpawner.singleton.SplitZombie (thisTransform.position, thisTransform.rotation, thisTransform.forward, targetNode);
			break;

		default:
			// generate 0 or 1 items...
			break;
		}
		// spawn a nice particle effect...
		EffectsManager.SpawnZombieDeathParticles (thisTransform.position, zombieType);
		// kill the zombie
		ZombieSpawner.singleton.KillZombie (zombieIndex);
		thisTransform.position = ZombieSpawner.singleton.zombieStore.position;
		thisRigidbody.velocity = Vector3.zero;
		thisRigidbody.angularVelocity = Vector3.zero;
		targetNode = null;
		gameObject.SetActive (false);
	}

	public void SetTargetNode (Node _t)
	{
		targetNode = _t;
	}

	public void TakeDamage (int dmg)
	{
		stats.health -= dmg;
		if (stats.health <= 0) {
			ZombieDeath ();
		}
	}
}
