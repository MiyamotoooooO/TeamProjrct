using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("ターゲット設定")]
    [Tooltip("本来のプレイヤーを登録（デコイがない時のデフォルト）")]
    public Transform defaultPlayer;

    private Transform currentTarget; // 現在狙っているターゲット
    NavMeshAgent agent;
    Animator animator;
    AudioSource audioSource;

    [Header("移動・攻撃設定")]
    public float runDistance = 10f;
    public float attackDistance = 2.5f;
    public float wanderRadius = 5f;
    public float wanderInterval = 5f;

    [Header("音の設定")]
    public AudioClip zombieSound;
    public AudioClip jumpScareSound;

    bool isChasing = false;
    float timer;
    bool isAttacking;
    public bool IsAttacking { get; private set; }

    // ★ 追加：初期位置と回転を記憶する変数
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        // ★ ゲーム開始時の位置と向きを保存
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.clip = zombieSound;
        }
        timer = wanderInterval;
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;

        // 一番近い「Player」タグを探す
        FindClosestTarget();

        if (currentTarget == null)
        {
            Wander();
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        // 攻撃フェーズ
        if (distance <= attackDistance)
        {
            if (!isAttacking)
            {
                agent.isStopped = true;
                animator.SetBool("IsAttacking", true);
                isAttacking = true;
                IsAttacking = true;
            }
        }
        // 追撃フェーズ
        else if (distance <= runDistance)
        {
            ExitAttack();
            if (!isChasing)
            {
                isChasing = true;
                if (audioSource != null && zombieSound != null) audioSource.Play();
            }
            agent.isStopped = false;
            agent.speed = 3.5f;
            agent.SetDestination(currentTarget.position);
        }
        // 徘徊フェーズ
        else
        {
            if (isChasing)
            {
                isChasing = false;
                if (audioSource != null) audioSource.Stop();
            }
            Wander();
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    // ★ 追加：プレイヤーが死んだときに呼ばれるリセット関数
    public void ResetToInitialPosition()
    {
        // 追跡や攻撃の状態をすべて解除
        isChasing = false;
        isAttacking = false;
        IsAttacking = false;

        if (audioSource != null) audioSource.Stop();
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetFloat("Speed", 0);
        }

        // NavMeshAgentを一時止めてワープさせる
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            // 直接transformを書き換えるとNavMeshが壊れることがあるためWarpを使う
            agent.Warp(initialPosition);
        }

        transform.rotation = initialRotation;
        Debug.Log("ゾンビを初期位置に戻しました");
    }

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

    void Wander()
    {
        timer += Time.deltaTime;
        if (agent.hasPath && agent.remainingDistance > 0.5f) return;

        if (timer >= wanderInterval)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius);
            agent.speed = 1.5f;
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDir = Random.insideUnitSphere * dist + origin;
        NavMesh.SamplePosition(randDir, out NavMeshHit hit, dist, NavMesh.AllAreas);
        return hit.position;
    }

    void ExitAttack()
    {
        if (isAttacking)
        {
            animator.SetBool("IsAttacking", false);
            agent.isStopped = false;
            isAttacking = false;
            IsAttacking = false;
        }
    }

    public void StopChaseSound()
    {
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        isChasing = false;
    }
}