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
    public int beKillID = -1;//被杀的人的ID
    public string name = "";
    public string objectName = "";
    
    public void print()
    {
        Debug.Log("Role+++++++id:" + id + ", isSpy:" + isSpy + ", isDeath:" + isDeath
            + ",beKillID:" + beKillID + ", name:" + name + ", objectName:" + objectName);
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

    public const int GAME_SCORE = 20;
    public static int gameScore = 0;
    public const int TOTAL_GAME_TIME = 60;
    public static int timeLeft = 0; //剩余时间

    public static GameObject s_endScene;

    private int selfID = -1;
    public static bool isHost = false;
    public static Role selfRole = new Role();
    private PhotonPlayer selfPlayer;
    public static List<Role> roleList = new List<Role>();
    public static string gameResultReason = "";


    public enum GameState {GameInit, JoinedLobby, JoinedRoom, GameStart, LeaveGame, SpyWin, SpyLose }
    public enum ProtocolCode {Host_PlayerJoined, Host_RejectJoin, Host_PlayerLeave, Host_GameStart,
        All_word, Guest_KillOtherRequest, Host_KillOtherResult, Host_SynRolesInfo, Host_GameEnd, All_StandUp}
    public static GameState gameState = GameState.GameInit;

    //private int rolesAllocation = 0;
    public static Vector2 NativeResolution = new Vector2(640, 360);
    private static float _guiScaleFactor = -1.0f;
    private static Vector3 _offset = Vector3.zero;
    static List<Matrix4x4> stack = new List<Matrix4x4>();
    static bool _didResizeUI = false;
    public void BeginUIResizing()
    {
        Vector2 nativeSize = NativeResolution;
        _didResizeUI = true;

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
        _didResizeUI = false;
    }

    // Use this for initialization
    void Start () {
        PhotonNetwork.ConnectUsingSettings("1.0");
        //UnityEngine.Random rd = new UnityEngine.Random();

        PhotonNetwork.OnEventCall += this.OnEvent;

        PhotonPeer.RegisterType(typeof(Role), (byte)'R', SerializeRole, DeSerializeRole);
        PhotonNetwork.autoCleanUpPlayerObjects = true;

        loadName();

        s_endScene = endScene;
    }

    void Update()
    {
        if (gameState == GameState.GameStart && isHost)
        {
            diskObject.transform.Rotate(Vector3.up * Time.deltaTime * 50, Space.World);
        }
    }
    private void OnGUI()
    {
        BeginUIResizing();
        //Debug.Log("OnGUI+++++++++width:" + Screen.width + ", height:" + Screen.height
        //    + ", offset x:" + _offset.x + ", y:" + _offset.y);

        //_offset x负数向左靠，正数向右靠；y负数向上靠，正数向下靠
        int diffY = 0;
        string strHost = isHost ? "房主" : "房客";
        string strIdentify = "身份";
        if (selfRole != null)
        {
            strIdentify = selfRole.isSpy ? "卧底" : "平民";
        }

        if (gameState == GameState.JoinedRoom)
        {
            GUI.Label(new Rect(0 - _offset.x + 20, diffY - _offset.y, 50, 20), PhotonNetwork.room.Name);
        }
        GUI.Label(new Rect(0 - _offset.x + 70, diffY - _offset.y, 50, 20), strHost);
        if (gameState == GameState.GameStart || gameState == GameState.SpyWin || gameState == GameState.SpyLose)
        {
            GUI.Label(new Rect(0 - _offset.x + 140, diffY - _offset.y, 50, 20), strIdentify);
        }
        GUI.Label(new Rect(0 - _offset.x + 210, diffY - _offset.y, 50, 20), PhotonNetwork.connectionStateDetailed.ToString());
        GUI.Label(new Rect(0 - _offset.x + 280, diffY - _offset.y, 50, 20), "score:" + gameScore);
        GUI.Label(new Rect(0 - _offset.x + 350, diffY - _offset.y, 100, 20), "剩余时间：" + timeLeft);


        if (gameState == GameState.SpyWin || gameState == GameState.SpyLose)
        {
            return;
        }

        if (!PhotonNetwork.inRoom && PhotonNetwork.connectionStateDetailed.ToString() == "JoinedLobby")
        {
            Rect rect = new Rect(NativeResolution.x / 2 - 50 - _offset.x, 30 + diffY - _offset.y, 70, 30);
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
            Rect rect = new Rect(NativeResolution.x / 2 - 50 - _offset.x, 30 + diffY - _offset.y, 70, 30);
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
            Rect rect = new Rect(NativeResolution.x / 2 + 50 - _offset.x, 30 + diffY - _offset.y, 70, 30);
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

        EndUIResizing();
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
        short death = (r.isSpy ? (short)1 : (short)0);
        Protocol.Serialize(death, bytes, ref index);
        Protocol.Serialize(r.beKillID, bytes, ref index);


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

        Protocol.Deserialize(out role.beKillID, bytes, ref index);

        int nameLength = 0;
        Protocol.Deserialize(out nameLength, bytes, ref index);
        role.name = UTF8Encoding.Default.GetString(bytes, index, nameLength);
        index += nameLength;
        int objectNameLength = 0;
        Protocol.Deserialize(out objectNameLength, bytes, ref index);
        role.objectName = UTF8Encoding.Default.GetString(bytes, index, objectNameLength);
        index = objectNameLength;
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
            GameObject.Find("setNameCanvas").transform.Find("InputField").GetComponent<InputField>().text = name;
        }
    }
    void setName()
    {
        string name = GameObject.Find("setNameCanvas").transform.Find("InputField").transform.Find("Text").GetComponent<Text>().text;
        if (name == "")
        {
            name = "wqy" + UnityEngine.Random.Range(0, 1000);
            GameObject.Find("setNameCanvas").transform.Find("InputField").GetComponent<InputField>().text = name;
        }

        Debug.Log("setName+++++++++++++++++ name:" + name);
        PlayerPrefs.SetString("MyName", name);
        PhotonNetwork.playerName = name;
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

    private static void cleanObjects()
    {
        /*
        for (int i = 0; i < 10; i++)
        {
            GameObject obj = GameObject.Find("role" + i + "(Clone)");
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        */
    }

    private void gameInit()
    {
        cleanObjects();
        //gameState = GameState.GameStart;

        allocateRoles();
        selfRole = roleList[0];
        createObject(0, roleList.Count);

        Role[] content = roleList.ToArray();
        PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_GameStart, content, true, null);

    //    gameScore = GAME_SCORE;
     //   timeLeft = COUNT_DOWN_TIME;
    //    Invoke("TimeSchedule", 1.0f);
        
    }

    public IEnumerator gameStart()
    {
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
        gameScore = GAME_SCORE;
        timeLeft = TOTAL_GAME_TIME;
        createTitleName();
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
            roleList[i].beKillID = -1;

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
        yield return new WaitForSeconds(1.5f);

        cleanObjects();
        PhotonNetwork.DestroyPlayerObjects(selfPlayer);

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
            Role r = roleList[i];
            r.print();
            string strIdentify = r.isSpy ? "卧底" : "平民";
            string strBeKill = "";
            if (r.beKillID != -1)
            {
                Role killRole = getRoleByID(r.beKillID);
                if (killRole.isSpy == false && r.isSpy == false)
                {
                    strBeKill += "被队友" + killRole.name + "错杀了";
                }
                else
                {
                    strBeKill += "被" + killRole.name + "杀了";
                }
            }
            text += r.name + "    " + strIdentify + "   " + strBeKill + "\n";
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
                        createObject(i, count);
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
            int killID = ids[0];
            Role killRole = getRoleByID(killID);
            int deathID = ids[1];
            for (int i = 0; i < roleList.Count; i++)
            {
                if (roleList[i].id == deathID)
                {
                    roleList[i].isDeath = true;
                    roleList[i].beKillID = killID;
                    Debug.Log("showLaserEffect++++++++++++++++killRole.objectName:" + killRole.objectName
                        + ", roleList[i].objectName:" + roleList[i].objectName);
                    showLaserEffect(killRole.objectName, roleList[i].objectName);
                }

                if (selfID == deathID)
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

        StartCoroutine(DelayToInvoke(delegate ()
        {
            if (laser != null)
            {
                laser.SetActive(false);
            }
        }, 1f));
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
                GameObject obj = GameObject.Find("role" + i + "(Clone)");
                obj.transform.Find("nameCanvas").gameObject.SetActive(true);
                string name = (roleList[i].id == selfRole.id)? "自己" : roleList[i].name;
                Color color = (roleList[i].id == selfRole.id) ? new Color(255, 0, 0, 255) : new Color(255, 255, 0, 255);
                obj.transform.Find("nameCanvas").transform.Find("Text").GetComponent<Text>().text = name;
                obj.transform.Find("nameCanvas").transform.Find("Text").GetComponent<Text>().color = color;
            }
        }, 0.5f));
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


    public void attack(string attackObjectName, string deathObjectName)
    {
        Debug.Log("+++++++++attackObjectName:" + attackObjectName + ", deathObjectName:" + deathObjectName);
        showLaserEffect(attackObjectName, deathObjectName);

        Role attack = getRoleByObjectName(attackObjectName);

        for (int i = 0; i < roleList.Count; i++)
        {
            if (roleList[i].objectName == deathObjectName)
            {
                roleList[i].isDeath = true;
                roleList[i].beKillID = attack.id;
                //广播状态
                int[] ids = new int[] {attack.id, roleList[i].id};
                PhotonNetwork.RaiseEvent((byte)ProtocolCode.Host_KillOtherResult, ids, true, null);

                checkGameEnd();

                return;
            }
        }
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
