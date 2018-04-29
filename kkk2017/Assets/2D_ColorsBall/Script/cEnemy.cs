using UnityEngine;
using System.Collections;

public class cEnemy : MonoBehaviour {

	public float vSpeed = 1f;
	public float vRotateSpeed = 8f; 
	private float RotateByAngle = 0f;
	public SpriteRenderer vBorder; 
	public SpriteRenderer vInside;
	public Color vColor;
	public bool vIsMainEnemy = false;
	public bool vIsRotating = false;

	private Rigidbody2D vRigidBody;
	private CircleCollider2D vCircleCollider;
	private GameManager vGameManager;

	// Use this for initialization
	void Start () {
		//show dying enemy animation
		GetComponent<Animator>().enabled = false;

		vRigidBody = GetComponent<Rigidbody2D>();
		vCircleCollider = GetComponent<CircleCollider2D>();
	}
	
	// Update is called once per frame
	void Update () {

		//make sure we got a rigidbody attached for moving it.
		//ONLY the main enemy can move forward. all other pieces doesn't move and make a armor around the enemy
		if (vRigidBody != null && vIsMainEnemy)
			vRigidBody.velocity = new Vector2 (-vSpeed, 0f);

		//check if it's rotating
		if (vIsRotating) {
			//rotate the enemy with time
			RotateByAngle = Time.deltaTime * (-vRotateSpeed * 10);

			Vector3 temp = transform.rotation.eulerAngles;
			temp.x = 0f;
			temp.y = 0f;
			temp.z += RotateByAngle;
			transform.rotation = Quaternion.Euler (temp);
		}
	}

	//kill this enemy
	public void Die()
	{
		GameObject.Destroy (this.gameObject);
	}

	//initialise this enemy correctly 
	public void Initialise(GameManager vvGameManager)
	{
		//get the gamemanager
		vGameManager = vvGameManager;

		//configure the enemy move speed
		vSpeed = vGameManager.vCurGameType.vEnemySpeed + (vGameManager.vCurGameType.vSpeed*vGameManager.vScoreValue);

		//get all the cenemy in this gameobject
		cEnemy[] venemy = GetComponentsInChildren<cEnemy> ();

		//get a random color for each part
		foreach (cEnemy child in venemy)
		{
			//if the mod isn't simple, we get a random color for the enemy
			if (vGameManager.vCurGameType.vGameType != GameManager.eGameType.Simple)
				child.vInside.color = vGameManager.GetRandomColor ();
			else
				child.vInside.color = vGameManager.vGameColors[0]; //get the first one
		}
	}

	public void WrongBall()
	{
		//increase enemy movement speed
		vSpeed += 1;
	}

	//remove all component to prevent a ball hitting the same enemey 2x time before it die
	public void BeforeDie()
	{
		//if we got it, just delete it
		if (vCircleCollider != null) 
			Destroy (vCircleCollider);
		
		//if we got it, just delete it
		if (vRigidBody != null)
			Destroy (vRigidBody);
	}

	void OnTriggerEnter2D (Collider2D col)
	{
		// If a enemy touch the border, it's game over
		if (col.tag == "Border") 
		{
			//game over
			vGameManager.GameOver ();
		}
	}
}
