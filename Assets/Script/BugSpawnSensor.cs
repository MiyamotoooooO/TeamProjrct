using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BugSpawnSensor : MonoBehaviour
{
    [Header("BugSpawnerスクリプト")]
    [SerializeField] BugSpawner bugSpawner;

    public bool isTriggered = false;

    void OnTriggerEnter(Collider collision)
    {
        if (isTriggered) return;

        // ★追加：字幕やクイズが再生中なら、踏んでも無視する
        if (GlobalSubtitleState.IsAnySubtitlePlaying) return;

        if (collision.CompareTag("Player"))
        {
            isTriggered = true;
            // 虫を出す（BugSpawner側でTime.deltaTimeを使っていれば、時間停止中に止まります）
            bugSpawner.SpawnBugs(100, 5);
        }
    }
}