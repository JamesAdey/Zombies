using UnityEngine;
using System.Collections;

public class NucleusAnim: MonoBehaviour {

	public static float nucleusSpeed = 2.5f;
	public float multiplier = 1;
	Transform thisTransform;
	//object to hold the tentacles
	public Transform tentacleTransform;
	public LineRenderer[] tentacleRenderers;
	public bool extended = false;
	float extendTimer;
	float timerPercent;
	static float extendDuration = 0.66f;
	static Vector3 centerPos = new Vector3 (0, 0.45f, 0.0625f);
	static Vector3[] mainExtendPos = {new Vector3(0,-0.05f,0.33f),new Vector3(0,0.05f,0.66f),new Vector3(0,0f,1)};
	Vector3[] mainRelaxPos = new Vector3[3];
	static float wobble = 0.125f;
	static float forwardWobble = -0.05f;

	float offset;
	float moveOffset = -0.03f;
	static Vector3 smoothPos;
	Vector3 rotAngle;
	static Vector3 tentaclePos;
	static float tentacleWobble = 0.125f;
	static float attackWobble = 0.1f;
	static float modTime;
	static float[] tentacleYPos = {0,-0.33f,-0.66f,-1f};
	// Use this for initialization
	// TODO possibly sync the animations to boost performance.
	void Start () {
		smoothPos = centerPos;
		thisTransform = this.transform;
		offset = Time.deltaTime * 1000;
	}
	
	// Update is called once per frame
	void Update () {
		modTime = Time.time * nucleusSpeed * multiplier;
		smoothPos.y = wobble * Mathf.Sin (modTime + offset);
		smoothPos.z = forwardWobble * Mathf.Cos (modTime + offset);
		rotAngle.x = smoothPos.y*7;
		smoothPos += centerPos;

		thisTransform.Rotate (rotAngle);
		thisTransform.localPosition = smoothPos;
		tentacleTransform.localPosition = smoothPos;

		for (int i = 0; i < tentacleRenderers.Length; i++) {
			if (extended == true && i == 0) {
				if (extendTimer < extendDuration) {
					extendTimer += Time.deltaTime*nucleusSpeed;
					timerPercent = extendTimer / extendDuration;
				}
				for (int n = 0; n < 3; n++) {
					tentaclePos = Vector3.Lerp (mainRelaxPos [n], mainExtendPos [n], timerPercent);
					tentaclePos.x = attackWobble * Mathf.Cos (modTime + offset + n + i)*1.5f;
					tentaclePos.y += attackWobble * Mathf.Sin ((modTime + offset + n)*2);
					tentacleRenderers [i].SetPosition (n + 1, tentaclePos);
				}
			} else {
				
				// move the tentacles normally
				for (int n = 1; n < 4; n++) {
					tentaclePos.z = (moveOffset * n) + (tentacleWobble * Mathf.Sin(modTime + offset + n + i));
					tentaclePos.x = tentacleWobble * Mathf.Cos (modTime + offset + n + i);
					tentaclePos.y = tentacleYPos [n];
					if (i == 0) {
						mainRelaxPos [n - 1] = tentaclePos;
						if (extendTimer > 0) {
							tentaclePos = Vector3.Lerp (mainRelaxPos [n - 1], mainExtendPos [n - 1], timerPercent);
						}
					}
					tentacleRenderers [i].SetPosition (n, tentaclePos);

				}
			}
		}
		if (extended == false && extendTimer > 0) {
			extendTimer -= Time.deltaTime*nucleusSpeed;
			timerPercent = extendTimer / extendDuration;
		}
	}
}
