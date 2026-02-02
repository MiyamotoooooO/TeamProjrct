using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombiePassByEvent : MonoBehaviour
{
    [Header("このイベントの名前を設定")]
    public string eventID = "Event_UniqueName";

    [Header("zoomcameraを参照")]
    public Transform[] focusTargets;

    [Header("演出用のゾンビを参照")]
    public GameObject walkerZombie;

    [Header("StartPointを参照")]
    public Transform zombieStartPoint;

    [Header("EndPointを参照")]
    public Transform zombieEndPoint;

    [Header("zombieの歩く速度")]
    public float zombieWalkSpeed = 2.0f;

    [Header("zombieの動きを止めるためにSpeedを記載")]
    public string animSpeedParam = "Speed";

    [Header("演出中に止める敵のLayer")]
    public string targetLayerName = "Enemy";

    [Header("zoomcameraの位置までカメラが寄る速度")]
    public float zoomInDuration = 3.0f;

    [Header("演出が終わってカメラがPlayerの視点に戻る時間")]
    public float zoomOutDuration = 0.5f;

    [Header("カメラが移動しきった後静止している時間")]
    public float stopDuration = 3.0f;

    [Header("周囲の敵を止める範囲")]
    public float stopDistance = 2.0f;

    [Header("カメラの位置ずれの調節用")]
    public Vector3 positionOffset = Vector3.zero;

    [Header("カメラの角度ずれの調節用")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("一度きりのイベントにするか")]
    public bool playOnlyOnce = true;

    // private
    private bool hasPlayed = false; // もう再生済みというフラグ
    private PlayerController playerScript; // 演出中にPlayerの動きを無効化
    private Camera playerCamera; // カメラを動かすために取得
    private Vector3 defaultPos; // 演出前のカメラのもとの位置を覚えておく変数
    private Quaternion defaultRot; // 演出前のカメラのもとの位置を覚えておく変数

    // 停止用リスト
    private List<Animator> pausedAnimators = new List<Animator>(); // 停止させたゾンビたちのリスト
    private List<NavMeshAgent> pausedAgents = new List<NavMeshAgent>(); // 停止させたゾンビたちのリスト

    void Start()
    {
        // このイベントが既に終わっているか？
        if (playOnlyOnce && !string.IsNullOrEmpty(eventID))
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsEventCompleted(eventID))
            {
                // 終わってるなら、イベント用ゾンビも消して、トリガーも消す
                if (walkerZombie != null) Destroy(walkerZombie);
                Destroy(gameObject);
            }
        }
    }

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
        StopEnemiesByLayer(); // 敵停止

        playerCamera = playerScript.cam.GetComponent<Camera>();
        defaultPos = playerCamera.transform.localPosition;
        defaultRot = playerCamera.transform.localRotation;

        // ターゲット位置計算
        Vector3 centerPoint = CalculateCenterPoint();
        Vector3 directionFromCenter = (playerCamera.transform.position - centerPoint).normalized;
        Vector3 targetWorldPos = centerPoint + (directionFromCenter * stopDistance) + positionOffset;
        Quaternion baseLookRot = Quaternion.LookRotation(centerPoint - targetWorldPos);
        Quaternion targetWorldRot = baseLookRot * Quaternion.Euler(rotationOffset);

        // ズームイン
        float t = 0;
        Vector3 startWorldPos = playerCamera.transform.position;
        Quaternion startWorldRot = playerCamera.transform.rotation;
        while (t < 1.0f)
        {
            t += Time.deltaTime / zoomInDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            playerCamera.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, smoothT);
            playerCamera.transform.rotation = Quaternion.Slerp(startWorldRot, targetWorldRot, smoothT);
            yield return null;
        }

        // ゾンビ移動
        if (walkerZombie != null && zombieStartPoint != null && zombieEndPoint != null)
        {
            StartCoroutine(MoveZombieRoutine());
        }

        yield return new WaitForSeconds(stopDuration);

        // ズームアウト
        t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / zoomOutDuration;
            float sharpT = t;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, defaultPos, sharpT);
            playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, defaultRot, sharpT);
            yield return null;
        }

        playerCamera.transform.localPosition = defaultPos;
        playerCamera.transform.localRotation = defaultRot;
        playerScript.SyncRotationToCurrent();
        playerScript.canControl = true;

        ResumeAllEnemies(); // 敵再開

        // セーブデータに「終わったよ」と記録する
        if (playOnlyOnce)
        {
            if (SaveManager.Instance != null && !string.IsNullOrEmpty(eventID))
            {
                SaveManager.Instance.MarkEventAsCompleted(eventID);
            }
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

        while (walkerZombie != null && Vector3.Distance(walkerZombie.transform.position, zombieEndPoint.position) > 0.1f)
        {
            walkerZombie.transform.position = Vector3.MoveTowards(walkerZombie.transform.position, zombieEndPoint.position, zombieWalkSpeed * Time.deltaTime);
            yield return null;
        }
        if (walkerZombie != null) Destroy(walkerZombie);
    }

    void StopEnemiesByLayer()
    {
        pausedAnimators.Clear();
        pausedAgents.Clear();
        NavMeshAgent[] allAgents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        int targetLayerIndex = LayerMask.NameToLayer(targetLayerName);
        if (targetLayerIndex == -1) return;

        foreach (var agent in allAgents)
        {
            if (agent.gameObject == walkerZombie) continue;
            if (agent.gameObject.layer == targetLayerIndex)
            {
                if (agent.enabled) { agent.isStopped = true; pausedAgents.Add(agent); }
                var anim = agent.GetComponent<Animator>();
                if (anim != null && anim.enabled) { anim.speed = 0f; pausedAnimators.Add(anim); }
            }
        }
    }

    void ResumeAllEnemies()
    {
        foreach (var agent in pausedAgents) if (agent != null) agent.isStopped = false;
        foreach (var anim in pausedAnimators) if (anim != null) anim.speed = 1f;
        pausedAgents.Clear();
        pausedAnimators.Clear();
    }

    Vector3 CalculateCenterPoint()
    {
        if (focusTargets == null || focusTargets.Length == 0) return transform.position + transform.forward * 2f;
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Transform t in focusTargets) { if (t != null) { center += t.position; count++; } }
        return count > 0 ? center / count : transform.position;
    }
}