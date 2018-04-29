using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotate : MonoBehaviour {

    public Rigidbody rb;

    // Use this for initialization
    void Start()
    {
        //print("+++++hello");
        //		Debug.Log ("++++++hello");
        //		Debug.LogWarning ("++++++++++++warning");
        //		Debug.LogError ("++++++++++++++++++error");
        //rb = gameObject.GetComponent<Rigidbody>();

        //Time.fixedDeltaTime = 0.1f; 

    }

    //使用fixedUpdate的好处是每帧的间隔时间都一样
    void FixedUpdate()
    {
        //Debug.Log("+++++++++++FixedUpdate time:" + Time.deltaTime + ", time:" + Time.time);

    }


    //使用Update每帧间隔时间不一样，需要deltaTime来修正
    void Update()
    {

        //this.transform.Rotate(Vector3.up * Time.deltaTime *50, Space.World);
    }
}
