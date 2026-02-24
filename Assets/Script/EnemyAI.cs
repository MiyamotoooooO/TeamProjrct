using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Playerを参照")]
    public Transform player;
    NavMeshAgent agent;
    Animator animator;
    CapsuleCollider col;

    [Header("走る速度")]
    public float runDistance = 10f;

    [Header("攻撃が当たる範囲")]
    public float attackDistance = 2.5f;

    [Header("徘徊半径")]
    public float wanderRadius = 2f;

    [Header("徘徊間隔")]
    public float wanderInterval = 10f;

    //[Header("ジャンプスケア用")]
    //public Transform cameraFacePoint;

    [Header("音の設定")]
    [Tooltip("ボスの音（ここに音源を入れる）")]
    public AudioClip zombieSound;
    private AudioSource audioSource;
    [Header("ジャンプスケア")]
    public AudioClip jumpScareSound;

    bool isChasing = false;

    float timer;
    bool isAttacking;
    public bool IsAttacking { get; private set; }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>(); // Zombie用
        col = GetComponent<CapsuleCollider>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false; // 勝手に鳴らないように
            audioSource.loop = true;        // ループしないように
            audioSource.clip = zombieSound; //ここでセット
        }
        timer = wanderInterval;
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;
        float distance = Vector3.Distance(transform.position, player.position);

        //攻撃フェーズ
        if (distance <= attackDistance)
        {
            if (!isAttacking) //毎フレーム攻撃はいらないように
            {
                agent.isStopped = true;
                animator.SetBool("IsAttacking", true);
                isAttacking = true;
                IsAttacking = true; // ←追加
            }

        }
        //追撃フェーズ
        else if (distance <= runDistance)
        {
            ExitAttack(); //攻撃解除処理
                          // ★ 追いかけ開始した瞬間
            if (!isChasing)
            {
                Debug.Log("追いかけ開始");
                isChasing = true;

                if (audioSource != null && zombieSound != null)
                {
                    audioSource.Play();   // ループ再生
                }
            }
            agent.isStopped = false;
            agent.speed = 3.5f;
            if (agent.destination != player.position)
            {
                agent.SetDestination(player.position);
            }

            else
            {
                ExitAttack();

                if (isChasing)
                {
                    isChasing = false;

                    if (audioSource != null)
                    {
                        audioSource.Stop();
                    }
                }
                Wander();
            }
            //Animator制御
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        //徘徊処理
        void Wander()
        {
            timer += Time.deltaTime;
            //移動中は新しい目的地を出さない
            if (agent.hasPath && agent.remainingDistance > 0.5f)
                return;

            if (timer >= wanderInterval)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius);
                agent.speed = 1.5f;
                agent.SetDestination(newPos);
                timer = 0;
            }
        }
        // NavMesh上のランダム地点取得
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
    }
    public void StopChaseSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isChasing = false;
    }
}
