using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour {


	[System.Serializable]
	public class cGameType
	{
		public eGameType vGameType;					//check out which game type we handle
		public List<GameObject> vEnemyList;			//keep a list of enemy which will be used for the mod selected
		public float vSpeed = 0.1f;					//spawn factor which will be increased between each enemy killed. so the game become faster after each kill
		public float vSpawnSpeed = 1f;				//spawn speed for the enemy to be created
		public float vEnemySpeed = 1f; 				//enemy speed moving toward player
		public bool UseRandomBallColor = false;		//each time we launch a balls, the next one has a random color
		public bool UseColorsFall = false;			//activate the colors fall
	}

	[System.Serializable]
	public class cSpawnList
	{
		//1st spawn 
		public GameObject vSpawnObject;
		public List<GameObject> vSpawnList;
	}

	public enum eGameType {Simple, RandomColor, ColorsFall};

	public List<cGameType> vGameConfig; 			//has all the game type differents settings
	public eGameType vGameType = eGameType.Simple;  //when starting game, we check which kind of game we want to play!
	public List<cSpawnList> vSpawnList; 			//handle different spawn position + get the number of enemy per line so we try to send new enemy on the line with the lowest count
	public SpriteRenderer vBackGround; 				//background objects which change color
	public int vLevel = 1; 							//keep in mind the level for the player. Higher level has deadly enemies
	public List<GameObject> vCurrentBalls;			//balls we are using. Could use more than 1 kind of balls (random)
	public List<GameObject> vEnemyList; 			
	public int vBallSpeed = 10; 
	public GameObject vPlayer;	
	public GameObject vMenuObject; 
	public GameObject vPauseScreen;
	public GameObject vPauseButton;
	public GameObject vBallsList;					//when we need to choose a balls color
	public float vTimeNeededNext = 1f; 				//how much time for the next enemy?
	public Text vScore;
	public int vScoreValue = 0;
	public GameObject vCurBall; 					//keep in mind the ball we will launch!
	public List<Color> vGameColors;					//configure here all the colors used for Enemies or Balls
	public GameObject vColorFall;					//if we use the colorfall, we instantiate color drop
	public GameObject vColorDrop;					
	public float vDropSpawnSpeed = 1f;				//instantiate drop at this speed!
	public float vDropSpeed = 1f;					//drop as fast as this speed
	public AudioSource vAudioSource; 				//manage all the sound 
	public AudioClip vSndStartGame;
	public AudioClip vSndGameOver;
	public AudioClip vEnemyDie;

	private bool GameStarted = false;
	private bool ClickOnce = false;
	private float SpawnTimeNext = 0f;
	private float vRemainingFall = 0;
	private int vCurColor = 0;						//check wich color we show on the color fall
	public cGameType vCurGameType;
	public GameObject vColorsChoicePanel;			//has all the color we can choose in the game
	public List<Image> vColorsChoice; 	
	private EventSystem eventsystem;

	void Awake()
	{
		//increase framerate so the movement is more accurate
		Application.targetFrameRate = 60;
	}

	// Use this for initialization
	void Start () {
		//make sure when we start the game, everything is correct
		RefreshVariables ();
	}

	void RefreshVariables()
	{
		//hide pause button
		vPauseButton.SetActive(false);

		//hide pause screen
		vPauseScreen.SetActive(false);

		//refresh score
		vScoreValue = 0;
		vScore.text = "0";

		//make sure to remove the old ball
		if (vCurBall != null) {
			GameObject.Destroy (vCurBall);
			vCurBall = null;
		}

		//initialize variables
		vLevel = 1;
		ClickOnce = false;
		GameStarted = false;
		SpawnTimeNext = 0f;

		//disable the score text
		vScore.enabled = false;

		//enable the player to shoot
		vPlayer.SetActive(false);

		//hide menu
		vMenuObject.SetActive(true);

		//show the panel
		vColorsChoicePanel.SetActive(false);
	}

	void CleanEnemy()
	{
		//destroy all enemy
		foreach (cSpawnList vSpawn in vSpawnList)
			foreach (Transform child in vSpawn.vSpawnObject.transform)
				GameObject.Destroy (child.gameObject);
	}

	public void GameOver ()
	{
		//make sure the time start again
		Time.timeScale = 1f;

		//stop the game
		GameStarted = false;

		//clean all the enemy in this game
		CleanEnemy();

		//clean all the variable
		RefreshVariables ();
	}

	public void ResumeGame()
	{
		//stop the time
		Time.timeScale = 1f;

		//hide pause screen
		vPauseScreen.SetActive (false);

		//show pause button
		vPauseButton.SetActive(true);

		//resume the game
		GameStarted = true;
	}

	public void MenuScreen()
	{
		//finish game already
		GameOver ();
	}

	public void PauseGame()
	{
		//stop the time
		Time.timeScale = 0f;

		//show pause screen
		vPauseScreen.SetActive (true);

		//hide pause button
		vPauseButton.SetActive(false);

		//hold the player so he cannot create balls
		GameStarted = false;
	}
	
	// Update is called once per frame
	void Update () {
		//check if game started!
		if (GameStarted) {

			//check if we need to create a new ball
			if (vCurBall == null) {
				CreateBall ();
			}
				
			//check if we instantiate a drop in the color fall
			if (vCurGameType.UseColorsFall)
			{
				//reduce it's time
				vRemainingFall -= Time.deltaTime;

				if (vRemainingFall <= 0f) {
					//get back original time for the next one
					vRemainingFall = vDropSpawnSpeed;

					//increase color counter
					vCurColor++;
					if (vCurColor >= vGameColors.Count)
						vCurColor = 0; //go back to 0

					//create this drop to fall 
					GameObject vNewDrop = Instantiate (vColorDrop);
					vNewDrop.transform.position = vColorFall.transform.position;					//teleport the drop at the gameobject
					vNewDrop.GetComponent<ColorFall> ().vInside.color = GetRandomColor (vCurColor); //get a random color
					vNewDrop.GetComponent<ColorFall> ().vSpeed = vDropSpeed;						//change the drop speed
				}
			}

			//check if we are above a GUI panel so we can't shoot and lose Ammo when switching
			eventsystem = EventSystem.current;

			//Shoot balls only when we click!
			if (Input.GetMouseButton (0) && !ClickOnce && vCurBall != null && CanShoot()) {
				
				//make sure we cannot send multiple ball at once
				ClickOnce = true;

				//ball now can be destroyed by time
				vCurBall.GetComponent<Ball> ().vCanBeDestroyed = true;

				//get angle from the mouse click and the player
				Vector3 dir = Camera.main.ScreenToWorldPoint (Input.mousePosition) - vPlayer.transform.position;

				//calcualte it's speed
				Rigidbody2D ballInstance = vCurBall.GetComponent<Rigidbody2D> ();
				ballInstance.velocity = new Vector2 (1, (1 / dir.x) * dir.y) * vBallSpeed;

				//unlink this ball so we get a new one
				vCurBall = null;
			}

			//check if we are not clicking, to be able to click again
			if (!Input.GetMouseButton (0) && ClickOnce)
				ClickOnce = false;

			//reduce time to check if we spawn a new enemy
			SpawnTimeNext -= Time.deltaTime;
			if (SpawnTimeNext <= 0f) {
				SpawnTimeNext = vCurGameType.vSpawnSpeed;
				SpawnNewEnemy ();
			}
		}
	}

	void CreateBall()
	{
		vCurBall = Instantiate (vCurrentBalls [0]); //create the new ball

		//cannot be destroyed until launch
		vCurBall.GetComponent<Ball> ().vCanBeDestroyed = false;

		//send this component to the ball itself to be sure it can return a score when it kill
		vCurBall.GetComponent<Ball> ().vGameManager = this;

		//change the position for the ball to be on the player
		vCurBall.transform.position = vPlayer.transform.position;
	}

	//get a random color from the color list
	public Color GetRandomColor(int vColorIndex = -1)
	{
		//init variable
		Color vColor; 

		//check here if we got a random color or a precise one
		if (vColorIndex >= 0)
			vColor = vGameColors [vColorIndex]; 					//get this color (not random)
		else
			vColor = vGameColors[Random.Range(0, vGameColors.Count)];//get a random color from list

		//return the right color
		return vColor;
	}

	bool CanShoot()
	{
		//init.
		bool result = false;

		if (eventsystem.currentSelectedGameObject == null)
			result = true;
		else if (eventsystem.currentSelectedGameObject.tag != "GUI")
			result = false;

		return result; 
	}

	//show current game colors
	public void ChangeChoiceColorPanel()
	{
		//show the panel
		vColorsChoicePanel.SetActive(true);

		//initialise variable
		int cpt = 0;

		//get the appropriate color
		foreach (Image vImage in vColorsChoice) {
			vImage.color = vGameColors [cpt];
			cpt++;
		}
	}

	public void ChangeBallColor(int vIndex)
	{
		//change the ball color for this one
		if (vCurBall != null)
			vCurBall.GetComponent<Ball> ().vInside.color = GetRandomColor (vIndex);
	}

	//check which mod we are and spawn a random enemy in the list on a random line
	void SpawnNewEnemy()
	{
		//get a random enemy in the current game mod
		GameObject vNewEnemy = Instantiate (vCurGameType.vEnemyList[Random.Range(0, vCurGameType.vEnemyList.Count)]);

		//we randomize all the enemy for all mobs but SIMPLE
		vNewEnemy.GetComponent<cEnemy> ().Initialise (this);

		cSpawnList vRandomSpawn = vSpawnList [Random.Range (0, vSpawnList.Count)];

		//get a random spawn position now
		vNewEnemy.transform.position = vRandomSpawn.vSpawnObject.transform.position;

		//make this enemy a child of the current line to be able to delete it when game is finish
		vNewEnemy.transform.parent = vRandomSpawn.vSpawnObject.transform;
	}

	public void EnemyDied()
	{
		//if the score is disabled, we just enable it and refresh the score to 0
		if (!vScore.enabled) {
			vScore.enabled = true;
			vScore.text = "0";
		}

		//increase the score by 1
		vScoreValue++;

		//change the score
		vScore.text = vScoreValue.ToString();
	}

	//function which start the game
	public void StartGame(string vSelectedGameType)
	{
		//show pause button
		vPauseButton.SetActive(true);

		//change the game type before we start
		vGameType = (eGameType)System.Enum.Parse (typeof(eGameType), vSelectedGameType);

		//get the game type we are on for further usage
		foreach (cGameType gametype in vGameConfig)
			if (vGameType == gametype.vGameType)
				vCurGameType = gametype;

		//enable the player to shoot
		vPlayer.SetActive(true);

		//hide menu
		vMenuObject.SetActive(false);

		if (vCurGameType.UseRandomBallColor)
			ChangeChoiceColorPanel ();

		//let's play!
		GameStarted = true;
	}
}
