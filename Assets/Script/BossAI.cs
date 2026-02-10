using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    enum State
    {
        Idle,
        Chase,
        AttackWait,
        Attacking,
        Escape,
        Dead
    }

    [Header("参照")]
    public Transform player;
    NavMeshAgent agent;
    CapsuleCollider col;
    Animator anim;

    [Header("距離設定")]
    public float detectDistance = 8.0f; //索敵距離

    [Header("攻撃設定")]
    public float appearDelay = 0.3f;　//ワープしてから姿を見せるまでの時間

    [Header("逃走ポイント")]
    public Transform[] escapePoints;

    [Header("接触判定")]
    public float contactEscapeTime = 3f;
    float contactTimer = 0.0f;

    [Header("背後出現位置調整（待機）")]
    public float backDistance = 1.2f;   // プレイヤーからの距離（小さいほど近い）
    public float backHeightOffset = 0.0f; // 高さ調整（目線に合わせる）
    public float backSideOffset = 0.0f; // 左右ズレ（0で真後ろ）
    public float backWaitTime = 1.0f;


    [Header("プレイヤー死亡処理")]
    public PlayerHealth playerHealth;

    [Header("ギミック用")]
    public int lookBackAttackCount = 3; // この攻撃回数のときだけ振り向き必須
    public int killAttackCount = 4;     // この回数でボス撃破
    int damageCount = 0;

    State state = State.Idle;
    bool isProcessingAttack = false;

    [Header("Attack演出待ち時間")]
    public float attackHoldTime = 1.0f;
    public float attackwait = 1.5f;

    [Header("プレイヤー位置固定")]
    public bool lockPlayerPosition = false;
    Vector3 lockedPlayerPosition;

    [Header("ジャンプスケア演出(視界強奪)")]
    public float hijackFOV = 50f;      // 顔ドアップ時のFOV
    public float hijackDistance = 0.3f; // 顔との距離（かなり近く）
    public float hijackTime = 0.2f;    // 奪う速さ
    public Transform faceTarget; // ボスの顔（Headボーン推奨）
    bool isCameraHijacked = false;
    [Header("ジャンプスケア後の待ち時間")]
    public float deathDelayAfterHijack = 0.1f;

    [Header("ジャンプスケア用固定カメラ位置")]
    public Transform cameraFacePoint; // ★追加
    [Header("カメラ制御")]
    public MonoBehaviour playerCameraController;

    [Header("ジャンプスケア時カメラ揺れ")]
    public float shakePosAmount = 0.03f;   // 位置揺れ（かなり大きめOK）
    public float shakeRotAmount = 5f;      // 回転揺れ（超重要）
    public float shakeFrequency = 8f;     // 揺れの速さ

    [Header("3回目猶予時間")]
    public float finalAttackGraceTime = 2f;
    
    Coroutine finalGraceCoroutine = null;
    bool isFinalGraceActive = false;
    bool canTakeDamage = true; //演出中でもダメージを受けられるか
    
    [Header("音の設定")]
    [Tooltip("ボスの音（ここに音源を入れる）")]
    public AudioClip bosuSound;
    private AudioSource audioSource;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        col = GetComponent<CapsuleCollider>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false; // 勝手に鳴らないように
            audioSource.loop = false;        // ループしないように
        }
        ResetToIdle();
    }

    void Update()
    {
        if (state == State.Dead) return;

        //プレイヤー座標固定
        if (lockPlayerPosition)
        {
            player.position = lockedPlayerPosition;
        }

        //宣言
        float dist = Vector3.Distance(transform.position, player.position);

        UpdateAnimator();

        // ===== Idle中：近づいたら追跡 =====
        if (state == State.Idle && dist < detectDistance)
        {
            ResumeBoss();
            StartChase();
        }

        // ===== 追跡中 =====
        if (state == State.Chase)
        {
            agent.SetDestination(player.position);
        }


    }
    void OnCollisionStay(Collision collision)
    {
        if (state == State.Dead) return;
        if (isProcessingAttack) return;

        if (collision.transform == player)
        {
            //// 接触中は押さない
            //agent.isStopped = true;
            //agent.velocity = Vector3.zero;

            contactTimer += Time.deltaTime;

            if (contactTimer >= contactEscapeTime)
            {
                Debug.Log("接触維持 → エスケープワープ");
                StartCoroutine(EscapeWarp());
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.transform == player)
        {
            contactTimer = 0f;

            // 追跡状態なら再開
            if (state == State.Chase)
            {
                agent.isStopped = false;
            }
        }
    }


    void FreezeBoss()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        anim.speed = 0f;
    }

    void ResumeBoss()
    {
        anim.speed = 1f;
        agent.isStopped = false;
    }

    // =========================
    // Animator制御（ゾンビ方式）
    // =========================
    void UpdateAnimator()
    {
        // 攻撃中は一切Speedを触らない
        if (state == State.Attacking || state == State.AttackWait)
        {
            anim.SetBool("IsAttacking", true);
            anim.SetFloat("Speed", 0f);
            return;
        }

        anim.SetBool("IsAttacking", false);

        // ★ agent.isStopped の時は Speed を 0 固定
        float speed = agent.isStopped ? 0f : agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);
    }


    void StartChase()
    {
        state = State.Chase;
        agent.isStopped = false;

        //プレイヤーに「今回の遭遇で一回だけ攻撃OK」を与える
        player.GetComponent<PlayerAttack>().EnbleAttack();
    }
    IEnumerator EscapeWarp()
    {
        isProcessingAttack = true;

        // 完全停止
        FreezeBoss();

        // 消失
        col.enabled = false;
        GetComponentInChildren<Renderer>().enabled = false;

        yield return new WaitForSeconds(0.2f);

        // ランダム逃走
        Transform point = escapePoints[Random.Range(0, escapePoints.Length)];
        agent.Warp(point.position);

        yield return new WaitForSeconds(0.2f);

        col.enabled = true;
        GetComponentInChildren<Renderer>().enabled = true;

        player.GetComponent<PlayerAttack>().DisableAttack();
        ResumeBoss();
        ResetToIdle();

        canTakeDamage = true;
    }
    IEnumerator BackAttackFlow()
    {
        isProcessingAttack = true;
        canTakeDamage = true;        // ★待ち時間中でも殴れる
        state = State.Attacking;
        agent.isStopped = true;

        // Attackジャンプ
        anim.SetBool("IsAttacking", true);
        //アニメーターを見せる待ち時間
        yield return new WaitForSeconds(attackHoldTime);

        // 消失
        col.enabled = true;
        GetComponentInChildren<Renderer>().enabled = false;

        // 背後ワープ

        // プレイヤーの真後ろベース
        Vector3 backPos =
            player.position
            - player.forward * backDistance      // 距離
            + Vector3.up * backHeightOffset      // 高さ
            + player.right * backSideOffset;     // 左右ズレ

        agent.Warp(backPos);

        // 必ずプレイヤーを見る
        transform.LookAt(
            new Vector3(player.position.x, transform.position.y, player.position.z)
        );
        //出現後の待機時間
        yield return new WaitForSeconds(attackwait);
        //停止
        FreezeBoss();

        yield return new WaitForSeconds(appearDelay);

        col.enabled = true;
        GetComponentInChildren<Renderer>().enabled = true;

        // ===== 背後待機 =====
        player.GetComponent<PlayerAttack>().EnbleAttack(); // ★ここで攻撃許可
        canTakeDamage = true;

        float timer = 0f;
        bool lookedBack = false;
        while (timer < backWaitTime)
        {
            Vector3 toBoss = (transform.position - player.position).normalized;

            // 水平成分だけで判定（上下視点ブレ防止）
            toBoss.y = 0;
            Vector3 playerForward = player.forward;
            playerForward.y = 0;

            float dot = Vector3.Dot(playerForward.normalized, toBoss.normalized);

            // ★ 半分以上振り向いたら即拉致
            if (dot > 0.0f)
            {
                lookedBack = true;
                if (damageCount != lookBackAttackCount)
                {
                    if (isCameraHijacked) yield break;

                    isCameraHijacked = true;
                    FreezeBoss();
                    yield return StartCoroutine(CameraHijackHoldAndKill());
                    yield break;

                }
                //3回目は助かる
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
        player.GetComponent<PlayerAttack>().DisableAttack(); // 攻撃NG
        //  3回目：振り向かないと死亡
        if (damageCount == lookBackAttackCount)
        {
            if (!lookedBack)
            {
                Debug.Log("プレイヤー死亡（3回目で振り向かなかった）");
                FreezeBoss();
                yield return StartCoroutine(CameraHijackHoldAndKill());
                yield break;
            }

            // 正解：振り向いた → 生存して次の攻撃へ
            Debug.Log("正解！振り向いたので次で倒せる");
            anim.SetBool("IsAttacking", false);


            state = State.Chase;
            //攻撃許可
            player.GetComponent<PlayerAttack>().EnbleAttack();

            isProcessingAttack = false;
            lockPlayerPosition = false;
            //最終猶予スタ-ト
            if (finalGraceCoroutine != null)
                StopCoroutine(finalGraceCoroutine);

            finalGraceCoroutine = StartCoroutine(FinalAttackGraceTimer());
            yield break;

        }

        // 振り向かなかった → 逃走
        anim.SetBool("IsAttacking", false);
        lockPlayerPosition = false;
        StartCoroutine(EscapeWarp());
    }
    IEnumerator FinalAttackGraceTimer()
    {
        isFinalGraceActive = true;

        float timer = 0f;
        while (timer < finalAttackGraceTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 3秒間攻撃されなかった → 死亡
        Debug.Log("3秒以内に攻撃しなかった → プレイヤー死亡");
        FreezeBoss();
        yield return StartCoroutine(CameraHijackHoldAndKill());
    }


    IEnumerator CameraHijackToFace()
    {
        if (audioSource != null && bosuSound != null)
        {
            Debug.Log("音再生");
            audioSource.clip = bosuSound; // 音をセット
            audioSource.Play();           // 再生！
        }
        col.enabled = true;
        GetComponentInChildren<Renderer>().enabled = true;

        Camera camComp = Camera.main;
        Transform cam = camComp.transform;

        float startFOV = camComp.fieldOfView;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        Vector3 targetPos = cameraFacePoint.position;
        Quaternion targetRot = cameraFacePoint.rotation;


        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / hijackTime;

            cam.position = Vector3.Lerp(startPos, targetPos, t);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, t);
            camComp.fieldOfView = Mathf.Lerp(startFOV, hijackFOV, t);

            yield return null;
        }

        // 完全固定
        cam.position = targetPos;
        cam.rotation = targetRot;
        camComp.fieldOfView = hijackFOV;
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    IEnumerator CameraHijackHoldAndKill()
    {
        // ★ プレイヤーカメラ操作を無効化
        if (playerCameraController != null)
            playerCameraController.enabled = false;
        // ① 顔まで寄る
        yield return StartCoroutine(CameraHijackToFace());

        // ② 顔ドアップのまま揺らし停止
        StartCoroutine(CameraShakeHold(deathDelayAfterHijack));

        // ★ ③「死ぬ前の硬直時間」
        yield return new WaitForSeconds(deathDelayAfterHijack);

        // ③ そのまま死亡
        playerHealth.Die();
    }
    IEnumerator CameraShakeHold(float duration)
    {
        Camera cam = Camera.main;
        Transform camTransform = cam.transform;

        Vector3 basePos = camTransform.position;
        Quaternion baseRot = camTransform.rotation;

        float timer = 0f;

        // ランダムシード（毎回違う揺れ）
        float seed = Random.Range(0f, 100f);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Time.time * shakeFrequency;

            float noiseX = Mathf.PerlinNoise(seed, t) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(seed + 10f, t) * 2f - 1f;
            float noiseRot = Mathf.PerlinNoise(seed + 20f, t) * 2f - 1f;

            // ★ 追加：最初強く → 徐々に弱く
            float strength = 1f - (timer / duration);

            Vector3 posOffset =
                new Vector3(noiseX, noiseY, 0f) * shakePosAmount * strength;

            Quaternion rotOffset =
                Quaternion.Euler(0f, 0f, noiseRot * shakeRotAmount * strength);

            camTransform.position = basePos + posOffset;
            camTransform.rotation = baseRot * rotOffset;

            yield return null;
        }


        camTransform.position = basePos;
        camTransform.rotation = baseRot;
    }
    public void TakeDamage()
    {
        //最終猶予中なら解除
        if (isFinalGraceActive)
        {
            isFinalGraceActive = false;
            if (finalGraceCoroutine != null)
                StopCoroutine(finalGraceCoroutine);
        }
        //プレイヤ―位置固定ON
        lockedPlayerPosition = player.position;
        lockPlayerPosition = true;

        if (state == State.Dead)
        {
            return;
        }
        if (!canTakeDamage)
        {
            return;
        }



        isProcessingAttack = false;
        canTakeDamage = false;

        damageCount++;
        Debug.Log("ボス被弾 残り:" + damageCount);

        // 4回目で撃破
        if (damageCount >= killAttackCount)
        {
            Die();
            return;
        }
        StartCoroutine(BackAttackFlow());
    }

    void Die()
    {
        state = State.Dead;
        agent.isStopped = true;

        player.GetComponent<PlayerAttack>().DisableAttack();

        anim.SetFloat("Speed", 0f);
        anim.SetBool("IsAttacking", false);

        col.enabled = false;
        GetComponentInChildren<Renderer>().enabled = false;

        Destroy(gameObject, 1.2f);
    }

    void ResetToIdle()
    {
        //攻撃
        state = State.Idle;
        isProcessingAttack = false;
        contactTimer = 0;

        //NavMesh停止
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        anim.speed = 1f;
        //Animator 初期化
        anim.Play("Idle", 0, 0f);
        anim.SetBool("IsAttacking", false);
        anim.SetFloat("Speed", 0f);

        canTakeDamage = true;
    }

}

