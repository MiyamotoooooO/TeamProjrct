using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombiePassByEvent : MonoBehaviour
{
    [Header("セーブ設定（重要！）")]
    [Tooltip("このイベントの名前（ID）。他のイベントと被らない名前にしてください。例：Event_ZombieCorridor")]
    public string eventID = "Event_UniqueName";

    [Header("ターゲット設定")]
    public Transform[] focusTargets;

    [Header("通り過ぎるゾンビの設定")]
    public GameObject walkerZombie;
    public Transform zombieStartPoint;
    public Transform zombieEndPoint;
    public float zombieWalkSpeed = 2.0f;
    public string animSpeedParam = "Speed";

    [Header("周囲の敵の停止設定")]
    public string targetLayerName = "Enemy";

    [Header("演出設定")]
    public float zoomInDuration = 3.0f;
    public float zoomOutDuration = 0.5f;
    public float stopDuration = 3.0f;
    public float stopDistance = 2.0f;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("一度きりのイベントにするか")]
    public bool playOnlyOnce = true;

    // private
    private bool hasPlayed = false;
    private PlayerController playerScript;
    private Camera playerCamera;
    private Vector3 defaultPos;
    private Quaternion defaultRot;

    // 停止用リスト
    private List<Animator> pausedAnimators = new List<Animator>();
    private List<NavMeshAgent> pausedAgents = new List<NavMeshAgent>();

    void Start()
    {
        // ★ゲーム開始時チェック：このイベントが既に終わっているか？
        if (playOnlyOnce && !string.IsNullOrEmpty(eventID))
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsEventCompleted(eventID))
            {
                // 終わってるなら、イベント用ゾンビも消して、自分（トリガー）も消す
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

        // ★演出終了：セーブデータに「終わったよ」と記録する
        if (playOnlyOnce)
        {
            if (SaveManager.Instance != null && !string.IsNullOrEmpty(eventID))
            {
                SaveManager.Instance.MarkEventAsCompleted(eventID);
                // ここで SaveManager.Instance.SaveGame(); を呼べば、イベント直後にオートセーブも可能です
            }
            Destroy(gameObject);
        }
    }

    // --- (以下、前回と同じ移動・停止処理) ---
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