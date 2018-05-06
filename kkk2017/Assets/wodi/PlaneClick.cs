using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlaneClick : MonoBehaviour {

    public Camera mainCamera;
    PUNConnect punScript;

    // Use this for initialization
    void Start () {
        punScript = GameObject.Find("MainCamera").GetComponent<PUNConnect>();
	}
	
	// Update is called once per frame
	void Update () {
        if (PUNConnect.gameState != PUNConnect.GameState.GameStart)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("++++++++++++++++++Space");
            jump();
        }

        if (Input.GetMouseButtonUp(0))
        {
            checkKill();
        }
    }

    void OnClick()
    {
        Debug.Log("++++++++++++++++++click");
        jump();
    }

    void jump()
    {
       // Debug.Log("++++++++++++++++++jump");
        /*
        if (PUNConnect.selfRole == null || PUNConnect.selfRole.isDeath )
        {
            Debug.Log("22++++++++++++++++++jump");
            return;
        }
        */
        if(PUNConnect.gameState != PUNConnect.GameState.GameStart)
        {
            Debug.LogWarning("jump++++++++++++++++++PUNConnect.gameState:" + PUNConnect.gameState);
            return;
        }

        GameObject ob = GameObject.Find(PUNConnect.selfRole.objectName);
        if (ob == null)
        {
            Debug.LogWarning("jump++++++++++++++++ob == null, PUNConnect.selfRole.objectName:" + PUNConnect.selfRole.objectName);
            return;
        }
        if (!ob.GetActive())
        {
            Debug.LogWarning("jump++++++++++++++++ob is not active");
            return;
        }

        if (!punScript.canJump())
        {
            return;
        }

        //已经在起跳状态，不再跳
        if (ob.transform.position.y >  1.1f)
        {
            return;
        }

        //跌倒状态，需要站立起来
        Quaternion rotation = ob.transform.rotation;
        if (rotation.x < -0.2 || rotation.x > 0.2
            || rotation.y < -0.2 || rotation.y > 0.2
            || rotation.z < -0.2 || rotation.z > 0.2)
        {
            punScript.resetObject(ob);
            return;
        }

        //Debug.Log("55++++++++++++++++++jump");
        Rigidbody rb = ob.GetComponent<Rigidbody>();
        rb.AddForce(0, 8, 0, ForceMode.Impulse);
    }

    GameObject getHitObject(Transform ts)
    {
        for (int i = 0; i < PUNConnect.roleList.Count; i++)
        {
            Role r = PUNConnect.roleList[i];
            

            GameObject obj = GameObject.Find(r.objectName);
            //扩大点击区域，名字也可点
            Transform cubeTransform = obj.transform.Find("Cube");
            if (obj != null && (ts == obj.transform || ts == cubeTransform))
            {
                if (r.id == PUNConnect.selfRole.id)
                {
                    punScript.showTips("不能自杀", 1f);
                    return null;
                }
                else
                {
                    return obj;
                }
            }
        }

        return null;
    }
    private void checkKill()
    {
        RaycastHit hit;
        Vector2 screenPosition = Input.mousePosition;
        var ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out hit))
        {
            GameObject otherObject = getHitObject(hit.transform);
            if (otherObject == null)
            {
                return;
            }
            
            if (PUNConnect.selfRole == null || PUNConnect.selfRole.isDeath)
            {
				if (PUNConnect.selfRole == null) {
					Debug.Log ("++++++++++++++++++++++++selfRole == null");
				}
				Debug.Log ("++++++++++++++++++++++++PUNConnect.selfRole.isDeath:" + PUNConnect.selfRole.isDeath);

                punScript.showTips("你已经挂了，不能再杀人", 1f);
                return;
            }
            
            if (PUNConnect.selfRole.isSpy)
            {
                Debug.Log("+++++++++++++++++你是卧底不能杀人");
                punScript.showTips("你是卧底不能杀人", 1f);
                return;
            }

            GameObject selfObject = GameObject.Find(PUNConnect.selfRole.objectName);
			Quaternion rotation = selfObject.transform.rotation;
			if(rotation.x < -0.1 || rotation.x > 0.1
				|| rotation.y < -0.1 || rotation.y > 0.1
				|| rotation.z < -0.1 || rotation.z > 0.1)
            {
                punScript.showTips("只有站立状态才能杀人", 1f);
				Debug.Log ("++++++++++++++rotation:" + selfObject.transform.rotation);
                return;
            }

            //射线相交后会将碰撞点保存在hit中
            Debug.Log("++++++hit point:" + hit.point + ", self obj name:" + otherObject.name
                + ", PUNConnect.selfRole.objectName:" + PUNConnect.selfRole.objectName);

            //特效不再使用圆柱体，而是使用激光
            if (PUNConnect.isHost) //房主则直接做杀人的逻辑判断
            {
                punScript.attack(selfObject.name, otherObject.name);
            }
            else //房客则先把消息发给房主
            {
                string[] content = new string[] { PUNConnect.selfRole.objectName, otherObject.name };
                //Role需要序列化才发出去
                PhotonNetwork.RaiseEvent((byte)PUNConnect.ProtocolCode.Guest_KillOtherRequest,
                    content, true, null);
            }
            /*
            StartCoroutine(DelayToInvoke(delegate ()
            {
                otherObject.SetActive(false);
            }, 0.2f));
            */
            //使用圆柱体特效
            //StartCoroutine(attackEffect(otherObject));
        }

    }


    /*
    public IEnumerator attackEffect(GameObject otherObject)
    {
        GameObject selfObject = GameObject.Find(PUNConnect.selfRole.objectName);

        if (selfObject == null || otherObject == null)
        {
            yield return new WaitForSeconds(0);
        }

        Vector3 pos1 = selfObject.transform.position;
        pos1.y += 0.5f;
        Vector3 pos2 = otherObject.transform.position;
        pos2.y += 0.5f;

        Debug.DrawLine(pos1, pos2, Color.yellow);//只能在Scene窗口显示

        GameObject hit = PhotonNetwork.Instantiate("hit", new Vector3(-3f, 2, -2), Quaternion.identity, 0);
        Vector3 direct = pos2 - pos1;
        Vector3 center = pos1 + direct / 2;

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

        int step = 5;
        for (int i = step; i > 0; i--)
        {
            scale.y = totalScaleY / i;
            hit.transform.localScale = scale;
            Vector3 position = center - (direction * totalScaleY / 2) * i / step;
            hit.transform.position = position;
            yield return new WaitForSeconds(0.03f);
        }

        attack(otherObject);

        //step += 5;
        //缩回去
        for (int i = 1; i <= step; i++)
        {
            scale.y = totalScaleY / i;
            hit.transform.localScale = scale;
            Vector3 position = center - (direction * totalScaleY / 2) * i / step;
            hit.transform.position = position;
            yield return new WaitForSeconds(0.05f);
        }

        Destroy(hit);
    }

    public void attack(GameObject otherObject)
    {
        if (otherObject == null)
        {
            Debug.LogWarning("+++++++++++++++++otherObject == null");
            return;
        }
        otherObject.SetActive(false);
        //Debug.Log("++++++++++++++++after coroutine");
        if (PUNConnect.isHost)
        {
            punScript.attack(PUNConnect.selfRole.objectName, otherObject.name);
        }
        else
        {
            string[] content = new string[] { PUNConnect.selfRole.objectName, otherObject.name };
            //Debug.Log("checkKick+++++++++++++++content:" + content);
            bool reliable = true;
            //Role需要序列化才发出去
            PhotonNetwork.RaiseEvent((byte)PUNConnect.ProtocolCode.Guest_KillOtherRequest,
                content, reliable, null);
        }
    }
    */
    private IEnumerator DelayToInvoke(Action action, float delaySecondes)
    {
        yield return new WaitForSeconds(delaySecondes);
        action();
    }
    
}
