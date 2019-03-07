using UnityEngine;
using System.Collections;

public class ZombieDeathParticle : MonoBehaviour {
	public int count = 30;
	public Vector3 offset = new Vector3 (0, 0.9f, 0);
	public ParticleSystem particles;
	//public ParticleSystemRenderer rend;
	//public ParticleSystem.ShapeModule shapes;
	//float timer;

	Transform thisTransform;
	//Transform particleTransform;
	//GameObject thisGameObject;

	void Awake () {
		//rend = particles.GetComponent<ParticleSystemRenderer> ();
		//shapes = particles.shape;
		//particleTransform = particles.transform;
		thisTransform = this.transform;
		//thisGameObject = this.gameObject;
	}

	// Use this for initialization
	void Start () {
	
	}

	public void SpawnParticles(Vector3 pos) {
		thisTransform.position = pos+offset;
		particles.Emit (count);
	}

	// Update is called once per frame
/*	void Update () {
		timer += Time.deltaTime;
		if (timer > particles.startLifetime) {
			// remove the particle system
		}
	}*/
}
