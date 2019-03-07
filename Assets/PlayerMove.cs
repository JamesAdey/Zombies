using UnityEngine;
using System.Collections;

public class PlayerMove : MonoBehaviour {

	public Transform firePoint;
	public Transform shootPoint;
	RaycastHit hit = new RaycastHit();
	Zombie hitZombie;
	public int damage = 5;
	public static Transform playerTransform;
	Rigidbody thisRigidbody;
	public Transform cam;

	public static float health;

	public float speed = 2.5f;
	public float slowDown = 1f;
	Vector3 angularVel;
	Vector3 localVel;
	Vector3 desiredVel;
	Vector3 cameraAngles;
	Quaternion extraRot;

	public float maxLookAngle = 90;
	public float minLookAngle = -90;

	// Use this for initialization
	void Awake () {
		playerTransform = this.transform;
		thisRigidbody = this.GetComponent<Rigidbody> ();
	}
	
	// Update is called once per frame
	void Update () {
		localVel.x = Input.GetAxis ("Horizontal");
		localVel.z = Input.GetAxis ("Vertical");
		angularVel.y = Input.GetAxis ("Mouse X");
		cameraAngles.x -= Input.GetAxis ("Mouse Y");
		if (Input.GetMouseButtonUp (0)) {
			// simple shooting
			Fire();
		}
	}

	void Fire () {
		if (Physics.Raycast (firePoint.position, firePoint.forward, out hit, 100)) {
			ProjectilePool.singleton.SpawnLaserProjectile (shootPoint.position, hit.point);
			if (hit.transform.tag.Equals ("zombie")) {
				hitZombie = hit.transform.GetComponent<Zombie> ();
				hitZombie.TakeDamage (damage);
			}
		}
	}

	void FixedUpdate () {
		
		thisRigidbody.angularVelocity = angularVel;
		desiredVel = playerTransform.TransformDirection (localVel).normalized * speed;

		if (desiredVel.x > 0 && thisRigidbody.velocity.x > desiredVel.x) {
			desiredVel.x = Mathf.Lerp (thisRigidbody.velocity.x, desiredVel.x, slowDown * Time.deltaTime);
		}
		else if(desiredVel.x < 0 && thisRigidbody.velocity.x < desiredVel.x){
			desiredVel.x = Mathf.Lerp (thisRigidbody.velocity.x, desiredVel.x, slowDown * Time.deltaTime);
		}

		if (desiredVel.z > 0 && thisRigidbody.velocity.z > desiredVel.z) {
			desiredVel.z = Mathf.Lerp (thisRigidbody.velocity.z, desiredVel.z, slowDown * Time.deltaTime);
		}
		else if(desiredVel.z < 0 && thisRigidbody.velocity.z < desiredVel.z){
			desiredVel.z = Mathf.Lerp (thisRigidbody.velocity.z, desiredVel.z, slowDown * Time.deltaTime);
		}

		desiredVel.y = thisRigidbody.velocity.y;
		thisRigidbody.velocity = desiredVel;
		extraRot = Quaternion.Euler (angularVel);
		thisRigidbody.MoveRotation (thisRigidbody.rotation * extraRot);
		// clamp the look angles to within the allowed amount
		if (cameraAngles.x > maxLookAngle) {
			cameraAngles.x = maxLookAngle;
		} else if(cameraAngles.x < minLookAngle) {
			cameraAngles.x = minLookAngle;
		}
		cam.localEulerAngles = cameraAngles;
	}

	void OnGUI () {
		GUI.Box (new Rect (5, 5, 100, 30), health.ToString ());
	}

	public static void TakeDamage (float _dmg) {
		health += _dmg;
	}

}
