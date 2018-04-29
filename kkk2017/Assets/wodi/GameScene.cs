using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//需要旧的Photon 插件才包含Lite，所以现在全部屏蔽
/*
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.Lite;

public class GameScene : MonoBehaviour, IPhotonPeerListener
{
    public enum ClientEventTimeCode {  FreshTime = 3}
    public enum ClientEventTImeKey {  Time = 3}
    public enum ChatEventCode : byte { Chat = 10 }
    public enum ChatEventKey : byte { UserName = 11, ChatMessage = 12 }
    public string message;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        GameClient.peer.Service();
	}

    private void OnGUI()
    {
        //GameClient.peer.PeerState.ToString();
        GUI.Label(new Rect(Screen.width / 2 - 50, 100, 100, 20), GameClient.peer.PeerState.ToString());

        GUI.Label(new Rect(Screen.width / 2 - 50, 150, 100, 20), GameClient.message);
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        throw new System.NotImplementedException();
    }

    public void OnOperationResponse(OperationResponse operationResponse)
    {
        throw new System.NotImplementedException();
    }

    public void OnStatusChanged(StatusCode statusCode)
    {
        throw new System.NotImplementedException();
    }

    public void OnEvent(EventData eventData)
    {
        Debug.Log("gameScene触发了事件:" + eventData.ToStringFull());
    }
}
*/
