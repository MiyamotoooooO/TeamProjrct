using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    bool isDead = false;

    [Header("設定")]
    [Tooltip("プレイヤーの移動制御スクリプト")]
    public MonoBehaviour playerMovementScript;
    [Tooltip("プレイヤーのカメラ")]
    public Camera playerCamera;

    [Header("アイテム連携")]
    public LighterSystem lighterSystem;
    public FlashlightSystem flashlightSystem;

    [Header("死亡演出の設定")]
    [Tooltip("倒れる前にふらふらする時間（秒）")]
    public float wobbleDuration = 2.0f;
    [Tooltip("ふらつきの激しさ")]
    public float wobbleIntensity = 10.0f;
    [Tooltip("ふらつきの回転スピード")]
    public float wobbleSpeed = 10.0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("プレイヤー死亡");

        // 移動操作をオフ
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (lighterSystem != null)
        {
            lighterSystem.canUseLighter = false;
        }

        if (flashlightSystem != null)
        {
            flashlightSystem.canUseFlashlight = false;
        }

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // --- ① 指定した秒数だけ、360度ふらふらする ---
        float timer = 0;
        Quaternion startRot = playerCamera.transform.localRotation;

        while (timer < wobbleDuration)
        {
            timer += Time.deltaTime;

            // 時間経過（0.0 〜 1.0）
            float progress = timer / wobbleDuration;

            // 徐々に揺れを大きくする
            float currentIntensity = Mathf.Lerp(wobbleIntensity * 0.5f, wobbleIntensity * 1.5f, progress);

            float angle = timer * wobbleSpeed;
            float x = Mathf.Sin(angle) * currentIntensity; // 前後の揺れ
            float z = Mathf.Cos(angle) * currentIntensity; // 左右の揺れ

            // Y軸（横回転）にも少しノイズを入れて、焦点が定まらない感じに
            float y = (Mathf.PerlinNoise(timer, 0) - 0.5f) * 10f;

            // 回転を適用
            playerCamera.transform.localRotation = startRot * Quaternion.Euler(x, y, z);

            yield return null;
        }

        // --- ② 物理演算でバタリと倒れる ---
        rb.constraints = RigidbodyConstraints.None; // ロック解除

        // 倒れるきっかけの力を加える
        rb.AddTorque(transform.right * -50f, ForceMode.Impulse); // 後ろには倒れず、前にガクッといく
        rb.AddTorque(transform.forward * Random.Range(-20f, 20f), ForceMode.Impulse);

        // --- ③ 地面に着くまで待機（3秒） ---
        yield return new WaitForSeconds(3.0f);

        // --- ④ 動きを完全に止める（死体固定） ---
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // カーソル表示
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}