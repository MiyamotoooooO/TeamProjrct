using System.Collections;
using UnityEngine;

public class FrogTongueController : MonoBehaviour
{
    [Header("舌の設定")]
    [Tooltip("【重要】舌の根元となる空のオブジェクト（TonguePivot）をここにセット")]
    public Transform tonguePivot;

    [Tooltip("実際の舌の見た目（TongueMesh）。最初は非表示になります")]
    public GameObject tongueMeshObject;

    [Tooltip("通常時（ターゲットがいない時）の最大長さ")]
    public float maxTongueLength = 5.0f;

    [Tooltip("舌が伸びるスピード（秒）")]
    public float extendDuration = 0.15f;

    [Tooltip("伸びきった状態で停止する時間（秒）")]
    public float stayDuration = 0.1f;

    [Tooltip("舌が戻るスピード（秒）")]
    public float retractDuration = 0.2f;

    [Header("ターゲット検知（エイムアシスト）設定")]
    [Tooltip("検知する半径（メートル/マス）")]
    public float detectionRadius = 3.0f;

    [Tooltip("検知する前方の角度（この角度内にいれば吸い付く）")]
    public float detectionAngle = 45.0f;

    [Tooltip("検知する敵のタグ名（毒蜘蛛のタグ）")]
    public string targetTag = "PoisonSpider";

    [Header("オーディオ（任意）")]
    [Tooltip("舌を出す時の音")]
    public AudioClip shootSound;
    private AudioSource audioSource;

    // 内部変数
    private bool isShooting = false;
    private Vector3 initialPivotScale; // Pivotの初期スケール
    private Quaternion initialPivotRotation; // Pivotの初期の向き
    private PlayerController playerController;

    void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        audioSource = GetComponent<AudioSource>();

        if (tonguePivot != null)
        {
            // Pivotの初期スケールと向きを記憶
            initialPivotScale = tonguePivot.localScale;
            initialPivotRotation = tonguePivot.localRotation;

            // 最初はZスケールを0（長さゼロ）にしておく
            tonguePivot.localScale = new Vector3(initialPivotScale.x, initialPivotScale.y, 0f);
        }

        // 最初は舌の見た目を完全に非表示にしておく
        if (tongueMeshObject != null)
        {
            tongueMeshObject.SetActive(false);
        }
    }

    void Update()
    {
        // プレイヤーが操作できない時やインベントリを開いている時は無視
        if (playerController != null && (!playerController.canControl || playerController.isInventoryOpen))
        {
            return;
        }

        // 右クリック（1）で舌を伸ばす。
        if (Input.GetMouseButtonDown(1) && !isShooting && tonguePivot != null)
        {
            StartCoroutine(ShootTongueRoutine());
        }
    }

    IEnumerator ShootTongueRoutine()
    {
        isShooting = true;

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // 舌の見た目を表示する
        if (tongueMeshObject != null)
        {
            tongueMeshObject.SetActive(true);
        }

        // --- 毒蜘蛛を検知する処理 ---
        Transform targetSpider = null;
        float targetDistance = maxTongueLength;
        float minDistance = float.MaxValue;

        if (playerController != null && playerController.cam != null)
        {
            Transform camTransform = playerController.cam.transform;
            Collider[] colliders = Physics.OverlapSphere(camTransform.position, detectionRadius);

            foreach (var col in colliders)
            {
                if (col.CompareTag(targetTag))
                {
                    Vector3 dirToSpider = (col.transform.position - camTransform.position).normalized;
                    float angle = Vector3.Angle(camTransform.forward, dirToSpider);

                    if (angle <= detectionAngle)
                    {
                        float dist = Vector3.Distance(tonguePivot.position, col.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            targetSpider = col.transform;
                        }
                    }
                }
            }
        }

        // ★ターゲットが見つかった場合の処理
        if (targetSpider != null)
        {
            tonguePivot.LookAt(targetSpider.position);
            targetDistance = minDistance;

            // 【重要】舌が伸びる前に、毒蜘蛛の動きと当たり判定を無効化する
            // これにより、引き寄せ中にプレイヤーに当たっても死ななくなります。
            UnityEngine.AI.NavMeshAgent agent = targetSpider.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            Collider[] spiderColliders = targetSpider.GetComponentsInChildren<Collider>();
            foreach (var c in spiderColliders) c.enabled = false;
        }
        else
        {
            tonguePivot.localRotation = initialPivotRotation;
            targetDistance = maxTongueLength;
        }

        // 1. ビヨーンと伸びる（PivotのZスケールを0から目標の長さへ）
        float timer = 0f;
        Vector3 targetScale = new Vector3(initialPivotScale.x, initialPivotScale.y, targetDistance);

        while (timer < extendDuration)
        {
            timer += Time.deltaTime;
            float t = timer / extendDuration;
            tonguePivot.localScale = Vector3.Lerp(new Vector3(initialPivotScale.x, initialPivotScale.y, 0f), targetScale, t);
            yield return null;
        }
        tonguePivot.localScale = targetScale;

        // 2. 伸びきった状態で少し待機（蜘蛛にくっついた瞬間）
        yield return new WaitForSeconds(stayDuration);

        // 3. シュルッと口元に戻る ＆ ★蜘蛛を引き寄せる
        timer = 0f;
        Vector3 spiderStartPos = targetSpider != null ? targetSpider.position : Vector3.zero;

        while (timer < retractDuration)
        {
            timer += Time.deltaTime;
            float t = timer / retractDuration;

            // 舌を縮める
            tonguePivot.localScale = Vector3.Lerp(targetScale, new Vector3(initialPivotScale.x, initialPivotScale.y, 0f), t);

            // ★毒蜘蛛がいたら、舌と一緒に口元(tonguePivotの位置)へ引き寄せる
            if (targetSpider != null)
            {
                targetSpider.position = Vector3.Lerp(spiderStartPos, tonguePivot.position, t);
            }

            yield return null;
        }

        // 確実に0に戻す
        tonguePivot.localScale = new Vector3(initialPivotScale.x, initialPivotScale.y, 0f);
        tonguePivot.localRotation = initialPivotRotation;

        // 完全に口元に引っ込んだら、再び非表示にする
        if (tongueMeshObject != null)
        {
            tongueMeshObject.SetActive(false);
        }

        // ★口に戻ったタイミングで毒蜘蛛を消滅させる（捕食完了！）
        if (targetSpider != null)
        {
            Destroy(targetSpider.gameObject);
        }

        isShooting = false;
    }
}