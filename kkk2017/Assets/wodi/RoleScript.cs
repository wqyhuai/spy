/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleScript : MonoBehaviour {

    public Camera mainCamera;
    

    // Use this for initialization
    void Start () {
        mainCamera = (Camera)GameObject.Find("MainCamera").GetComponent<Camera>();
    }
	

    void Update()
    {
        
        if (Input.GetMouseButtonUp(0))
        {
            checkKick();
        }
        
        
    }

    private void checkKick()
    {
        RaycastHit hit;
        Vector2 screenPosition = Input.mousePosition;
        var ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out hit) && hit.transform == transform)
        {
            Debug.Log("checkKick+++++++++++++++++++++++++++++");
            if (PUNConnect.selfRole == null || PUNConnect.selfRole.isDeath)
            {
                return;
            }

            if (PUNConnect.selfRole.isSpy)
            {
                Debug.Log("+++++++++++++++++你是卧底不能杀人");
                //return;
            }

            //射线相交后会将碰撞点保存在hit中
            Debug.Log("++++++++++++++++++++hit point:" + hit.point + ", name:" + gameObject.name
                + ", PUNConnect.selfRole.objectName:" + PUNConnect.selfRole.objectName);
            if (PUNConnect.selfRole.objectName == gameObject.name)
            {
                return;
            }


            StartCoroutine(attackEffect());
        }

    }

    public IEnumerator attackEffect()
    {
        Debug.Log("attackEffect+++++++++++++++");
        GameObject selfObject = GameObject.Find(PUNConnect.selfRole.objectName);
        GameObject otherObject = gameObject;
       // selfObject = GameObject.Find("role0");
       // otherObject = GameObject.Find("role1");

        if (selfObject == null || otherObject == null)
        {
            yield return new WaitForSeconds(0);
        }

        Vector3 pos1 = selfObject.transform.position;
        pos1.y += 0.5f;
        Vector3 pos2 = otherObject.transform.position;
        pos2.y += 0.5f;

        Debug.DrawLine(pos1, pos2, Color.yellow);//只能在Scene窗口显示

        //GameObject hit = GameObject.Find("hit");
        GameObject hit = PhotonNetwork.Instantiate("hit", new Vector3(-3f, 2, -2), Quaternion.identity, 0);

        Vector3 direct = pos2 - pos1;
        Vector3 center = pos1 + direct / 2;
        //设置位置
        //hit.transform.position = center;

        //设置方向
        Quaternion currentRotation = hit.transform.rotation;
        Vector3 direction = direct.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        hit.transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, 360);
        //因为lookRotation是z轴朝向的，所以需要对X轴旋转90度
        hit.transform.Rotate(new Vector3(1, 0, 0), 90);

        //动态设置长度和位置以达到伸长效果
        Vector3 scale = hit.transform.localScale;
        float totalScaleY = direct.magnitude / 2;

        int step = 3;
        for (int i = step; i > 0; i--)
        {
            Debug.Log("attackEffect+++++++++++++++i:" + i);
            scale.y = totalScaleY / i;
            hit.transform.localScale = scale;
            Vector3 position = center - (direction * totalScaleY/2)*i/ step;
            hit.transform.position = position;
            if (i == 1)
            {
                attack();
            }
            else
            {
                yield return new WaitForSeconds(0.03f);
            }
        }

        //缩回去
        for (int i = 1; i <= step; i++)
        {
            Debug.Log("attackEffect+++++++++++++++i:" + i);
            scale.y = totalScaleY / i;
            hit.transform.localScale = scale;
            Vector3 position = center - (direction * totalScaleY / 2) * i / step;
            hit.transform.position = position;
            yield return new WaitForSeconds(0.03f);
        }
    }

    public void attack()
    {
        gameObject.SetActive(false);
        Debug.Log("++++++++++++++++after coroutine");
        if (PUNConnect.isHost)
        {
            PUNConnect.attack(PUNConnect.selfRole.objectName, gameObject.name);
        }
        else
        {
            string[] content = new string[] { PUNConnect.selfRole.objectName, gameObject.name };
            Debug.Log("checkKick+++++++++++++++content:" + content);
            bool reliable = true;
            //Role需要序列化才发出去
            PhotonNetwork.RaiseEvent((byte)PUNConnect.ProtocolCode.Guest_KillOtherRequest, 
                content, reliable, null);
        }
    }
}

*/
