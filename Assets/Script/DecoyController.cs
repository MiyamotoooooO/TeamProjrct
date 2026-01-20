using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Decoi : MonoBehaviour
{
    [Header("行動範囲の中心")]
    [SerializeField] private Transform areaCenter;
    [Header("行動半径")]
    [SerializeField] private float moveRadius;
    [Header("目的地再設定猶予")]
    [SerializeField] private float repathDistance;
    [Header("効果時間")]
    [SerializeField] private float lifeTime;

    NavMeshAgent agent;

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.autoBraking = false;
        agent.stoppingDistance = 0f;
        agent.acceleration = 999f;

        //自分の位置を中心に設定する
        if (areaCenter == null)
            areaCenter = transform;

        Destroy(gameObject, lifeTime);

        SetNewDestination();
    }

    void Update()
    {
        if (!agent.pathPending &&　agent.remainingDistance <= repathDistance)
            SetNewDestination();
    }

    void SetNewDestination()
    {
        Vector3 random = Random.insideUnitSphere * moveRadius;
        random.y = 0;

        Vector3 candidate = areaCenter.position + random;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(
            candidate,
            out hit,
            moveRadius,
            NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
