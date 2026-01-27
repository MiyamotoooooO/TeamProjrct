using UnityEngine;

public class CageTrap : MonoBehaviour
{
    [Header("--- 設定 ---")]
    [Tooltip("檻（自分自身）が落ちて止まる目標の高さ（Y座標）")]
    public float targetYPosition = 0.5f; // 床より少し下か、ピッタリの位置を指定

    [Tooltip("落ちるスピード（大きいほど速い）")]
    public float fallSpeed = 15.0f;

    [Header("--- 参照 ---")]
    [Tooltip("トリガーとなる透明な箱を指定してください")]
    public GameObject trapTriggerObj;

    [Tooltip("落ちた時の音（あれば）")]
    public AudioSource trapSound;

    // 内部変数
    private bool isActivated = false;
    private Transform myTransform;

    void Start()
    {
        myTransform = this.transform;

        // 念のため、物理演算で勝手に落ちないようにしておく
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 物理演算を無効化（これで壁も貫通します）
            rb.useGravity = false; // 重力もオフ
        }
    }

    void Update()
    {
        // 罠が発動したら、目標位置まで移動し続ける
        if (isActivated)
        {
            // 現在の位置
            Vector3 currentPos = myTransform.position;

            // 目標の位置（XとZは今のまま、Yだけ指定した高さ）
            Vector3 targetPos = new Vector3(currentPos.x, targetYPosition, currentPos.z);

            // MoveTowardsを使って、指定スピードで目標へ向かう
            // （isKinematicがtrueなので、床や壁があっても無視して貫通します）
            myTransform.position = Vector3.MoveTowards(currentPos, targetPos, fallSpeed * Time.deltaTime);

            // 到着したら（位置がほぼ同じになったら）
            if (Vector3.Distance(myTransform.position, targetPos) < 0.001f)
            {
                // ここに「着地時の処理」を書けます（音を止めるなど）
            }
        }
    }

    // トリガー側から呼ばれる関数
    // （TrapTriggerスクリプトを作る手間を省くため、このスクリプトだけで完結させます）
    void OnTriggerEnter(Collider other)
    {
        CheckActivation(other);
    }

    // 外部から呼ぶ用
    public void ActivateTrap()
    {
        if (!isActivated)
        {
            isActivated = true;
            Debug.Log("檻、落下開始！");
            if (trapSound != null) trapSound.Play();
        }
    }

    // 衝突判定
    private void CheckActivation(Collider other)
    {
        if (isActivated) return;
        if (other.CompareTag("Player"))
        {
            ActivateTrap();
        }
    }
}