using UnityEngine;
using System.Collections;
using EENet;
using System;

public class TestEEClient : MonoBehaviour {


	private EEClient mClient; 

	// Use this for initialization
	void Start () {
		mClient = new EEClient();
		mClient.NetworkStateChangedEvent += (state) =>
		{
			Debug.Log("state change:" + state);
		};
		mClient.InitClient("127.0.0.1", 4321, connectToServerCallback);
		
	}


	void connectToServerCallback()
	{	
		Debug.Log("Connect to server success!");
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
