using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Player‚ğQÆ")]
    public Transform player;

    [Header("‘–‚é‘¬“x")]
    public float runDistance = 10f;

    [Header("UŒ‚‚ª“–‚½‚é”ÍˆÍ")]
    public float attackDistance = 5f;

    [Header("œpœj”¼Œa")]
    public float wanderRadius = 8f;

    [Header("œpœjŠÔŠu")]
    public float wanderInterval = 3f;

    NavMeshAgent agent;
    Animator animator;

    float timer;
    bool isAttacking;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>(); // Zombie—p
        timer = wanderInterval;
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;
        float distance = Vector3.Distance(transform.position, player.position);

        //UŒ‚ƒtƒF[ƒY
        if (distance <= attackDistance)
        {
            if (!isAttacking) //–ˆƒtƒŒ[ƒ€UŒ‚‚Í‚¢‚ç‚È‚¢‚æ‚¤‚É
            {
                agent.isStopped = true;
                animator.SetBool("IsAttacking", true);
                isAttacking = true;
            }

        }
        //’ÇŒ‚ƒtƒF[ƒY
        else if (distance <= runDistance)
        {
            ExitAttack(); //UŒ‚‰ğœˆ—
            agent.isStopped = false;
            agent.speed = 3.5f;
            if (agent.destination != player.position)
            {
                agent.SetDestination(player.position);
            }
        }
        else
        {
            ExitAttack();
            Wander();
        }
        //Animator§Œä
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    //œpœjˆ—
    void Wander()
    {
        timer += Time.deltaTime;
        //ˆÚ“®’†‚ÍV‚µ‚¢–Ú“I’n‚ğo‚³‚È‚¢
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
    // NavMeshã‚Ìƒ‰ƒ“ƒ_ƒ€’n“_æ“¾
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
        }

    }
}
