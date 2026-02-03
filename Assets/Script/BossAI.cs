using System.Collections;
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
    public float detectDistance = 8f;
    public float attackDistance = 1.8f;

    [Header("攻撃設定")]
 
    public float appearDelay = 0.6f;
  

    [Header("逃走ポイント")]
    public Transform[] escapePoints;

    [Header("接触判定")]
    public float contactEscapeTime = 3f;
    float contactTimer = 0f;

    [Header("背後出現位置調整（待機）")]
    public float backDistance = 0.0f;   // プレイヤーからの距離（小さいほど近い）
    public float backHeightOffset = 0.0f; // 高さ調整（目線に合わせる）
    public float backSideOffset = 0.0f; // 左右ズレ（0で真後ろ）
    public float backWaitTime = 0.0f;

    int damageCount = 0;
    [Header("プレイヤー死亡処理")]
    public PlayerHealth playerHealth;

    [Header("ギミック用")]
    public int lookBackAttackCount = 3; // この攻撃回数のときだけ振り向き必須
    public int killAttackCount = 4;     // この回数でボス撃破

    [Header("HP")]
    public int hitCount = 0;

    State state = State.Idle;
    bool isProcessingAttack = false;
    [Header("Attack演出待ち時間")]
    public float attackHoldTime = 0f;
    public float attackwait = 0f;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        col = GetComponent<CapsuleCollider>();
        anim = GetComponent<Animator>();

        ResetToIdle();
    }

    void Update()
    {
        if (state == State.Dead) return;

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

        // ===== 接触判定 =====
        if (dist < attackDistance)
        {
            contactTimer += Time.deltaTime;

            if (contactTimer >= contactEscapeTime && !isProcessingAttack)
            {
                StartCoroutine(EscapeWarp());
            }
        }
        else
        {
            contactTimer = 0f;
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

        //初期化
        ResetToIdle();
    }
    IEnumerator BackAttackFlow()
    {
        isProcessingAttack = true;
        state = State.Attacking;
        agent.isStopped = true;

        // Attackジャンプ
        anim.SetBool("IsAttacking", true);
        //アニメーターを見せる待ち時間
        yield return new WaitForSeconds(attackHoldTime);

        // 消失
        col.enabled = false;
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
        float timer = 0f;
        bool lookedBack = false;
        while (timer < backWaitTime)
        {
            // プレイヤーが振り向いたか
            Vector3 dir = (transform.position - player.position).normalized;
            float dot = Vector3.Dot(player.forward, dir);

            if (dot > 0.7f)
            {
                lookedBack = true;
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // ▼ 3回目：振り向かないと死亡
        if (damageCount == lookBackAttackCount)
        {
            if (!lookedBack)
            {
                Debug.Log("プレイヤー死亡（3回目で振り向かなかった）");
                FreezeBoss();
                playerHealth.Die();
                yield break;
            }

            // 正解：振り向いた → 生存して次の攻撃へ
            Debug.Log("正解！振り向いたので次で倒せる");
            anim.SetBool("IsAttacking", false);
            ResetToIdle();
            yield break;
        }


        if (damageCount < lookBackAttackCount)
        {
            if (lookedBack)
            {
                Debug.Log("プレイヤー死亡（早く振り向いた）");
                FreezeBoss();
                playerHealth.Die();
                yield break;
            }
        
        }
        // 振り向かなかった → 逃走
        anim.SetBool("IsAttacking", false);
        StartCoroutine(EscapeWarp());
    }

    public void TakeDamage()
    {
        if (state == State.Dead) return;

        damageCount++;
        Debug.Log("ボス被弾 残り:" + damageCount);

        if (hitCount <= 0)
        {
            Die();
            return;
        }
        //3回目は背後を見る必要あり
        //被弾した時だけ
        // 4回目で撃破
        if (damageCount >= killAttackCount)
        {
            Die();
            return;
        }
        if (!isProcessingAttack)
        {
            StartCoroutine(BackAttackFlow());
        }
    }

    void Die()
    {
        state = State.Dead;
        agent.isStopped = true;

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
    }
}
