using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour {

	private float RotateByAngle = 0f;
	public float vSpeed = 1f;
	public float vTimeRemaining = 3f;				//how much second it will last until destroy. 
	public bool vCanBeDestroyed = true;				//make sure the ball on the player cannot be destroyed, so we don't check it's TimeRemaining
	public GameManager vGameManager;				//to be able to return a count of the enemy died on the impact
	public SpriteRenderer vInside; 					//can change the color inside the ball
	public SpriteRenderer vBorder;					//can change the color oustide the ball

	private bool InitialeCheck = false;

	// Use this for initialization
	void Start () {
		//initialise variable
		InitialeCheck = false;
	}

	// Update is called once per frame
	void Update () 
	{
		//rotate the ball with time
		RotateByAngle = Time.deltaTime*(-vSpeed*10);

		Vector3 temp = transform.rotation.eulerAngles;
		temp.x = 0f;
		temp.y = 0f;
		temp.z += RotateByAngle;
		transform.rotation = Quaternion.Euler(temp);

		//check if the balls need to be destroyed
		if (vCanBeDestroyed) {
			vTimeRemaining -= Time.deltaTime;
			if (vTimeRemaining <= 0f)
				GameObject.Destroy (this.gameObject);
		}

		//check out the mode to see what we do with the ball
		if (vGameManager != null && !InitialeCheck) {
			InitialeCheck = true;

			//get a random color
			if (vGameManager.vCurGameType.vGameType == GameManager.eGameType.RandomColor)
				vInside.color = vGameManager.GetRandomColor ();
			else
				vInside.color = Color.white;	//color white
		}
	}

	void OnTriggerEnter2D (Collider2D col)
	{
		//ball can only damage Enemy when it's launch
		if (col.tag == "Enemy" && vCanBeDestroyed) 
		{
			//check if we have to check if it's the same color
			if ((vGameManager.vCurGameType.vGameType != GameManager.eGameType.Simple && vInside.color == col.GetComponent<cEnemy> ().vInside.color)
			    || vGameManager.vCurGameType.vGameType == GameManager.eGameType.Simple) {
				//show dying enemy animation
				col.GetComponent<Animator> ().enabled = true;

				//tell the gamemanager, we killed a enemy!
				vGameManager.EnemyDied ();
			} else {
				col.GetComponent<cEnemy> ().WrongBall ();
			}
				
			//destroy ball
			GameObject.Destroy (this.gameObject);
		}
	}
}
