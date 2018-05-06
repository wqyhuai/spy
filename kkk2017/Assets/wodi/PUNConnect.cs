using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ExitGames.Client.Photon;
using System.Text;
using System;
using UnityEngine.UI;


//using System;

public class Role
{
    public int id = -1;
    public bool isSpy = false;
    public bool isDeath = false;
    public int killID = -1;//杀了谁
    public string name = "";
    public string objectName = "";
    
    public void print()
    {
        Debug.Log("Role+++++++id:" + id + ", isSpy:" + isSpy + ", isDeath:" + isDeath
            + ",killID:" + killID + ", name:" + name + ", objectName:" + objectName);
    }
    
}
public class RoleList
{
    public List<Role> roleList;
}
//游戏规则：暂定死一个平民或者积分为0就平民输，卧底全死或者超时了积分还不为0就卧底输了。
//平民杀错队友就输
//卧底不可杀人，否则一下杀光所有人，倒计时也没用（可以考虑多个卧底的时候能杀人，杀错自己人就输）
public class PUNConnect : MonoBehaviour, IPunCallbacks, IPunObservable
{
    public GameObject diskObject;
    public GameObject endScene;
    public GUIStyle customSelfStyle;
    public GUIStyle customStyle;
    public GUIStyle customIdentifyStyle;
    public GUIStyle customScoreStyle;
    public GUIStyle customCDStyle;
    public GUIStyle customSkillStyle;

    //public const int GAME_SCORE = 20;
    public static int gameScore = 0;
    private static int timeLeft = 0; //剩余时间
    private int rotateSpeed = 0; //旋转速度，每秒80度
    private int pointCount = 0; //多少个人指出才杀人
    private int roundCount = 0; //多少圈没跌倒才奖励冰冻技能

    private float roundTime = 0; //转一圈的时间：秒
    private DateTime lastFallDownTime;
    private int propCount = 0;
    private DateTime selfFreezeTime;

    public static GameObject s_endScene;

    private int selfID = -1;
    public static bool isHost = false;
    public static Role selfRole = new Role();
    private PhotonPlayer selfPlayer;
    public static List<Role> roleList = new List<Role>();
    public static string gameResultReason = "";


    public enum GameState {GameInit, JoinedLobby, JoinedRoom, GameStart, LeaveGame, SpyWin, SpyLose }
    public enum ProtocolCode {Host_PlayerJoined, Host_RejectJoin, Host_PlayerLeave, Host_SynParams,
        Host_GameStart,All_word, Guest_KillOtherRequest, Host_KillOtherResult, Host_SynRolesInfo,
        Host_GameEnd, All_StandUp, All_Freeze
    }
    public static GameState gameState = GameState.GameInit;

    //private int rolesAllocation = 0;
    public static Vector2 NativeResolution = new Vector2(640, 360);
    private static float _guiScaleFactor = -1.0f;
    private static Vector3 _offset = Vector3.zero;
    static List<Matrix4x4> stack = new List<Matrix4x4>();
    //static bool _didResizeUI = false;
    public void BeginUIResizing()
    {
        Vector2 nativeSize = NativeResolution;
        //_didResizeUI = true;

        stack.Add(GUI.matrix);
        Matrix4x4 m = new Matrix4x4();
        var w = (float)Screen.width;
        var h = (float)Screen.height;
        var aspect = w / h;
        var offset = Vector3.zero;
        if(aspect < (nativeSize.x/nativeSize.y))
        {
            _guiScaleFactor = (Screen.width / nativeSize.x);
            offset.y += (Screen.height - (nativeSize.y * _guiScaleFactor)) * 0.5f;
            //Debug.Log("11+++++++++++offset:" + offset);
        }
        else
        {
            _guiScaleFactor = (Screen.height / nativeSize.y);
            offset.x += (Screen.width - (nativeSize.x * _guiScaleFactor)) * 0.5f;
            //Debug.Log("22+++++++++++offset:" + offset);
        }

        m.SetTRS(offset, Quaternion.identity, Vector3.one * _guiScaleFactor);
        _offset = offset/_guiScaleFactor;
        //Debug.Log("33+++++++++++_offset:" + _offset + ", _guiScaleFactor:" + _guiScaleFactor);
        GUI.matrix *= m;
    }

    public void EndUIResizing()
    {
        GUI.matrix = stack[stack.Count - 1];
        stack.RemoveAt(stack.Count - 1);
        //_didResizeUI = false;
    }

    // Use this for initialization
    void Start () {
        PhotonNetwork.ConnectUsingSettings("1.0");
        //UnityEngine.Random rd = new UnityEngine.Random();

        PhotonNetwork.OnEventCall += this.OnEvent;

        PhotonPeer.RegisterType(typeof(Role), (byte)'R', SerializeRole, DeSerializeRole);
        PhotonNetwork.autoCleanUpPlayerObjects = true;

        //加载数据到input控件
        loadName();
        loadData("MaxScore", 20, "scoreInput");
        loadData("MaxTime", 100, "timeInput");
        loadData("MaxSpeed", 80, "speedInput");
        loadData("MaxPoint", 2, "pointCountInput");
        loadData("MaxRound", 3, "RoundInput");


        s_endScene = endScene;
    }

    void Update()
    {
        if (gameState == GameState.GameStart)
        {
            if (isHost)
            {
                diskObject.transform.Rotate(Vector3.up * Time.deltaTime * rotateSpeed, Space.World);
            }
            
            if (selfRole.isSpy)
            {
                TimeSpan timeDiff = DateTime.Now - lastFallDownTime;
                //roundCount圈内没跌倒就给一个道具
                if (timeDiff.TotalSeconds >= roundCount * roundTime)
                {
                    propCount++;
                    lastFallDownTime = DateTime.Now;
                }
            }
        }
    }

    void FixedUpdate()
    {
        for (int i = 0; i < roleList.Count; i++)
        {
            Role srcRole = roleList[i];

            if (srcRole.killID >= 0)
            {
                Role destRole = getRoleByID(srcRole.killID);
                if (destRole == null)
                {
                    Debug.LogWarning("FixedUpdate not found role+++++++++++++srcRole.killID:" + srcRole.killID);
                    continue;
                }
                showLaserEffect(srcRole.objectName, destRole.objectName);
            }
        }
    }

    private void OnGUI()
    {
        BeginUIResizing();

        //_offset x负数向左靠，正数向右靠；y负数向上靠，正数向下靠
        int diffY = 0;
        string strHost = isHost ? "房主" : "房客";
        string strIdentify = "身份";
        if (selfRole != null)
        {
            strIdentify = selfRole.isSpy ? "你是卧底" : "你是平民";
        }

        if (gameState == GameState.JoinedRoom)
        {
            GUI.Label(new Rect(0 - _offset.x + 20, diffY - _offset.y, 50, 20), PhotonNetwork.room.Name);
        }
        GUI.Label(new Rect(0 - _offset.x + 70, diffY - _offset.y, 50, 20), strHost);
        GUI.Label(new Rect(0 - _offset.x + 100, diffY - _offset.y, 50, 20), PhotonNetwork.connectionStateDetailed.ToString());
        if (gameScore > 0 && gameScore < 5)
        //if(true)
        {
            GUI.Label(new Rect(NativeResolution.x - 120 + _offset.x, NativeResolution.y/2 - 50, 50, 20), "积分:" + gameScore, customScoreStyle);
        }
        else
        {
            GUI.Label(new Rect(0 - _offset.x + 170, diffY - _offset.y, 50, 20), "积分:" + gameScore);
        }

        if (timeLeft > 0 && timeLeft < 5)
        //if(true)
        {
            GUI.Label(new Rect(NativeResolution.x - 120 + _offset.x, NativeResolution.y/2, 100, 20), 
                "时间:" + timeLeft, customCDStyle);
        }
        else
        {
            GUI.Label(new Rect(0 - _offset.x + 230, diffY - _offset.y, 100, 20), 
                "时间:" + timeLeft);
        }
            

        if (gameState == GameState.GameStart || gameState == GameState.SpyWin || gameState == GameState.SpyLose)
        {
            GUI.Label(new Rect(NativeResolution.x/2 - 40, NativeResolution.y - 40 + _offset.y, 50, 20), 
                strIdentify, customIdentifyStyle);
        }

        if (gameState == GameState.SpyWin || gameState == GameState.SpyLose)
        {
            return;
        }

        if (!PhotonNetwork.inRoom && PhotonNetwork.connectionStateDetailed.ToString() == "JoinedLobby")
        {
            Rect rect = new Rect(NativeResolution.x / 2 - 50 - _offset.x, 40 + diffY - _offset.y, 70, 30);
            if (GUI.Button(rect, "加入房间"))
            {
                setName();
                if (PhotonNetwork.connected)
                {
                    //Debug.Log("++++++++++++PhotonNetwork.connectionStateDetailed.ToString():"
                    //     + PhotonNetwork.connectionStateDetailed.ToString());
                    //PhotonNetwork.CreateRoom("极客学院", new RoomOptions { MaxPlayers = 16 }, null);
                    //必须用JoinOrCreateRoom才能同步PhotonView物体
                    PhotonNetwork.JoinOrCreateRoom("room1", new RoomOptions { MaxPlayers = 16 }, null);
                }
            }
        }


        if (PhotonNetwork.inRoom)
        {
            Rect rect = new Rect(NativeResolution.x / 2 - 50 - _offset.x, 40 + diffY - _offset.y, 70, 30);
            if (GUI.Button(rect, "退出房间"))
            {
                gameState = GameState.JoinedLobby;
                roleList.Clear();
                PhotonNetwork.LeaveRoom();
            }
        }

        //PhotonNetwork.inRoom与gameState == GameState.JoinedRoom是同时出现的，为保险起见都判断
        if (PhotonNetwork.connected && isHost && PhotonNetwork.inRoom && gameState == GameState.JoinedRoom)
        {
            Rect rect = new Rect(NativeResolution.x / 2 + 50 - _offset.x, 40 + diffY - _offset.y, 70, 30);
            if (GUI.Button(rect, "开始游戏"))
            {
                gameInit();

                StartCoroutine(gameStart());
            }
        }


        /*
        if (GUI.Button(new Rect(NativeResolution.x / 2 + 150 - _offset.x, 30 + diffY - _offset.y, 50, 30), "测试"))
        {
            GameObject disk = getObjectByName("disk");
            Debug.Log("test begin+++++++++++++++disk:" + disk.name);

        }
        */
        //显示玩家列表
        if (PhotonNetwork.inRoom)
        {
            for (int i = 0; i < roleList.Count; i++)
            {
                Role r = roleList[i];
                //Debug.Log("++++++++++i:" + i + ", name:" + r.name);
                int y = 70 + 40 * i;
                if (r.id == selfRole.id)
                {
                    GUI.Label(new Rect(20 - _offset.x, y, 70, 20), r.name, customSelfStyle);
                }
                else
                {
                    //根据状态显示不同的button颜色
                    if (GUI.Button(new Rect(20 - _offset.x, y, 70, 20), r.name, customStyle))
                    {
                        if (gameState == GameState.GameStart)
                        {
                            pointSpy(selfRole, r);
                            int[] ids = new int[] { selfRole.id, r.id };
                            PhotonNetwork.RaiseEvent((byte)ProtocolCode.All_word, ids, true, null);
                        }
                    }
                }
            }
        }

        if (gameState == GameState.GameStart && propCount > 0)
        {
            GUI.Label(new Rect(NativeResolution.x / 2 + 100, NativeResolution.y - 40 + _offset.y, 50, 20),
                "冰冻技能：" + propCount, customSkillStyle);

            for (int i = 0; i < roleList.Count; i++)
            {
                Role r = roleList[i];
                if (r.id == selfID)
                {
                    continue;
                }
                //Debug.Log("++++++++++i:" + i + ", name:" + r.name);
                int y = 70 + 40 * i;
                if (GUI.Button(new Rect(NativeResolution.x - 70 - _offset.x, y, 70, 20), r.name, customStyle))
                {
                    propCount--;
                    int[] ids = new int[] { selfID, r.id };
                    PhotonNetwork.RaiseEvent((byte)ProtocolCode.All_Freeze, ids, true, null);
                }
            }
        }

        EndUIResizing();
    }

    public void selfFallDown(string objName)
    {
        Debug.Log("+++++++++++++falldown");
        if (objName == selfRole.objectName)
        {
            lastFallDownTime = DateTime.Now;
        }
        
        gameScore--;
    }

    public bool canJump()
    {
        Debug.LogWarning("canJump+++++++++++++++++++++");
        if (DateTime.Compare(selfFreezeTime, DateTime.Now) > 0)
        {
            Debug.LogWarning("+++++++++++++++++你被卧底冰冻，一圈内不能起跳");
            showTips("你被卧底冰冻，一圈内不能起跳", 1f);
            return false;
        }

        return true;
    }

    void TimeSchedule()
    {
        if (timeLeft > 0)
        {
            timeLeft--;
        }

        if (timeLeft <= 0)
        {
            checkGameEnd();
        }
        else
        {
            Invoke("TimeSchedule", 1.0f);
        }
    }

    public static Role getRoleByID(int id)
    {
        for (int i = 0; i < roleList.Count; i++)
        {
            if (roleList[i].id == id)
            {
                return roleList[i];
            }
        }

        Debug.LogWarning("getRoleByID+++++++++++++++++++++not found id:" + id);
        return null;
    }

    public int getIndexByID(int id)
    {
        for (int i = 0; i < roleList.Count; i++)
        {
            if (roleList[i].id == id)
            {
                return i;
            }
        }

        Debug.LogWarning("getIndexByID+++++++++++++++++++++not found id:" + id);
        return -1;
    }

    public static Role getRoleByObjectName(string objName)
    {
        for (int i = 0; i < roleList.Count; i++)
        {
            if (roleList[i].objectName == objName)
            {
                return roleList[i];
            }
        }
        return null;
    }

    public GameObject getObjectByName(string name)
    {
        GameObject[] pAllObjects = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));

        foreach (GameObject pObject in pAllObjects)
        {
            if (pObject.name == name)
            {
                return pObject;
            }
        }

        return null;
    }
 
    static byte[] SerializeRole(object role)
    {
        Role r = (Role)role;
        byte[] nameBytes = UTF8Encoding.Default.GetBytes(r.name);
        byte[] objectNameBytes = UTF8Encoding.Default.GetBytes(r.objectName);
        //sizeof(int) + 2*sizeof(short) + int + int + int = 20
        byte[] bytes = new byte[20 + nameBytes.Length + objectNameBytes.Length];
        
        int index = 0;
        Protocol.Serialize(r.id, bytes, ref index);
        short spy = (r.isSpy ? (short)1 : (short)0);
        Protocol.Serialize(spy, bytes, ref index);
        short death = (r.isDeath ? (short)1 : (short)0);
        Protocol.Serialize(death, bytes, ref index);
        Protocol.Serialize(r.killID, bytes, ref index);


        int nameLength = nameBytes.Length;
        Protocol.Serialize(nameLength, bytes, ref index);
        nameBytes.CopyTo(bytes, index);
        index += nameBytes.Length;

        Protocol.Serialize(objectNameBytes.Length, bytes, ref index);
        objectNameBytes.CopyTo(bytes, index);
        index += objectNameBytes.Length;

        return bytes;
    }

    static object DeSerializeRole(byte[] bytes)
    {
        Role role = new Role();
        int index = 0;
        Protocol.Deserialize(out role.id, bytes, ref index);
        short spy = -1;
        Protocol.Deserialize(out spy, bytes, ref index);
        role.isSpy = (spy == 1);
        short death = -1;
        Protocol.Deserialize(out death, bytes, ref index);
        role.isDeath = (death == 1);

        Protocol.Deserialize(out role.killID, bytes, ref index);

        int nameLength = 0;
        Protocol.Deserialize(out nameLength, bytes, ref index);
        role.name = UTF8Encoding.Default.GetString(bytes, index, nameLength);
        index += nameLength;
        int objectNameLength = 0;
        Protocol.Deserialize(out objectNameLength, bytes, ref index);
        role.objectName = UTF8Encoding.Default.GetString(bytes, index, objectNameLength);
        index = objectNameLength;

        Debug.Log("DeSerializeRole++++++++++++name:" + role.name + ", isDeath:" + role.isDeath);
        return role;
    }
    /*
    static byte[] SerializeRoleList(object obj)
    {
        RoleList rl = (RoleList)obj;
        List<Role> list = rl.roleList;
        int length = 0;
        for (int i = 0; i < list.Count; i++)
        {
            byte[] buffer = SerializeRole(list[i]);
            length += 4;
            length += buffer.Length;
        }
        Debug.Log("SerializeRoleList++++++++++++++++++++begin");
        byte[] bytes = new byte[length];
        int index = 0;
        for (int i = 0; i < list.Count; i++)
        {
            Debug.Log("SerializeRoleList++++++++i:" + i + " id:" + list[i].id + ", name:" + list[i].name);
            byte[] buffer = SerializeRole(list[i]);
            Debug.Log("SerializeRoleList+++++++++++++++i:" + i + ", buffer.Length:" + buffer.Length);
            Protocol.Serialize(buffer.Length, bytes, ref index);
            Debug.Log("11+++++++++++++++++++++++index:" + index + ", buffer:" + buffer);
            buffer.CopyTo(bytes, index);
            index += buffer.Length;
            Debug.Log("22+++++++++++++++++++++++index:" + index);
        }

        Debug.Log("SerializeRoleList++++++++++++++++length:" + bytes.Length);
        Debug.Log("SerializeRoleList++++++++++++++++++++end");
        return bytes;
    }

    static object DeSerializeRoleList(byte[] bytesList)
    {
        Debug.Log("DeSerializeRoleList++++++++++++++++length:" + bytesList.Length);
        int index = 0;
        List<Role> list = new List<Role>();
        while(index < bytesList.Length - 1)
        {
            int roleLength = 0;
            Protocol.Deserialize(out roleLength, bytesList, ref index);
            byte[] roleByte = bytesList.Skip(4).Take(roleLength).ToArray();
            Debug.Log("11++++++++++++++index" + index + ",  roleLength:" + roleLength 
                + ", bytesList.Length:" + bytesList.Length);
            index += roleLength;
            Debug.Log("22++++++++++roleByte length:" + roleByte.Length);
           // Debug.Log("33++++++++++roleByte:" + roleByte);
            Role r = (Role)DeSerializeRole(roleByte);
            Debug.Log("44DeSerializeRoleList++++++++id:" + r.id + ", name:" + r.name + ", objectName:" + r.objectName);
            list.Add(r);
        }

        RoleList rl = new RoleList();
        rl.roleList = list;
        return rl;
  
    }
    */
    // Update is called once per frame


    void loadName()
    {
        if (PlayerPrefs.HasKey("MyName"))
        {
            string name = PlayerPrefs.GetString("MyName");
            GameObject.Find("settingCanvas").transform.Find("InputField").GetComponent<InputField>().text = name;
        }
    }
    void setName()
    {
        string name = GameObject.Find("settingCanvas").transform.Find("InputField").transform.Find("Text").GetComponent<Text>().text;
        if (name == "")
        {
            name = "wqy" + UnityEngine.Random.Range(0, 1000);
            GameObject.Find("settingCanvas").transform.Find("InputField").GetComponent<InputField>().text = name;
        }

        Debug.Log("setName+++++++++++++++++ name:" + name);
        PlayerPrefs.SetString("MyName", name);
        PhotonNetwork.playerName = name;
    }

    void loadData(string key, int defaultValue, string inputName)
    {
        if (PlayerPrefs.HasKey(key))
        {
            int value = PlayerPrefs.GetInt(key);
            GameObject.Find("settingCanvas").transform.Find(inputName).GetComponent<InputField>().text = value + "";
        }
        else
        {
            GameObject.Find("settingCanvas").transform.Find(inputName).GetComponent<InputField>().text = defaultValue + "";
        }
    }
    int saveData(string key, int value, string inputName)
    {
        int v = 0;
        if (value > 0) //传进来的
        {
            v = value;
        }
        else //小于0则从input控件读取
        {
            string str = GameObject.Find("settingCanvas").transform.Find(inputName).transform.Find("Text").GetComponent<Text>().text;
            if (str != "")
            {
                v = Convert.ToInt32(str);
            }
        }

        
        if (v == 0)
        {
            v = 50;
        }

        GameObject.Find("settingCanvas").transform.Find(inputName).GetComponent<InputField>().text = v + "";
        PlayerPrefs.SetInt(key, v);
        return v;
    }

    int getIdentify(int rolesAllocation, int selfIndex)
    {
        int result = rolesAllocation & (1 << selfIndex);

        return (result == 0) ? 0 : 1;
    }

    public void showTips(string tips, float time)
    {
        GameObject text = GameObject.Find("tipsCanvas/Text");
        if (text == null)
        {
            Debug.LogWarning("+++++++++++++++++++tipsCanvas is null");
            return;
        }
        text.GetComponent<Text>().text = tips;
        Vector3 scale = text.transform.localScale;
        // Vector3 position = text.transform.position;
        //position.x = 0;
        //text.transform.position = position;
        text.transform.localScale = new Vector3(1, 1, 1);

        StartCoroutine(DelayToInvoke(delegate ()
        {
            text.GetComponent<Text>().text = "初始化";
            //position.x = 10000;
            //text.transform.position = position;
            text.transform.localScale = new Vector3(0, 0, 0);
        }, time));
    }

    private void createSelfRole()
    {
        gameState = GameState.JoinedRoom;

        selfID = PhotonNetwork.player.ID;

        selfRole.id = PhotonNetwork.player.ID;
        selfRole.name = PhotonNetwork.playerName;
        selfRole.isDeath = false;
        Debug.Log("自己加入房间createSelfRole+++++++++++++++++++++++++++++name:" + PhotonNetwork.room.Name
            + ", playerCount:" + PhotonNetwork.room.PlayerCount + ", selfID:" + selfID
            + ", roomString:" + PhotonNetwork.room.ToString() 
            + ", roomStringFull:" + PhotonNetwork.room.ToStringFull());
        if (PhotonNetwork.room.PlayerCount == 1)
        {
            isHost = true;

            //房主才添加
            Role r = new Role();
            r.id = PhotonNetwork.player.ID;
            r.name = PhotonNetwork.playerName;
            r.isDeath = false;
            r.isSpy = false;
            roleList.Add(r);
            Debug.Log("+++++++++++++++++roleList count:" + roleList.Count);
        }
        else
        {
            isHost = false;
        }
        
    }

    private void otherPlayerJoint(PhotonPlayer player)
    {
        Debug.Log("11otherPlayerJoint+++++++++player name:" + player.NickName + ", player.ID:" + player.ID);

        //房主才判断
        if (!isHost)
        {
            return;
        }

        //添加玩家
        Role r = new Role();
        r.id = player.ID;
        r.name = player.NickName;
        r.isDeath = false;
        r.isSpy = false;
        roleList.Add(r);

        
        Debug.Log("+++++++++++++++++roleList count:" + roleList.Count);
        if (isHost)
        {
            if (gameState != GameState.GameStart)
            {
                Role[] content = roleList.ToArray();
                PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_PlayerJoined, content, true, null);
            }
            else
            {
                int[] content = new int[] { player.ID };
                PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_RejectJoin, content, true, null);
            }
            
        }
    }


    private void gameInit()
    {
        //设置参数并同步
        gameScore = saveData("MaxScore", -1, "scoreInput");
        timeLeft = saveData("MaxTime", -1, "timeInput");
        rotateSpeed = saveData("MaxSpeed", -1, "speedInput");
        pointCount = saveData("MaxPoint", -1, "pointCountInput");
        roundCount = saveData("MaxRound", -1, "RoundInput");

        roundTime = 360 / (float)rotateSpeed;
        Debug.LogWarning("gameInit+++++++++rotateSpeed:" + rotateSpeed + ",roundTime:" + roundTime);
        int[] param = new int[] { gameScore, timeLeft, rotateSpeed, pointCount, roundCount };
        PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_SynParams, param, true, null);

        //创建角色数据和物体对象
        allocateRoles();
        selfRole = roleList[0];
       // createObject(0, roleList.Count);
        //同步角色数据
        Role[] content = roleList.ToArray();
        PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_GameStart, content, true, null);        
    }

    public IEnumerator gameStart()
    {
        if (selfPlayer != null)
        {
            PhotonNetwork.DestroyPlayerObjects(selfPlayer);
        }

        createObject(getIndexByID(selfRole.id), roleList.Count);

        propCount = 0;
        lastFallDownTime = DateTime.Now;
        selfFreezeTime = DateTime.Now;

        diskObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        diskObject.transform.position = new Vector3(0, 1, 0);

        GameObject cd = GameObject.Find("cdCanvas/cd");
        cd.transform.localScale = new Vector3(1, 1, 1);
        for (int i = 3; i > 0; i--)
        {
            cd.transform.Find("Text").GetComponent<Text>().text = i + "";
            yield return new WaitForSeconds(1f);
        }
        cd.transform.localScale = new Vector3(0, 0, 0);

        gameState = GameState.GameStart;
        //gameScore = GAME_SCORE;
        //timeLeft = TOTAL_GAME_TIME;
        createTitleName();
        initKillList();
        Invoke("TimeSchedule", 1.0f);
    }
    private void allocateRoles()
    {
        //房主才判断
        if (!isHost)
        {
            return;
        }
        //int playerCount = PhotonNetwork.room.PlayerCount;
        int playerCount = roleList.Count;
        //playerCount = 6;
        if (playerCount <= 0)
        {
            return;
        }

        int spyCount = 1;
        if (playerCount >= 6)
        {
            spyCount = 2;
        }


        //要判断当前随机数是否一样，一样则以时间戳加上100作为种子再算
        //UnityEngine.Random rd = new UnityEngine.Random();
        int spy1 = UnityEngine.Random.Range(0, playerCount);
        //spy1 = 0;
        int spy2 = -1;
        if (spyCount > 1)
        {
            spy2 = UnityEngine.Random.Range(0, playerCount - 2);
            if (spy2 >= spy1)
            {
                spy2++;
            }
        }
        //spy1 = 3;

        for (int i = 0; i < roleList.Count; i++)
        {
            roleList[i].objectName = "role" + i + "(Clone)";
            roleList[i].isDeath = false;
            roleList[i].killID = -1;

            if (i == spy1 || i == spy2)
            {
                roleList[i].isSpy = true;
            }
            else
            {
                roleList[i].isSpy = false;
            }
        }

        
        /*
        rolesAllocation = 0;
        for (int i = 0; i < playerCount; i++)
        {
            rolesAllocation = rolesAllocation << 1;
            if (i == spy1 || i == spy2)
            {
                rolesAllocation += 1;
            }
        }
        */
        //Debug.Log("33333++++++++++++spy1:" + spy1 + ", spy2:" + spy2 + ", rolesAllocation:" + rolesAllocation + ", playerCount:" + playerCount);
    }
    
    //只有自己才会创建object
    public void createObject(int i, int playerCount)
    {
        GameObject obj = PhotonNetwork.Instantiate("role" + i, new Vector3(0f, 1f, -2.5f), Quaternion.identity, 0);
        float angle = 360 / playerCount;
        obj.transform.RotateAround(diskObject.transform.position, Vector3.up, i * angle);

        obj.AddComponent<Rigidbody>();

        obj.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public Vector3 resetObject(GameObject obj)
    {
        //让速度变为0
        obj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        obj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        //role.GetComponent<Rigidbody>().velocity = Vector3.zero;//光设速度为0不行的，位置不变但rotation还是会变
        //恢复旋转
        obj.transform.rotation = Quaternion.Euler(0, 0, 0);

        //恢复位置
        Vector3 center = diskObject.transform.position;
        Vector3 originalRolePoint = new Vector3(0f, 2f, -2.5f);
        //float angle = index * 90;
        float angle = PUNConnect.getAngle(obj.name);
        Vector3 axis = Vector3.up;
        Vector3 point = Quaternion.AngleAxis(angle, axis) * (originalRolePoint - center);
        Debug.Log("standup++++++++++++point:" + point + ", center:" + center + ", name:" + obj.name);
        obj.transform.position = point;

        return point;
    }

    public static float getAngle(string objName)
    {
        for (int i = 0; i < roleList.Count; i++)
        {
            if (roleList[i].objectName == objName)
            {
                float angle = 360 / roleList.Count;
                angle *= i;
                return angle;
            }
        }

        Debug.LogWarning("++++++++++++++++not found objectName:" + objName);
        return -1;
    }


    public void checkGameEnd()
    {
        if (!isHost || gameState != GameState.GameStart)
        {
            return;
        }

        int result = checkResult();
        if (result == 0)
        {

        }
        else
        {
            if (result == 1)
            {
                gameState = GameState.SpyWin;
            }
            else if (result == 2)
            {
                gameState = GameState.SpyLose;
            }


            Role[] content = roleList.ToArray();
            PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_SynRolesInfo, content, true, null);


            string[] strReusltReason = new string[] {gameState + "", gameResultReason };
            PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_GameEnd, strReusltReason, true, null);

            StartCoroutine(gameOver());
            //gameOver();
        }

    }
    public IEnumerator gameOver()
    {
        yield return new WaitForSeconds(0.2f);
        /*
        StartCoroutine(DelayToInvoke(delegate ()
        {
            PhotonNetwork.DestroyPlayerObjects(selfPlayer);
        }, 3f));
        */
        s_endScene.SetActive(true);
        String winner = "胜利者";
        if (gameState == GameState.SpyWin)
        {
            winner = selfRole.isSpy ? "你赢了" : "你输了";
        }
        else if (gameState == GameState.SpyLose)
        {
            winner = selfRole.isSpy ? "你输了" : "你赢了";
        }
        else
        {
            winner = "胜利状态异常";
        }
        s_endScene.transform.Find("gameOver").GetComponent<Text>().text = winner;
        s_endScene.transform.Find("overReason").GetComponent<Text>().text = gameResultReason;

        string text = "";
        for (int i = 0; i < roleList.Count; i++)
        {
            Role srcRole = roleList[i];
            string strIdentify = srcRole.isSpy ? "卧底" : "平民";
            string strBeKill = "";
            
            if (srcRole.killID != -1)
            {
                Role destRole = getRoleByID(srcRole.killID);
                if (destRole.isDeath)
                {
                    if (destRole.isSpy == false && srcRole.isSpy == false)
                    {
                        strBeKill += "错杀了队友：" + destRole.name;
                    }
                    else
                    {
                        strBeKill += "杀了：" + destRole.name;
                    }
                }
            }

            text += srcRole.name + "    " + strIdentify + "   " + strBeKill + "\n";
        }

        Debug.Log("+++++++++++++++show text:" + text);
        s_endScene.transform.Find("list").GetComponent<Text>().text = text;

    }

    public void OnButtonRetry()
    {
        Debug.Log("OnButtonRetry+++++++++++++++++++++");
        s_endScene.SetActive(false);
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (PhotonNetwork.inRoom)
        {
            if (gameState != GameState.GameStart)
            {
                gameState = GameState.JoinedRoom;
            }
        }
        else
        {
            
        }
        //
    }

    public static int checkResult()
    {
        int result = 0;//0未结束, 1卧底胜利，2平民胜利

        //时间结束积分不为0，平民胜利
        if (timeLeft <= 0 && gameScore > 0)
        {
            result = 2;
            gameResultReason = "平民胜利，时间结束";
            return result;
        }


        int livingSpyCount = 0;//活着的卧底数量
        for (int i = 0; i < roleList.Count; i++)
        {
            Role role = roleList[i];
            if (!role.isSpy && role.isDeath) //卧底胜利，死了一个平民
            {
                result = 1;
                gameResultReason = "卧底胜利，平民错杀一个队友";
                return result;
            }

            if (role.isSpy && !role.isDeath)
            {
                livingSpyCount++;
            }
        }

        if (gameScore <= 0) //卧底胜利，积分为0
        {
            result = 1;
            gameResultReason = "卧底胜利，积分为0";
            return result;
        }

        if (livingSpyCount <= 0)//平民胜利，卧底被杀光了
        {
            result = 2;
            gameResultReason = "平民胜利，卧底被杀光了";
            return result;
        }

        return result;
    }

    void OnEvent(byte eventcode, object content, int senderid)
    {
        Debug.Log("kkk2017++++++++++eventcode:" + eventcode + ", content:" + content + ", senderid:" + senderid);
        if (eventcode == (byte)ProtocolCode.Host_PlayerJoined)
        {
            roleList.Clear();

            Role[] roles = content as Role[];
            int count = roles.Length;
            for (int i = 0; i < count; i++)
            {
                roleList.Add(roles[i]);
            }
        }
        else if (eventcode == (byte)ProtocolCode.Host_RejectJoin)
        {
            int[] ids = content as int[];
            if (ids[0] == selfID)
            {
                showTips("房间正在游戏中，请稍后加入", 3f);

                gameState = GameState.JoinedLobby;
                roleList.Clear();
                PhotonNetwork.LeaveRoom();
            }
        }
        else if (eventcode == (byte)ProtocolCode.Host_SynParams)
        {
            int[] param = content as int[];
            gameScore = saveData("MaxScore", param[0], "scoreInput");
            timeLeft = saveData("MaxTime", param[1], "timeInput");
            rotateSpeed = saveData("MaxSpeed", param[2], "speedInput");
            pointCount = saveData("MaxPoint", param[3], "pointCountInput");
            roundCount = saveData("MaxRound", param[4], "RoundInput");
            roundTime = 360 / (float)rotateSpeed;
        }
        else if (eventcode == (byte)ProtocolCode.Host_GameStart)//gameStart
        {
            if (!isHost)
            {
                s_endScene.SetActive(false);
                roleList.Clear();
                Role[] roles = content as Role[];
                

                int count = roles.Length;
                for (int i = 0; i < count; i++)
                {
                    roleList.Add(roles[i]);

                    if (roles[i].id == selfID)
                    {
                        selfRole = roles[i];
                        //createObject(i, count);
                    }
                }

				selfRole.isDeath = false;

                StartCoroutine(gameStart());
            }
        }
        else if (eventcode == (byte)ProtocolCode.All_word)
        {
            int[] ids = content as int[];
            Role sayRole = getRoleByID(ids[0]);
            Role spyRole = getRoleByID(ids[1]);

            pointSpy(sayRole, spyRole);
        }
        else if (eventcode == (byte)ProtocolCode.Guest_KillOtherRequest) //杀死玩家的消息
        {
            if (!isHost)
            {
                return;
            }
            //PhotonPlayer sender = PhotonPlayer.Find(senderid); 
            string[] selected = content as string[];
            string attackObjectName = selected[0];
            string deathObjectName = selected[1];

            attack(attackObjectName, deathObjectName);
        }
        else if (eventcode == (byte)ProtocolCode.Host_KillOtherResult) //房主收到杀死玩家的消息后广播玩家死亡状态
        {

            int[] ids = content as int[];
            int srcID = ids[0];
            Role srcRole = getRoleByID(srcID);
            int DestID = ids[1];
           // Debug.Log("Host_KillOtherResult+++++++ids0:" + ids[0] + ", ids1:" + ids[1] + ", ids2:" + ids[2]);
            for (int i = 0; i < roleList.Count; i++)
            {
                if (roleList[i].id == srcID)
                {
                    roleList[i].killID = DestID;
                    //showLaserEffect(srcRole.objectName, roleList[i].objectName);
                }

                if (roleList[i].id == DestID)
                {
                    roleList[i].isDeath = (ids[2] == 1);
                    //Debug.Log("Host_KillOtherResult+++++srcName:" + srcRole.name + ", destName:" + roleList[i].name 
                    //   + ", destID:" + DestID + ", id2:" + ids[2] + "isDeath:" + roleList[i].isDeath);

                }

                if (selfID == srcID)
                {
                    selfRole = roleList[i];
                }
            }
        }
        else if (eventcode == (byte)ProtocolCode.Host_SynRolesInfo)//游戏结束
        {
            Role[] roles = content as Role[];

            setRolesInfo(roles);
        }
        else if (eventcode == (byte)ProtocolCode.Host_GameEnd)//游戏结束
        {
            string[] result = content as string[];
            gameState = (result[0] == "SpyLose")? GameState.SpyLose : GameState.SpyWin;
            Debug.Log("Host_GameEnd++++++++++++result0:" + result[0] + ", result1:" + result[1]
                + ", gameState:" + gameState);
            gameResultReason = result[1];
            //gameOver();
            StartCoroutine(gameOver());
        }
        else if(eventcode == (byte)ProtocolCode.All_StandUp) //广播重新站立状态
        {
            /*
            float[] message = content as float[];
            //Debug.Log("重新站立++++++++++++++++message:" + message);
            int index = System.Convert.ToInt32(message[0]);
            Debug.LogWarning("11All_StandUp++++++++++++++index:" + index);

            //延时0.2秒是为了让他晚于同步信息
            StartCoroutine(DelayToInvoke(delegate ()
            {
                GameObject obj = GameObject.Find("role" + index + "(Clone)");
                if (obj != null)
                {
                    if (obj.name == selfRole.objectName)
                    {
                        Debug.LogWarning("All_StandUp++++++++++++++obj.name:" + obj.name);
                        obj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                        obj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                    }
                    

                    //恢复旋转方向
                    obj.transform.rotation = Quaternion.Euler(0, 0, 0);
                    //恢复位置
                    obj.transform.position = new Vector3(message[1], message[2], message[3]);
                }
            }, 0.2f));
            */
        }
        else if (eventcode == (byte)ProtocolCode.All_Freeze)
        {
            int[] ids = content as int[];
            if (ids[1] == selfID)
            {
                if (DateTime.Compare(selfFreezeTime, DateTime.Now) > 0)
                {
                    selfFreezeTime = selfFreezeTime.AddSeconds(roundTime);
                }
                else
                {
                    selfFreezeTime = DateTime.Now;
                    selfFreezeTime = selfFreezeTime.AddSeconds(roundTime);
                }
            }
        }
        else
        {
            Debug.LogWarning("unknown eventcode+++++++++++++++++++eventcode:" + eventcode);
        }
    }

    public void showLaserEffect(string attackObjectName, string deathObjectName)
    {
		GameObject attackObject = getObjectByName(attackObjectName);
		GameObject otherObject = getObjectByName(deathObjectName);
		if (attackObject == null || otherObject == null) {
			return;
		}
        Vector3 pos1 = attackObject.transform.position;
        pos1.y += 1f;
        Vector3 pos2 = otherObject.transform.position;
        pos2.y += 1f;

        GameObject laser = attackObject.transform.Find("Laser").gameObject;
        laser.SetActive(true);
        laser.GetComponent<LineRenderer>().SetPositions(new Vector3[] { pos1, pos2 });
        /*
        StartCoroutine(DelayToInvoke(delegate ()
        {
            if (laser != null)
            {
                laser.SetActive(false);
            }
        }, 1f));
        */
    }

    void setRolesInfo(Role[] roles)
    {
        roleList.Clear();
        int count = roles.Length;
        for (int i = 0; i < count; i++)
        {
            roleList.Add(roles[i]);
            if (roles[i].id == selfID)
            {
                selfRole = roles[i];
            }
        }
    }
    private void createTitleName()
    {
        Debug.Log("11createTitleName+++++++++++++++++++++++++");
        StartCoroutine(DelayToInvoke(delegate ()
        {
            Debug.Log("22createTitleName+++++++++++++++++++++++++");

            for (int i = 0; i < roleList.Count; i++)
            {
                //GameObject obj = GameObject.Find("role" + i + "(Clone)");
                GameObject obj = GameObject.Find(roleList[i].objectName);
                obj.transform.Find("nameCanvas").gameObject.SetActive(true);
                string name = (roleList[i].id == selfRole.id)? "自己" : roleList[i].name;
                Color color = (roleList[i].id == selfRole.id) ? new Color(255, 0, 0, 255) : new Color(255, 255, 0, 255);
                obj.transform.Find("nameCanvas").transform.Find("Text").GetComponent<Text>().text = name;
                obj.transform.Find("nameCanvas").transform.Find("Text").GetComponent<Text>().color = color;
            }
        }, 0.5f));
    }
    private void initKillList()
    {

    }
    void pointSpy(Role sayRole, Role spyRole)
    {
        GameObject obj = GameObject.Find(sayRole.objectName);
        GameObject word = obj.transform.Find("wordCanvas").gameObject;
        word.SetActive(true);
        word.transform.Find("Button").transform.Find("Text").GetComponent<Text>().text 
                = spyRole.name + "是卧底";

        StartCoroutine(DelayToInvoke(delegate ()
        {
            word.SetActive(false);
        }, 1.5f));
    }
    public IEnumerator DelayToInvoke(Action action, float delaySecondes)
    {
        yield return new WaitForSeconds(delaySecondes);
        action();
    }


    public void attack(string srcObjectName, string destObjectName)
    {
        Debug.Log("attack+++++++++srcObjectName:" + srcObjectName + ", destObjectName:" + destObjectName);
        //showLaserEffect(srcObjectName, destObjectName);

        Role srcRole = getRoleByObjectName(srcObjectName);
        Role destRole = getRoleByObjectName(destObjectName);

        int pCount = 0;
        for (int i = 0; i < roleList.Count; i++)
        {
            if (roleList[i].objectName == srcObjectName)
            {
                roleList[i].killID = destRole.id;
            }

            if (roleList[i].killID == destRole.id)
            {
                pCount++;
            }
        }


        int deathStatus = -1;
        if (pCount >= pointCount)
        {
            int index = getIndexByID(destRole.id);
            Debug.Log("attack++++++++++++++++index:" + index);
            roleList[index].isDeath = true;
            deathStatus = 1;
        }


        //广播状态
        int[] ids = new int[] { srcRole.id, destRole.id, deathStatus };
        PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_KillOtherResult, ids, true, null);
        checkGameEnd();
    }
        /*
    public override void OnConnectedToPhoton()
    {
        Debug.Log("OnConnectedToPhoton+++++++++++++++++++++++++++++");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom+++++++++++++++++++++++++++++");
    }
    */
    public void OnConnectedToPhoton()
    {
        Debug.Log("OnConnectedToPhoton+++++++++++++++++++++++++++++");
    }

    public void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom+++++++++++++++++++++++++++++");
        isHost = false;
    }

    public void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        Debug.Log("OnMasterClientSwitched+++++++++++++++++++++++++++++");
    }

    public void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        Debug.Log("OnPhotonCreateRoomFailed+++++++++++++++++++++++++++++");
        showTips("创建房间失败", 1f);
    }

    public void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        Debug.Log("OnPhotonJoinRoomFailed+++++++++++++++++++++++++++++");
        showTips("加入房间失败", 1f);
    }

    public void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom+++++++++++++++++++++++++++++");
    }

    public void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby+++++++++++++++++++++++++++++");
        gameState = GameState.JoinedLobby;
    }

    public void OnLeftLobby()
    {
        Debug.Log("OnLeftLobby+++++++++++++++++++++++++++++");
    }

    public void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        Debug.Log("OnFailedToConnectToPhoton+++++++++++++++++++++++++++++");
    }

    public void OnConnectionFail(DisconnectCause cause)
    {
        Debug.Log("OnConnectionFail+++++++++++++++++++++++++++++");
    }

    public void OnDisconnectedFromPhoton()
    {
        Debug.Log("OnDisconnectedFromPhoton+++++++++++++++++++++++++++++");
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Debug.Log("OnPhotonInstantiate+++++++++++++++++++++++++++++");
    }

    public void OnReceivedRoomListUpdate()
    {
        Debug.Log("OnReceivedRoomListUpdate+++++++++++++++++++++++++++++");
        
    }

    public void OnJoinedRoom()
    {
        Debug.Log("自己加入房间OnJoinedRoom+++++++++++++++++++++++++++++name:" + PhotonNetwork.room.Name
            + ", playerCount:" + PhotonNetwork.room.PlayerCount);
        createSelfRole();
        selfPlayer = PhotonNetwork.player;
    }

    public void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("有玩家进入了OnPhotonPlayerConnected+++++++++++++++++++++++++++++name:" + PhotonNetwork.room.Name
            + ", playerCount:" + PhotonNetwork.room.PlayerCount + ", newPlayerName:" + newPlayer.NickName);
        otherPlayerJoint(newPlayer);
    }

    public void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        Debug.Log("有玩家退出OnPhotonPlayerDisconnected++++++++++++++++++++++name:" + PhotonNetwork.room.Name
            + ", playerCount:" + PhotonNetwork.room.PlayerCount + ", name:" + otherPlayer.NickName);
        for(int i = 0; i < roleList.Count; i++)
        {
            Role r = roleList[i];
            if(r.id == otherPlayer.ID)
            {
                if (i == 0)//房主离开了
                {
                    if (roleList[1].id == selfID)//如果自己排在第一位，则自己成为房主
                    {
                        isHost = true;
                    }
                }



                roleList.Remove(r);
                return;
            }
        }

        Debug.LogWarning("++++++++++++++++not found the leave player");
    }

    public void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        Debug.Log("OnPhotonRandomJoinFailed+++++++++++++++++++++++++++++");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster+++++++++++++++++++++++++++++");
    }

    public void OnPhotonMaxCccuReached()
    {
        Debug.Log("OnPhotonMaxCccuReached+++++++++++++++++++++++++++++");
    }

    public void OnPhotonCustomRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log("房间属性变化OnPhotonCustomRoomPropertiesChanged+++++++++++++++++++++++++++++");
    }

    public void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
    {
        Debug.Log("玩家属性变化OnPhotonPlayerPropertiesChanged+++++++++++++++++++++++++++++playerAndUpdatedProps:" + playerAndUpdatedProps);
    }

    public void OnUpdatedFriendList()
    {
        Debug.Log("OnUpdatedFriendList+++++++++++++++++++++++++++++");
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log("OnCustomAuthenticationFailed+++++++++++++++++++++++++++++");
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("OnCustomAuthenticationResponse+++++++++++++++++++++++++++++");
    }

    public void OnWebRpcResponse(OperationResponse response)
    {
        Debug.Log("OnWebRpcResponse+++++++++++++++++++++++++++++");
    }

    public void OnOwnershipRequest(object[] viewAndPlayer)
    {
        Debug.Log("OnOwnershipRequest+++++++++++++++++++++++++++++");
    }

    public void OnLobbyStatisticsUpdate()
    {
        Debug.Log("OnLobbyStatisticsUpdate+++++++++++++++++++++++++++++");
    }

    public void OnPhotonPlayerActivityChanged(PhotonPlayer otherPlayer)
    {
        Debug.Log("OnPhotonPlayerActivityChanged+++++++++++++++++++++++++++++");
    }

    public void OnOwnershipTransfered(object[] viewAndPlayers)
    {
        Debug.Log("OnOwnershipTransfered+++++++++++++++++++++++++++++");
    }



    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        Debug.Log("OnPhotonSerializeView+++++++++++++++++++++++++++++");
    }
}
