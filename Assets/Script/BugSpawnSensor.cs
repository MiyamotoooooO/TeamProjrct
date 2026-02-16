using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BugSpawnSensor : MonoBehaviour
{
    [Header("BugSpawnerスクリプト")]
    [SerializeField] BugSpawner bugSpawner;

    // 一度だけ発動するためのフラグ
    public bool isTriggered = false;

    void OnTriggerEnter(Collider collision)
    {
        if (isTriggered) return;
        if (collision.CompareTag("Player"))
        {
            isTriggered = true;
            bugSpawner.SpawnBugs(100, 5);
        }
    }
}