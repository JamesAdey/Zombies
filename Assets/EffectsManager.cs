using UnityEngine;
using System.Collections;

public class EffectsManager : MonoBehaviour {

	public static EffectsManager singleton;

	public ZombieDeathParticle[] particleSystems;

	void Awake () {
		singleton = this;
	}

	public static void SpawnZombieDeathParticles(Vector3 _pos, ZombieType _typ){
		singleton.particleSystems[(int)_typ].SpawnParticles (_pos);
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
