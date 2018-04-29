
/*

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
*/

//需要旧的Photon 插件才包含Lite，所以现在全部屏蔽
/*
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.Lite;
using System.Text;
using System;
using UnityEngine.SceneManagement;
*/
/*
public class User
{
    public int UserID;
    public string UserName;
}
*/
public class GameClient //: MonoBehaviour, IPhotonPeerListener
{
	/*
    //PhotonPeer peer;
    public static LitePeer peer;//作为静态共有对象供不同的场景使用
    Dictionary<int, string> actorList = new Dictionary<int, string>();

    public enum ChatEventCode : byte { Chat = 10}
    public enum ChatEventKey : byte { UserName = 11, ChatMessage = 12}
	*/
    void Start()
    {
		/*
        //peer = new PhotonPeer(this, ConnectionProtocol.Udp);
        peer = new LitePeer(this, ConnectionProtocol.Udp);
        peer.Connect("127.0.0.1:5055", "Lite");
        peer.TimePingInterval = 1000;

        //注册序列化方法，photon才能识别User类
        PhotonPeer.RegisterType(typeof(User), (byte)'U', SerializeUser, DeserializeUser);
		*/
    }
	/*
    static byte[] SerializeUser(object user)
    {
        User u = (User)user;
        byte[] strBytes = UTF8Encoding.Default.GetBytes(u.UserName);
        byte[] bytes = new byte[4 + strBytes.Length];
        int index = 0;
        Protocol.Serialize(u.UserID, bytes, ref index);
        strBytes.CopyTo(bytes, index);
        return bytes;
    }

    static object DeserializeUser(byte[] bytes)
    {
        User user = new User();
        int index = 0;
        Protocol.Deserialize(out user.UserID, bytes, ref index);
        user.UserName = UTF8Encoding.Default.GetString(bytes, index, bytes.Length - 4);
        return user;
    }

    // Update is called once per frame
    //性能优化,每秒执行10次，其实用FixUpdate更简单
    int timeSpanMS = 100;
    int nextSendTickCount = Environment.TickCount; //TickCount是系统总时间，相当于Time.time
    void Update()
    {
        if (Environment.TickCount > this.nextSendTickCount)
        {
            peer.Service();
            this.nextSendTickCount = Environment.TickCount + timeSpanMS;
        }
        
    }

    void SendMessage(OperationRequest operationRequest)
    {
        
        //Dictionary<byte, object> para = new Dictionary<byte, object>();
        //para[LiteOpKey.GameId] = "1";
        //peer.OpCustom(LiteOpKey.GameId, para, true);
        

        peer.OpCustom(operationRequest, true, 0, false);
    }

    public static string userName = "", message = "", chatMessage = "";
    void OnGUI()
    {
        userName = GUI.TextField(new Rect(Screen.width / 2 - 50, 70, 100, 20), userName);
        if(GUI.Button(new Rect(Screen.width/2 - 50, 100, 100, 30), "进入游戏房间"))
        {
            
            //OperationRequest op = new OperationRequest();
            //op.OperationCode = LiteOpCode.Join;
            //Dictionary<byte, object> para = new Dictionary<byte, object>();
            //para[LiteOpKey.GameId] = "youxiID";
            //op.Parameters = para;
            //SendMessage(op);
            


            // peer.OpJoin("jikexueyuan");

            
            //Hashtable gameProperties, actorProperties;
            //gameProperties = new Hashtable();
            //actorProperties = new Hashtable();
            //actorProperties.Add((byte)ChatEventKey.UserName, userName);
            //peer.OpJoin("极客学院", gameProperties, actorProperties, true);
            

            //改用传输User对象
            User user = new User();
            user.UserID = -1;
            user.UserName = userName;
            Hashtable gameProperties, actorProperties;
            gameProperties = new Hashtable();
            actorProperties = new Hashtable();
            actorProperties.Add((byte)ChatEventKey.UserName, user);
            peer.OpJoin("极客学院", gameProperties, actorProperties, true);

           // SceneManager.LoadScene("GameScene");
        }

        if(GUI.Button(new Rect(Screen.width/2 - 50, 150, 100, 30), "离开游戏房间"))
        {
            peer.OpLeave();
        }

        GUI.Label(new Rect(Screen.width / 2 - 50, 200, 150, 20), message);

        chatMessage = GUI.TextField(new Rect(Screen.width / 2 - 50, 250, 100, 20), chatMessage);
        if (GUI.Button(new Rect(Screen.width / 2 + 120, 250, 100, 20), "发送消息"))
        {
            Hashtable chatContent = new Hashtable();
            chatContent.Add(ChatEventKey.ChatMessage, chatMessage);
            peer.OpRaiseEvent((byte)ChatEventCode.Chat, chatContent, true);
        }

        if (GUI.Button(new Rect(Screen.width / 2 - 50, 300, 100, 20), "获得我的积分"))
        {
            OperationRequest op = new OperationRequest();
           // op.OperationCode = (byte)CustomerOperationCode.GetMyScore;
        }

        if (GUI.Button(new Rect(Screen.width / 2 - 50, 350, 100, 30), "跳转到新场景"))
        {
            //Application.LoadLevel("GameScene");
            SceneManager.LoadScene("GameScene");
        }
    }



    public void DebugReturn(DebugLevel level, string message)
    {
        //throw new System.NotImplementedException();
    }

    public void OnEvent(EventData eventData)
    {
        Debug.Log("触发了事件:" + eventData.ToStringFull());
        switch(eventData.Code)
        {
            
            case LiteEventCode.Join:
                {
                    string userName = ((Hashtable)eventData.Parameters[LiteEventKey.ActorProperties])[(byte)ChatEventKey.UserName].ToString();
                    message = "玩家" + userName + "进入了游戏房间";
                    int actorNum = (int)eventData.Parameters[LiteEventKey.ActorNr];
                    Debug.Log("join+++++++++++++actorNum:" + actorNum + ", userName:" + userName);
                    if (!actorList.ContainsKey(actorNum))
                    {
                        Debug.Log("11join+++++++++++++actorNum:" + actorNum + ", userName:" + userName);
                        actorList.Add(actorNum, userName);
                    }
                    else
                    {
                        Debug.Log("22join+++++++++++++actorNum:" + actorNum + ", userName:" + userName);
                        actorList[actorNum] = userName;
                    }
                }
                break;
                
            case LiteEventCode.Leave:
                {
                    
                    int actorNum = (int)eventData.Parameters[LiteEventKey.ActorNr];
                    Debug.Log("leave+++++++++++++actorNum:" + actorNum);
                    if (actorList.ContainsKey(actorNum))
                    {
                        string userName = actorList[actorNum];
                        message = "玩家" + userName + "离开了游戏房间";
                    }
                }
                break;

            case (byte)ChatEventCode.Chat:
                {
                    Hashtable customEventContent = eventData.Parameters[LiteEventKey.Data] as Hashtable;
                    string chatMessage = customEventContent[(byte)ChatEventKey.ChatMessage].ToString();
                    int actorNum = (int)eventData.Parameters[LiteEventKey.ActorNr];
                    if(actorList.ContainsKey(actorNum))
                    {
                        string userName = actorList[actorNum];
                        message = "玩家" + userName + "说：" + chatMessage;
                    }
                }
                break;
        }



    }

    public void OnOperationResponse(OperationResponse operationResponse)
    {
       // Debug.Log("服务器返回响应：" + operationResponse.Parameters[LiteOpKey.ActorNr]);
        switch(operationResponse.OperationCode)
        {
            case LiteOpCode.Join:
                {
                    int num = (int)operationResponse.Parameters[LiteOpKey.ActorNr];
                    Debug.Log("进入游戏房间，玩家编号为：" + num);
                }
                break;

            case LiteOpCode.Leave:
                {
                    Debug.Log("离开游戏房间");
                }
                break;

            default:
                break;
        }
    }

    public void OnStatusChanged(StatusCode statusCode)
    {
        switch(statusCode)
        {
            case StatusCode.Connect:
                Debug.Log("++++++++++++++连接成功！");
                break;
            case StatusCode.Disconnect:
                Debug.Log("+++++++++关闭连接, statusCode:" + statusCode);
                break;

            case StatusCode.ExceptionOnConnect:
                Debug.Log("++++++++++连接异常");
                break;
        }
    }

*/
}


