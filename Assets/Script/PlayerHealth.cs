using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class PlayerHealth : MonoBehaviour
{
    public bool isDead = false;

    // ★追加：シーンをまたいで（死んでも）持ち物を記憶しておくための魔法の変数
    public static List<string> keptItemsOnDeath = null;

    [Header("設定")]
    [Tooltip("プレイヤーの移動制御スクリプト")]
    public MonoBehaviour playerMovementScript;
    [Tooltip("プレイヤーのカメラ")]
    public Camera playerCamera;

    [Header("アイテム連携")]
    public LighterSystem lighterSystem;
    public FlashlightSystem flashlightSystem;

    [Header("砂嵐エフェクト")]
    [Tooltip("砂嵐を表示するUIオブジェクト")]
    public GameObject sandStormUI;
    [Tooltip("砂嵐の音")]
    public AudioClip sandStormSound;

    [Header("死亡演出の設定")]
    [Tooltip("死亡してからリスポーンするまでの時間（秒）")]
    public float timeToRespawn = 4.0f;

    [Tooltip("ふらつきの激しさ（全体）")]
    public float wobbleIntensity = 15.0f;
    [Tooltip("ふらつきの回転スピード")]
    public float wobbleSpeed = 2.0f;
    [Tooltip("Z軸（視界の傾き）の揺れ幅")]
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
            }
            sandStormUI.SetActive(false);
        }

        // ★追加：リスポーン時に記憶しておいたアイテムを復元する
        StartCoroutine(RestoreInventoryRoutine());
    }

    // ★追加：他のスクリプトの準備が終わった後にアイテムを復元する処理
    IEnumerator RestoreInventoryRoutine()
    {
        yield return null; // 1フレーム待機して、InventoryManagerのStartが終わるのを待つ

        if (keptItemsOnDeath != null)
        {
            InventoryManager inv = FindAnyObjectByType<InventoryManager>();
            if (inv != null)
            {
                // 記憶しておいた持ち物を復元する。
                // （LoadItemDataの中で ReflectInventoryToScene が呼ばれるので、マップのアイテムも自動で消えます！）
                inv.LoadItemData(new List<string>(keptItemsOnDeath));
                Debug.Log("死亡時の持ち物を復元し、マップのアイテムを消去しました！");
            }
            // 復元が終わったらリセットしておく（ニューゲーム時に影響しないように）
            keptItemsOnDeath = null;
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("プレイヤー死亡");

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (lighterSystem != null) lighterSystem.canUseLighter = false;

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

        // ★追加：シーンをリロードする直前に、今の持ち物を記憶しておく！
        InventoryManager inv = FindAnyObjectByType<InventoryManager>();
        if (inv != null)
        {
            keptItemsOnDeath = new List<string>(inv.currentItems);
        }

        Time.timeScale = 1f;

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}