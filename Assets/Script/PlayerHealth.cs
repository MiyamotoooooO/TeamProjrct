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
    bool isDead = false;

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
    public float timeToRespawn = 4.0f; // 4秒後にリスポーン

    [Tooltip("ふらつきの激しさ（全体）")]
    public float wobbleIntensity = 15.0f;
    [Tooltip("ふらつきの回転スピード")]
    public float wobbleSpeed = 2.0f;
    [Tooltip("Z軸（視界の傾き）の揺れ幅")]
    public float zAxisWobbleAmount = 30.0f;

    private Rigidbody rb;
    private VideoPlayer sandStormPlayer;
    private AudioSource audioSource; // 音を鳴らすコンポーネント

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>(); // AudioSource取得

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        // ゲーム開始時、砂嵐UIがあれば非表示にする
        if (sandStormUI != null)
        {
            sandStormPlayer = sandStormUI.GetComponent<VideoPlayer>();

            if (sandStormPlayer != null)
            {
                sandStormPlayer.Stop();
            }
            sandStormUI.SetActive(false);
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
        //if (flashlightSystem != null) flashlightSystem.canUseFlashlight = false;

        // --- 砂嵐と音の再生 ---
        if (sandStormUI != null)
        {
            sandStormUI.SetActive(true);

            if (sandStormPlayer != null)
            {
                sandStormPlayer.time = 0;
                sandStormPlayer.Play();
            }
        }

        // 音を鳴らす
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

        // 指定時間（4秒間）、ふらふらしながら待機
        while (timer < timeToRespawn)
        {
            timer += Time.deltaTime;

            // 進行度 (0.0 ～ 1.0)
            float progress = timer / timeToRespawn;

            // 時間とともに揺れを激しくする
            float currentIntensity = Mathf.Lerp(wobbleIntensity * 0.5f, wobbleIntensity * 1.5f, progress);

            float x = Mathf.Sin(timer * wobbleSpeed) * currentIntensity;
            float y = (Mathf.PerlinNoise(timer * 0.5f, 0) - 0.5f) * currentIntensity * 2.0f;
            float z = Mathf.Sin(timer * wobbleSpeed * 0.8f) * zAxisWobbleAmount * progress;

            playerCamera.transform.localRotation = startRot * Quaternion.Euler(x, y, z);

            yield return null;
        }

        // --- リスポーン処理 ---
        // 現在のシーンを再読み込みする
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}


/*using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.AI;



public class PlayerHealth : MonoBehaviour

{

    bool isDead = false;



    [Header("設定")]

    [Tooltip("プレイヤーの移動制御スクリプト")]

    public MonoBehaviour playerMovementScript;

    [Tooltip("プレイヤーのカメラ")]

    public Camera playerCamera;



    [Header("アイテム連携")]

    public LighterSystem lighterSystem;

    public FlashlightSystem flashlightSystem;



    [Header("死亡演出の設定")]

    [Tooltip("倒れる前にふらふらする時間（秒）")]

    public float wobbleDuration = 2.0f;

    [Tooltip("ふらつきの激しさ")]

    public float wobbleIntensity = 10.0f;

    [Tooltip("ふらつきの回転スピード")]

    public float wobbleSpeed = 10.0f;



    private Rigidbody rb;



    void Start()

    {

        rb = GetComponent<Rigidbody>();

        if (playerCamera == null)

            playerCamera = GetComponentInChildren<Camera>();

    }



    public void Die()

    {

        if (isDead) return;

        isDead = true;



        Debug.Log("プレイヤー死亡");



        // 移動操作をオフ

        if (playerMovementScript != null)

            playerMovementScript.enabled = false;



        if (lighterSystem != null)

        {

            lighterSystem.canUseLighter = false;

        }



        if (flashlightSystem != null)

        {

            flashlightSystem.canUseFlashlight = false;

        }



        StopAllEnemies();



        StartCoroutine(DeathSequence());

    }



    void StopAllEnemies()

    {

        // シーン上のすべての「NavMeshAgent（移動AI）」がついているものを探す

        NavMeshAgent[] allAgents = FindObjectsOfType<NavMeshAgent>();



        foreach (NavMeshAgent agent in allAgents)

        {

            // プレイヤー自身にNavMeshAgentがついている場合は除外

            if (agent.gameObject == this.gameObject) continue;



            // ① 移動を完全に止める

            if (agent.isOnNavMesh)

            {

                agent.isStopped = true;

                agent.velocity = Vector3.zero;

            }



            // ② アニメーションを止める（その場で凍り付く）

            Animator anim = agent.GetComponent<Animator>();

            if (anim != null)

            {

                anim.speed = 0;

            }



            // ③ 物理演算を止める（死体に押されたりしないように）

            Rigidbody enemyRb = agent.GetComponent<Rigidbody>();

            if (enemyRb != null)

            {

                enemyRb.isKinematic = true;

            }



            // ④ 攻撃スクリプトなどの思考回路をオフにする

            // （EnemyAI, StatueEnemy, AttackPoint などを無効化）

            MonoBehaviour[] scripts = agent.GetComponents<MonoBehaviour>();

            foreach (var script in scripts)

            {

                // もしスクリプト名が敵AI関連ならオフにする

                // ※あなたのプロジェクトにある敵スクリプトの名前をここに追加すると確実です

                string name = script.GetType().Name;

                if (name == "EnemyAI" || name == "StatueEnemy" || name == "EnemyAttack")

                {

                    script.enabled = false;

                }

            }

        }

    }



    IEnumerator DeathSequence()

    {

        // --- ① 指定した秒数だけ、360度ふらふらする ---

        float timer = 0;

        Quaternion startRot = playerCamera.transform.localRotation;



        while (timer < wobbleDuration)

        {

            timer += Time.deltaTime;



            // 時間経過（0.0 〜 1.0）

            float progress = timer / wobbleDuration;



            // 徐々に揺れを大きくする

            float currentIntensity = Mathf.Lerp(wobbleIntensity * 0.5f, wobbleIntensity * 1.5f, progress);



            float angle = timer * wobbleSpeed;

            float x = Mathf.Sin(angle) * currentIntensity; // 前後の揺れ

            float z = Mathf.Cos(angle) * currentIntensity; // 左右の揺れ



            // Y軸（横回転）にも少しノイズを入れて、焦点が定まらない感じに

            float y = (Mathf.PerlinNoise(timer, 0) - 0.5f) * 10f;



            // 回転を適用

            playerCamera.transform.localRotation = startRot * Quaternion.Euler(x, y, z);



            yield return null;

        }



        // --- ② 物理演算でバタリと倒れる ---

        rb.constraints = RigidbodyConstraints.None; // ロック解除



        // 倒れるきっかけの力を加える

        rb.AddTorque(transform.right * -50f, ForceMode.Impulse); // 後ろには倒れず、前にガクッといく

        rb.AddTorque(transform.forward * Random.Range(-20f, 20f), ForceMode.Impulse);



        // --- ③ 地面に着くまで待機（3秒） ---

        yield return new WaitForSeconds(3.0f);



        // --- ④ 動きを完全に止める（死体固定） ---

        rb.velocity = Vector3.zero;

        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;



        // カーソル表示

        Cursor.lockState = CursorLockMode.None;

        Cursor.visible = true;

    }

}*/