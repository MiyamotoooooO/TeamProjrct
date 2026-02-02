using UnityEngine;

public class CageTrap : MonoBehaviour
{
    [Header("檻が落ちて止まる目標の高さ")]
    public float targetYPosition = 0.5f;

    [Header("落ちるスピード")]
    public float fallSpeed = 15.0f;

    [Header("トリガーとなる透明な箱")]
    public GameObject trapTriggerObj;

    [Header("落ちた時の音")]
    public AudioSource trapSound;

    // private
    private bool isActivated = false; // 罠がもう作動したかどうかのフラグ
    private Transform myTransform; // 自分自身の場所や向きの情報をしまっておく箱

    void Start()
    {
        myTransform = this.transform;

        // 念のため、物理演算で勝手に落ちないようにしておく
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 物理演算を無効化
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

            // 目標の位置
            Vector3 targetPos = new Vector3(currentPos.x, targetYPosition, currentPos.z);

            // MoveTowardsを使って、指定スピードで目標へ向かう
            myTransform.position = Vector3.MoveTowards(currentPos, targetPos, fallSpeed * Time.deltaTime);
        }
    }

    // トリガー側から呼ばれる関数
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