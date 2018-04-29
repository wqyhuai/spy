using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testCollider : MonoBehaviour {
    public GameObject diskObject;
    private GameObject lastCollider;
    private TimeSpan lastColliderTime;

    /*
    //只有勾选trigger，trigger相关的函数才会回调
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("+++++++++++++++++++++++OnTriggerEnter");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("+++++++++++++++++++++++OnTriggerExit");
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log("+++++++++++++++++++++++OnTriggerStay");
    }
    */
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "disk")
        {
            return;
        }
        GameObject role = collision.gameObject;
        Debug.Log("+++++++++++++++++++++++OnCollisionEnter, 开始碰到了, name:" + role.name);
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = ts - lastColliderTime;
        lastColliderTime = ts;
       // Debug.Log("++++++++++++++++++time diff:" + diff.TotalMilliseconds);
        //500毫秒内连续碰撞同一个物体认为无效
        if (lastCollider != null && role.name == lastCollider.name && diff.TotalMilliseconds < 500)
        {
            return;
        }

        PUNConnect.gameScore--;
        PUNConnect.checkGameEnd();

        lastCollider = role;
    }

    public IEnumerator DelayToInvoke(Action action, float delaySecondes)
    {
        yield return new WaitForSeconds(delaySecondes);
        action();
    }
    private void OnCollisionExit(Collision collision)
    {
        /*
        if (!PUNConnect.isHost)
        {
            return;
        }
        */
        //Debug.Log("11OnCollisionExit+++++++++++++++++++++++++++");

        GameObject obj = collision.gameObject;
        if (obj == null)
        {
           // Debug.Log("++++++++++++++++++++++++++++obj == null");

            return;
        }

        if (obj.name != PUNConnect.selfRole.objectName)
        {
            //Debug.Log("collider++++++++++++PUNConnect.selfRole.objectName:" + PUNConnect.selfRole.objectName);

            return;
        }

        Rigidbody rigidBody = obj.GetComponent<Rigidbody>();
        if (rigidBody == null)
        {
            //Debug.Log("++++++++++++++++++++++++++++rigidBody == null");
            return;
        }
       // Debug.Log("222OnCollisionExit+++++++++++++++++++++++++++");
        StartCoroutine(DelayToInvoke(delegate ()
        {
            //Debug.Log("333OnCollisionExit+++++++++++++++++++++++++++");
            if (obj == null)
            {
                //Debug.Log("22++++++++++++++++++++++++++++obj == null");
                return;
            }

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

            string str = obj.name.Substring(4, 1);
            int index = Convert.ToInt32(str);
            float[] content = new float[] {index, point.x, point.y, point.z};
            PhotonNetwork.RaiseEvent((byte)PUNConnect.ProtocolCode.All_StandUp, content, true, null);
        }, 1.5f));

        

        
    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("+++++++++++++++++++++++OnCollisionStay");
    }
}
