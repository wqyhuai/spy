using UnityEngine;
using System.Collections;

public class ColorFall : MonoBehaviour {

	public SpriteRenderer vInside;
	public SpriteRenderer vBorder;
	public float vSpeed = 1f;

	private Rigidbody2D vRigidBody;

	// Use this for initialization
	void Start () {
		vRigidBody = GetComponent<Rigidbody2D> ();
	}
	
	// Update is called once per frame
	void Update () {
		//make sure we got a rigidbody attached for moving it
		if (vRigidBody != null)
			vRigidBody.velocity = new Vector2 (0f, -vSpeed);
	}

	void OnTriggerEnter2D (Collider2D col)
	{
		// If it hits an destructible...
		if (col.tag == "Ball") 
		{
			//change it's color for this drop color
			col.GetComponent<Ball> ().vInside.color = vInside.color;
		}
		else if (col.tag == "Border") 
			//destroy itself
			GameObject.Destroy (this.gameObject);
	}
}
