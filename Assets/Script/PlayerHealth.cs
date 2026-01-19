using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    bool isDead = false;

    Camera cameraScript;
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        cameraScript = GetComponent<Camera>();
        rb = GetComponent<Rigidbody>();
        Debug.Log("コンポーネント取得");
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("プレイヤー死亡");

        //移動・視点停止
        if (cameraScript != null)
            cameraScript.enabled = false;

        Cursor.lockState = CursorLockMode.None;

    }

}