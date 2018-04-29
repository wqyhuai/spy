using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenRatio : MonoBehaviour {

	// Use this for initialization
	void Start () {
        int ManualWidth = 1280;
        int ManualHeight = 720;
        int manualHeight;
        Debug.Log("+++++++++++Screen.width:" + Screen.width + ", Screen.height:" + Screen.height);
        if (Convert.ToSingle(Screen.height) / Screen.width > Convert.ToSingle(ManualHeight)/ManualWidth)
        {
            Debug.Log("22+++++++++++Screen.width:" + Screen.width + ", Screen.height:" + Screen.height);
            manualHeight = Mathf.RoundToInt(Convert.ToSingle(ManualWidth) / Screen.width * Screen.height);
        }
        else
        {
            Debug.Log("33+++++++++++Screen.width:" + Screen.width + ", Screen.height:" + Screen.height);
            manualHeight = ManualHeight;
        }

        Camera camera = GetComponent<Camera>();
        float scale = Convert.ToSingle(manualHeight / ManualHeight);
        Debug.Log("screen ratio+++++++++++++++++++++scale:" + scale);
        camera.fieldOfView *= scale;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
