using UnityEngine;
using System.Collections;
using EENet;
using System;
using System.Collections.Generic;

public class TestEEClient : MonoBehaviour {


	private EEClient mClient; 

	// Use this for initialization
	void Start () {
		mClient = new EEClient();
		mClient.NetworkStateChangedEvent += (state) =>
		{
			Debug.Log("state change:" + state);
		};
		mClient.InitClient("39.106.112.186", 8001, connectToServerCallback);
		
	}


	void connectToServerCallback()
	{
		Debug.Log("Connect to server success!");
		mClient.StartReceivePacket();

		LoginMessage login = new LoginMessage();
		login.User = "111";
		login.Pwd = "222";
		mClient.Request("gate.authuser", login, AuthUserCallback);
	}

	private void AuthUserCallback(Dictionary<string, object> result)
	{
		Debug.Log("login result" + result);
		Debug.Log("result code:" + result["ErrCode"]);
		Debug.Log("result desc:" + result["ErrDesc"]);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
