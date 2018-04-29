using UnityEngine;
using System.Collections;

public class JustRotate : MonoBehaviour {
	
	private float RotateByAngle = 0f;
	public float vRotateSpeed = 10f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		//rotate the enemy with time
		RotateByAngle = Time.deltaTime * (-vRotateSpeed * 10);

		Vector3 temp = transform.rotation.eulerAngles;
		temp.x = 0f;
		temp.y = 0f;
		temp.z += RotateByAngle;
		transform.rotation = Quaternion.Euler (temp);
	}
}
