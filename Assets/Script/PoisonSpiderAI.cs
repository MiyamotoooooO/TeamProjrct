using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PoisonSpiderAI : MonoBehaviour
{
    [Header("エリア設定")]
    public BoxCollider roamArea;

    [Header("移動スピード設定")]
    public float patrolSpeed = 2.0f;
    public float chaseSpeed = 5.0f;
    public float patrolWaitTime = 1.0f;

    private NavMeshAgent agent;
    private Transform currentTarget; // ★ 追加
    private PlayerHealth playerHealth;
    private bool isChasing = false;
    private bool isWaiting = false;

    private UnityEngine.Animation legacyAnim;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        legacyAnim = GetComponentInChildren<UnityEngine.Animation>();
        agent.speed = patrolSpeed;
        SetRandomPatrolPoint();
    }

    void Update()
    {
        // ★ アニメーション再生
        if (legacyAnim != null)
        {
            if (agent.velocity.magnitude > 0.1f) legacyAnim.CrossFade("walk");
            else legacyAnim.CrossFade("idle");
        }

        // ★ 一番近い「Player」を探す
        FindClosestTarget();

        if (currentTarget == null || roamArea == null) return;

        // ターゲットが指定エリアの中にいるかどうかを判定
        bool isTargetInArea = roamArea.bounds.Contains(currentTarget.position);

        if (isTargetInArea)
        {
            isChasing = true;
            isWaiting = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                agent.speed = patrolSpeed;
                SetRandomPatrolPoint();
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isWaiting)
            {
                StartCoroutine(WaitAndPatrol());
            }
        }
    }

    // ★ 一番近い「Player」を探す（デコイ対応）
    void FindClosestTarget()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Player");
        float minDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject t in targets)
        {
            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = t.transform;
            }
        }
        currentTarget = closest;
    }

    private IEnumerator WaitAndPatrol()
    {
        isWaiting = true;
        yield return new WaitForSeconds(patrolWaitTime);
        if (!isChasing) SetRandomPatrolPoint();
        isWaiting = false;
    }

    private void SetRandomPatrolPoint()
    {
        if (roamArea == null) return;
        Bounds bounds = roamArea.bounds;
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 1.0f, NavMesh.AllAreas))
            {
                Vector3 checkPos = new Vector3(hit.position.x, bounds.center.y, hit.position.z);
                if (bounds.Contains(checkPos))
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }
        NavMeshHit centerHit;
        if (NavMesh.SamplePosition(bounds.center, out centerHit, 5.0f, NavMesh.AllAreas)) agent.SetDestination(centerHit.position);
    }

    // プレイヤー（またはデコイ）に触れた時の処理
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // デコイの場合はDie()を持っていない可能性があるのでチェック
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null) health.Die();
            else if (other.name.Contains("Decoy")) Destroy(other.gameObject); // デコイなら破壊するなどの処理
        }
    }
}