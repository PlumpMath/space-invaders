﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameController : MonoBehaviour
{
	#region cursor stuff
	public Texture2D cursorTexture;
	public CursorMode cursorMode = CursorMode.Auto;
	#endregion

	public GameObject[] invadersPrefabList;
	public float padding;
	public Vector2 defaultInvaderSpeed;
	private Vector2 invaderSpeed;
	public int score = 0;
	public GameObject missilePrefab;
	public AudioClip shootAudio;
	public bool playerWin = false;
	public int gameLevel = 1;
	// Sorry for one more flag...
	public bool gameStarted = false;
	public int gameLevelMax = 4;

	private bool movingRight = false;
	private int missileMax = 4;
	private float invadersResponseTimer = 0;
	private float invadersStartFiringSecondCount = 2f;
	private int INVADERS_ROW_COUNT = 5;
	private int INVADERS_COL_COUNT = 11;

	void Start () {
		Cursor.SetCursor(cursorTexture, Vector2.zero, cursorMode);
	}

	public void invadersStopFireAWhile() {
		invadersResponseTimer = 0;
	}

	public void cleanAndRestart() {
		gameStarted = true;
		score = 0;
		var player = FindObjectOfType<Player> ();
		player.reborn ();
		player.died = false;
		startNewLevel ();
	}

	public void startNewLevel(int level = 1) {
		Cursor.visible = false;
		invadersStopFireAWhile ();

		#region game level configuration
		gameLevel = level;
		missileMax = 3 + gameLevel;
		invaderSpeed = this.defaultInvaderSpeed * ((9 + gameLevel * 2) / 10.0f);
		#endregion
		cleanupExistingInvaders ();
		for (int i = 0; i < INVADERS_COL_COUNT; i++) {
			for (int j = 0; j < INVADERS_ROW_COUNT; j++) {
				// As a start, assume there are just three types of invaders
				int prefabIndex = (int)Math.Floor ((float)((j + 1) / 2));
				Instantiate<GameObject> (
					invadersPrefabList [prefabIndex],
					new Vector3 (transform.position.x + (float)(1.5 * i), transform.position.y - (float)(j * 1.5), 0),
					Quaternion.identity,
					transform.parent
				);
			}
		}
	}

	private void cleanupExistingInvaders() {
		var invaders = GameObject.FindGameObjectsWithTag ("Invader");
		foreach (var item in invaders) {
			Destroy (item);
		}
	}

	void cleanUp(GameObject[] invaders) {
		foreach (var item in invaders) {
			var cameraOrthographicSize = Camera.main.orthographicSize;
			if (Mathf.Abs(item.transform.position.y) > (cameraOrthographicSize + 2)) {
				Destroy (item);
			}
		}
	}

	void updateSpeedAndPosition(GameObject[] invaders) {
		float yInc = 0f;
		if (invaders.Length > 0) {
			var leftTopPoint = invaders [0].transform.position;
			var rightBottomPoint = invaders [0].transform.position;
			foreach (var item in invaders) {
				if (item.transform.position.x < leftTopPoint.x) {
					leftTopPoint.x = item.transform.position.x;
				}
				if (item.transform.position.y > leftTopPoint.y) {
					leftTopPoint.y = item.transform.position.y;
				}

				if (item.transform.position.x > rightBottomPoint.x) {
					rightBottomPoint.x = item.transform.position.x;
				}
				if (item.transform.position.y < rightBottomPoint.y) {
					rightBottomPoint.y = item.transform.position.y;
				}
			}
			var cameraOrthographicSize = Camera.main.orthographicSize;
			if (leftTopPoint.x < (-cameraOrthographicSize + padding) && !movingRight) {
				movingRight = !movingRight;
				yInc = invaderSpeed.y;
			}
			if (rightBottomPoint.x > (cameraOrthographicSize - padding) && movingRight) {
				movingRight = !movingRight;
				yInc = invaderSpeed.y;
			}

			// Make invaders move faster when their number decreases. If there are only one left, the speed is 1.5X
			var realSpeedX = invaderSpeed.x * (1.0f + 0.5f / invaders.Length);
			foreach (var item in invaders) {
				item.transform.position = new Vector3 (
					item.transform.position.x + Time.deltaTime * (movingRight ? realSpeedX : -realSpeedX),
					item.transform.position.y + yInc,
					0f
				);
				if (Math.Abs (item.transform.position.y) > (cameraOrthographicSize + 0.4)) {
					FindObjectOfType<Player> ().died = true;
				}
			}
		}
	}

	void attack(GameObject[] invaders) {
		List<GameObject> lastRowinvaders = new List<GameObject> ();
		foreach (var item in invaders) {
			if (item.tag != "Died") {
				var sameXItem = lastRowinvaders.Find (obj => obj.transform.position.x == item.transform.position.x);
				if (sameXItem == null) {
					lastRowinvaders.Add (item);
				};
				if (sameXItem != null && sameXItem.transform.position.y > item.transform.position.y) {
					lastRowinvaders.Remove (sameXItem);
					lastRowinvaders.Add (item);
				}
			}
		}
		if (lastRowinvaders.Count > 0) {
			var missileCount = Mathf.Min (missileMax, lastRowinvaders.Count);
			var newMissileCount = missileCount - GameObject.FindGameObjectsWithTag ("InvaderMissile").Length;

			if (newMissileCount > 0) {
				GetComponent<AudioSource> ().PlayOneShot (shootAudio, 0.2f);
			}
			// This list is being used to make the missiles being fired by "random invaders"
			var attackedInvaderIndexList = new List<int> ();
			while (newMissileCount > 0) {
				var index = UnityEngine.Random.Range (0, lastRowinvaders.Count);
				if (!attackedInvaderIndexList.Exists (item => item == index)) {
					attackedInvaderIndexList.Add (index);
					newMissileCount--;
					var invader = lastRowinvaders [index];
					Instantiate(missilePrefab, invader.transform.position, invader.transform.rotation);	
				}
			}
		}
	}

	void Update ()
	{
		var invaders = GameObject.FindGameObjectsWithTag ("Invader");
		var player = FindObjectOfType<Player> ();

		if (player.died && !Cursor.visible) {
			Cursor.visible = true;
			Cursor.SetCursor(cursorTexture, Vector2.zero, cursorMode);
		}
		var gameNotHasResult = invaders.Length > 0 && player.died == false;
		if (gameNotHasResult) {
			updateSpeedAndPosition (invaders);
			cleanUp(invaders);
			if (invadersResponseTimer > invadersStartFiringSecondCount) {
				attack (invaders);
			} else {
				invadersResponseTimer += Time.deltaTime;
			}
		}
	}
}
