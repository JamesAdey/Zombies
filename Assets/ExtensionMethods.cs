using UnityEngine;
using System.Collections;

public static class ExtensionMethods {

	public static int RoundToNearestInt (float value, int roundTo){
		return (Mathf.RoundToInt(value/roundTo) * roundTo);
	}
	
	public static Vector3 RoundVector3NearestInt(Vector3 vect, int RoundTo){
		vect.x = RoundToNearestInt(vect.x,RoundTo);
		vect.y = RoundToNearestInt(vect.y,RoundTo);
		vect.z = RoundToNearestInt(vect.z,RoundTo);
		return vect;
	}
	
	public static Vector3 RoundVector3NonZeroInt(Vector3 vect, int num){
		
		vect.x = RoundToNearestNonZeroInt(vect.x,num);
		vect.y = RoundToNearestNonZeroInt(vect.y,num);
		vect.z = RoundToNearestNonZeroInt(vect.z,num);
		return vect;
		
	}
	
	public static int RoundToNearestNonZeroInt (float value, int roundTo){
	//Debug.Log ("Rounding value "+value+" to "+ roundTo);
	// floating points can be massive decimals like 1e-88
	// so if it is basically zero... make it zero 
	// accurate to 5 s.f.
	if(Mathf.Abs(value) < 0.00001f){
	value = 0;
	}
		if(value > 0){
			return (Mathf.CeilToInt(value/roundTo) * roundTo);
		}
		else if(value < 0){
			return (Mathf.FloorToInt(value/roundTo) * roundTo);
		}
		else{
		value = 0.1f;
			return (Mathf.CeilToInt(value/roundTo) * roundTo);
		}
	}
	
	// variables
	static float dst;
	public static bool IsWithinRange(float startValue, float endValue, float nearDistance){
		dst = Mathf.Abs (startValue-endValue);
		if(dst < nearDistance){
			return true;
		}
		else{
			return false;
		}
	}
	
}
