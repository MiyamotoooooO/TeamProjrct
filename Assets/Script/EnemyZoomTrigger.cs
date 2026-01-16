using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyZoomTrigger : MonoBehaviour
{
    [Header("ターゲット設定")]
    [Tooltip("映したい敵たち（2体以上でもOK）")]
    public Transform[] enemies;

    [Header("距離と位置の調整")]
    [Tooltip("敵の中心点から「何メートル」離れた位置で止まるか")]
    public float stopDistance = 2.0f;

    [Tooltip("最終的なカメラ位置の微調整（XYZ座標）")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("最終的なカメラ角度の微調整（回転）")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("演出設定")]
    [Tooltip("ズーム（移動）にかかる時間")]
    public float moveDuration = 0.5f;

    [Tooltip("ズーム状態で静止する時間（秒）")]
    public float stopDuration = 2.0f;

    [Header("一度きりのイベントにするか")]
    public bool playOnlyOnce = true;

    // private
    private bool hasPlayed = false;
    private PlayerController playerScript;
    private Camera playerCamera;
    private Vector3 defaultPos;
    private Quaternion defaultRot;

    void OnTriggerEnter(Collider other)
    {
        if (playOnlyOnce && hasPlayed) return;

        if (other.CompareTag("Player"))
        {
            playerScript = other.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                hasPlayed = true;
                StartCoroutine(PlayZoomEffect());
            }
        }
    }

    IEnumerator PlayZoomEffect()
    {
        // プレイヤーの操作を禁止
        playerScript.canControl = false;
        playerCamera = playerScript.cam.GetComponent<Camera>();

        // 元の位置・回転を記憶（ローカル座標）
        defaultPos = playerCamera.transform.localPosition;
        defaultRot = playerCamera.transform.localRotation;

        // 目標地点の計算

        // 敵全員の中心点を計算
        Vector3 centerPoint = Vector3.zero;
        foreach (Transform enemy in enemies)
        {
            if (enemy != null) centerPoint += enemy.position;
        }
        if (enemies.Length > 0) centerPoint /= enemies.Length;

        // カメラが移動すべき目標位置を計算
        Vector3 directionFromCenter = (playerCamera.transform.position - centerPoint).normalized;

        // 基本の目標位置 = 中心点 + (方向 * 指定した距離)
        Vector3 targetWorldPos = centerPoint + (directionFromCenter * stopDistance);

        // Inspectorで設定した微調整（XYZ）を加える
        targetWorldPos += positionOffset;

        // 目標の回転（まずは中心を見る）
        Quaternion baseLookRot = Quaternion.LookRotation(centerPoint - targetWorldPos);
        // 微調整（角度）を加える
        Quaternion targetWorldRot = baseLookRot * Quaternion.Euler(rotationOffset);


        // アニメーション開始

        float t = 0;
        Vector3 startWorldPos = playerCamera.transform.position;
        Quaternion startWorldRot = playerCamera.transform.rotation;

        while (t < 1.0f)
        {
            t += Time.deltaTime / moveDuration;

            // スムーズなカーブ
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // カメラを動かす
            playerCamera.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, smoothT);
            playerCamera.transform.rotation = Quaternion.Slerp(startWorldRot, targetWorldRot, smoothT);

            yield return null;
        }

        // 静止（フリーズ）
        yield return new WaitForSeconds(stopDuration);

        // 元に戻す
        t = 0;
        // 戻る時は少しゆっくりにする
        while (t < 1.0f)
        {
            t += Time.deltaTime / moveDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, defaultPos, smoothT);
            playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, defaultRot, smoothT);

            yield return null;
        }

        // 確実に元の位置に戻す
        playerCamera.transform.localPosition = defaultPos;
        playerCamera.transform.localRotation = defaultRot;

        // PlayerControllerに今の視点を同期させる（ガクッとならないように）
        playerScript.SyncRotationToCurrent();

        // 操作を許可
        playerScript.canControl = true;

        if (playOnlyOnce)
        {
            Destroy(gameObject);
        }
    }

    // Scene画面で「どこにカメラが止まるか」を可視化する機能
    void OnDrawGizmosSelected()
    {
        if (enemies == null || enemies.Length == 0) return;

        // 中心点の計算
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Transform e in enemies)
        {
            if (e != null)
            {
                center += e.position;
                count++;
            }
        }
        if (count > 0) center /= count;

        // 中心点を赤丸で表示
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.3f);

        // カメラ位置の目安（プレイ中でないと正確なPlayer位置が不明なので、中心からZ手前に表示します）
        Gizmos.color = Color.yellow;
        Vector3 approxCamPos = center - (Vector3.forward * stopDistance) + positionOffset;
        Gizmos.DrawWireSphere(approxCamPos, 0.5f);
        Gizmos.DrawLine(center, approxCamPos);
    }
}