using UnityEngine;
using System.Collections;

public class LaserProjectile : MonoBehaviour {
	LineRenderer line;
	public float fadeTime = 1;
	float timer;
	public GameObject thisGameObject;

	static Color colour = Color.white;
	void Awake() {
		line = this.GetComponent<LineRenderer> ();
		thisGameObject = gameObject;
	}

	public void FireProjectile(Vector3 startPos, Vector3 endPos){
		thisGameObject.SetActive (true);
		line.SetPosition (0, startPos);
		line.SetPosition (1, endPos);
	}

	public void DestroyProjectile(){
		ProjectilePool.singleton.DestroyLaserProjectile ();
		thisGameObject.SetActive (false);

	}

	// Update is called once per frame
	void Update () {
		timer += Time.deltaTime;
		if (timer > fadeTime) {
			timer = 0;
			// hide the line renderer
			DestroyProjectile();
		}
		// calculate the desired alpha value
		colour.a = 1- (timer/fadeTime);
		// set the line renderers alpha
		line.SetColors(colour, colour);
	}
}
