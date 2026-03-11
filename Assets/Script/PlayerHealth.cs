using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(AudioSource))]
public class PlayerHealth : MonoBehaviour
{
    public bool isDead = false;

    [Header("設定")]
    public MonoBehaviour playerMovementScript;
    public Camera playerCamera;

    [Header("アイテム連携")]
    public LighterSystem lighterSystem;
    public FlashlightSystem flashlightSystem;

    [Header("砂嵐エフェクト")]
    public GameObject sandStormUI;
    public AudioClip sandStormSound;

    [Header("死亡演出の設定")]
    public float timeToRespawn = 4.0f;
    public float wobbleIntensity = 15.0f;
    public float wobbleSpeed = 2.0f;
    public float zAxisWobbleAmount = 30.0f;

    private Rigidbody rb;
    private VideoPlayer sandStormPlayer;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (sandStormUI != null)
        {
            sandStormPlayer = sandStormUI.GetComponent<VideoPlayer>();
            if (sandStormPlayer != null)
            {
                sandStormPlayer.Stop();
                // ========================================================
                // ★追加：ゲーム開始時に動画を裏で読み込んでおく（ラグ防止）
                // ========================================================
                sandStormPlayer.Prepare();
            }
            sandStormUI.SetActive(false);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (lighterSystem != null) lighterSystem.canUseLighter = false;

        // ========================================================
        // ★映像と音を即座に出す
        // ========================================================
        if (sandStormUI != null)
        {
            sandStormUI.SetActive(true);
            if (sandStormPlayer != null)
            {
                sandStormPlayer.time = 0;
                sandStormPlayer.Play();
            }
        }

        if (sandStormSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sandStormSound);
        }

        EnemyAI[] zombies = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI zombie in zombies)
        {
            zombie.ResetToInitialPosition();
        }

        StopAllEnemies();
        StartCoroutine(DeathSequence());
    }

    void StopAllEnemies()
    {
        NavMeshAgent[] allAgents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        foreach (NavMeshAgent agent in allAgents)
        {
            if (agent.gameObject == this.gameObject) continue;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            Animator anim = agent.GetComponent<Animator>();
            if (anim != null) anim.speed = 0;

            Rigidbody enemyRb = agent.GetComponent<Rigidbody>();
            if (enemyRb != null) enemyRb.isKinematic = true;

            MonoBehaviour[] scripts = agent.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script == null) continue;
                string name = script.GetType().Name;
                if (name == "EnemyAI" || name == "StatueEnemy" || name == "EnemyAttack" || name == "NavMeshAgent")
                {
                    script.enabled = false;
                }
            }
        }
    }

    void ResumeAllEnemies()
    {
        NavMeshAgent[] allAgents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        foreach (NavMeshAgent agent in allAgents)
        {
            if (agent.gameObject == this.gameObject) continue;

            MonoBehaviour[] scripts = agent.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script == null) continue;
                string name = script.GetType().Name;
                if (name == "EnemyAI" || name == "StatueEnemy" || name == "EnemyAttack" || name == "NavMeshAgent")
                {
                    script.enabled = true;
                }
            }

            if (agent.isOnNavMesh) agent.isStopped = false;

            Animator anim = agent.GetComponent<Animator>();
            if (anim != null) anim.speed = 1;

            Rigidbody enemyRb = agent.GetComponent<Rigidbody>();
            if (enemyRb != null) enemyRb.isKinematic = false;
        }
    }

    IEnumerator DeathSequence()
    {
        float timer = 0;
        Quaternion startRot = playerCamera.transform.localRotation;

        while (timer < timeToRespawn)
        {
            timer += Time.unscaledDeltaTime;

            float progress = timer / timeToRespawn;
            float currentIntensity = Mathf.Lerp(wobbleIntensity * 0.5f, wobbleIntensity * 1.5f, progress);

            float x = Mathf.Sin(timer * wobbleSpeed) * currentIntensity;
            float y = (Mathf.PerlinNoise(timer * 0.5f, 0) - 0.5f) * currentIntensity * 2.0f;
            float z = Mathf.Sin(timer * wobbleSpeed * 0.8f) * zAxisWobbleAmount * progress;

            playerCamera.transform.localRotation = startRot * Quaternion.Euler(x, y, z);
            yield return null;
        }

        // WakeUpControllerに「もう一度寝かせる」処理を丸投げする
        WakeUpController wakeUp = FindAnyObjectByType<WakeUpController>();
        if (wakeUp != null)
        {
            wakeUp.SetupRespawnState();
        }
        else
        {
            transform.position = new Vector3(0, 2, 0);
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        // UIやエフェクトを消して元に戻す
        if (sandStormUI != null)
        {
            sandStormUI.SetActive(false);
            if (sandStormPlayer != null) sandStormPlayer.Stop();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 止めていた敵の動きを再開させる
        ResumeAllEnemies();

        // 死亡中のイベントロックも念のため解除
        GlobalSubtitleState.IsAnySubtitlePlaying = false;

        isDead = false;
    }
}