using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PoisonSpiderAI : MonoBehaviour
{
    [Header("エリア設定")]
    [Tooltip("毒蜘蛛が徘徊する＆プレイヤーを検知するエリアのBoxCollider")]
    public BoxCollider roamArea;

    [Header("移動スピード設定")]
    [Tooltip("徘徊中のスピード")]
    public float patrolSpeed = 2.0f;
    [Tooltip("プレイヤー追跡中のスピード")]
    public float chaseSpeed = 5.0f;
    [Tooltip("徘徊時、次の目的地に着いてから動き出すまでの待機時間（秒）")]
    public float patrolWaitTime = 1.0f;

    // 内部変数
    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private bool isChasing = false;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // プレイヤーとPlayerHealthスクリプトを自動取得
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        // 初期スピード設定と最初の徘徊ポイント設定
        agent.speed = patrolSpeed;
        SetRandomPatrolPoint();
    }

    void Update()
    {
        if (player == null || roamArea == null) return;

        // プレイヤーが指定エリア(roamArea)の中にいるかどうかを判定
        bool isPlayerInArea = roamArea.bounds.Contains(player.position);

        if (isPlayerInArea)
        {
            // プレイヤーがエリア内にいる -> 追跡モード
            isChasing = true;
            isWaiting = false; // 待機中ならキャンセル
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position); // 常にプレイヤーの位置を目的地にする
        }
        else
        {
            // プレイヤーがエリア外にいる -> 徘徊モード
            if (isChasing)
            {
                // 追跡から徘徊に切り替わった瞬間
                isChasing = false;
                agent.speed = patrolSpeed;
                SetRandomPatrolPoint(); // 追跡をやめて新しい徘徊ポイントへ
            }

            // 徘徊中の目的地到着判定
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isWaiting)
            {
                StartCoroutine(WaitAndPatrol());
            }
        }
    }

    // 目的地に着いたら少し待ってから次の場所へ行くコルーチン
    private IEnumerator WaitAndPatrol()
    {
        isWaiting = true;
        yield return new WaitForSeconds(patrolWaitTime);

        // 待機中にプレイヤーが入ってきていなければ、次の目的地へ
        if (!isChasing)
        {
            SetRandomPatrolPoint();
        }
        isWaiting = false;
    }

    // エリア内でランダムな目的地を設定する関数
    private void SetRandomPatrolPoint()
    {
        if (roamArea == null) return;

        // BoxColliderの範囲内でランダムな座標(X, Z)を生成
        Bounds bounds = roamArea.bounds;
        Vector3 randomPos = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            transform.position.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );

        // 生成した座標の近くにあるNavMesh上の歩ける場所を探す
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPos, out hit, 3.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // 見つからなかったらもう一度やり直す
            SetRandomPatrolPoint();
        }
    }

    // プレイヤーに触れた時の処理 (Triggerにしている場合)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && playerHealth != null)
        {
            playerHealth.Die();
        }
    }

    // プレイヤーに触れた時の処理 (物理Colliderにしている場合)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && playerHealth != null)
        {
            playerHealth.Die();
        }
    }
}