using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombiePassByEvent : MonoBehaviour
{
    [Header("ターゲット設定")]
    [Tooltip("カメラが向く対象（空のオブジェクトなど）")]
    public Transform[] focusTargets;

    [Header("通り過ぎるゾンビの設定")]
    public GameObject walkerZombie;
    public Transform zombieStartPoint;
    public Transform zombieEndPoint;
    public float zombieWalkSpeed = 2.0f;
    public string animSpeedParam = "Speed";

    [Header("距離と位置の調整")]
    public float stopDistance = 2.0f;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("演出設定（時間）")]
    [Tooltip("行き：ズームにかかる時間（秒）")]
    public float zoomInDuration = 3.0f; // ★ここを3秒にする

    [Tooltip("帰り：元に戻る時間（秒）")]
    public float zoomOutDuration = 0.5f; // ★ここを0.5秒にする

    [Tooltip("ズーム状態で静止する時間（秒）")]
    public float stopDuration = 3.0f;

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
        playerScript.canControl = false;
        playerCamera = playerScript.cam.GetComponent<Camera>();

        // 元の位置・回転を記憶
        defaultPos = playerCamera.transform.localPosition;
        defaultRot = playerCamera.transform.localRotation;

        // 目標地点計算
        Vector3 centerPoint = CalculateCenterPoint();
        Vector3 directionFromCenter = (playerCamera.transform.position - centerPoint).normalized;
        Vector3 targetWorldPos = centerPoint + (directionFromCenter * stopDistance);
        targetWorldPos += positionOffset;

        Quaternion baseLookRot = Quaternion.LookRotation(centerPoint - targetWorldPos);
        Quaternion targetWorldRot = baseLookRot * Quaternion.Euler(rotationOffset);

        // 1. 行き（ズームイン）
        // ★ここでは zoomInDuration を使う
        float t = 0;
        Vector3 startWorldPos = playerCamera.transform.position;
        Quaternion startWorldRot = playerCamera.transform.rotation;

        while (t < 1.0f)
        {
            t += Time.deltaTime / zoomInDuration; // 行きの時間
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            playerCamera.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, smoothT);
            playerCamera.transform.rotation = Quaternion.Slerp(startWorldRot, targetWorldRot, smoothT);
            yield return null;
        }

        // 2. ゾンビ移動開始
        if (walkerZombie != null && zombieStartPoint != null && zombieEndPoint != null)
        {
            StartCoroutine(MoveZombieRoutine());
        }

        // 3. 静止
        yield return new WaitForSeconds(stopDuration);

        // 4. 帰り（ズームアウト）
        // ★ここでは zoomOutDuration を使う
        t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / zoomOutDuration; // 帰りの時間

            // 帰りは0.5秒と早いので、SmoothStepを使わずリニア（直線的）に戻すことで
            // 「最後のもたつき」をなくしてキビキビ動かします
            float sharpT = t;

            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, defaultPos, sharpT);
            playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, defaultRot, sharpT);
            yield return null;
        }

        // 復帰処理
        playerCamera.transform.localPosition = defaultPos;
        playerCamera.transform.localRotation = defaultRot;
        playerScript.SyncRotationToCurrent();
        playerScript.canControl = true;

        if (playOnlyOnce)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator MoveZombieRoutine()
    {
        walkerZombie.SetActive(true);
        var agent = walkerZombie.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        walkerZombie.transform.position = zombieStartPoint.position;
        walkerZombie.transform.LookAt(zombieEndPoint);

        Animator anim = walkerZombie.GetComponent<Animator>();
        if (anim != null) anim.SetFloat(animSpeedParam, 1.0f);

        while (Vector3.Distance(walkerZombie.transform.position, zombieEndPoint.position) > 0.1f)
        {
            walkerZombie.transform.position = Vector3.MoveTowards(
                walkerZombie.transform.position,
                zombieEndPoint.position,
                zombieWalkSpeed * Time.deltaTime
            );
            yield return null;
        }
        if (anim != null) anim.SetFloat(animSpeedParam, 0f);
    }

    Vector3 CalculateCenterPoint()
    {
        if (focusTargets == null || focusTargets.Length == 0) return transform.position + transform.forward * 2f;
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Transform t in focusTargets) { if (t != null) { center += t.position; count++; } }
        return count > 0 ? center / count : transform.position;
    }

    void OnDrawGizmosSelected()
    {
        if (zombieStartPoint != null && zombieEndPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(zombieStartPoint.position, 0.3f);
            Gizmos.DrawLine(zombieStartPoint.position, zombieEndPoint.position);
            Gizmos.DrawSphere(zombieEndPoint.position, 0.3f);
        }
    }
}