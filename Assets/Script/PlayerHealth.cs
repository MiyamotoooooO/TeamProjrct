using System.Collections;
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
    public float wobbleDuration = 3.0f; // 少し長くしました
    [Tooltip("ふらつきの激しさ（全体）")]
    public float wobbleIntensity = 15.0f;
    [Tooltip("ふらつきの回転スピード")]
    public float wobbleSpeed = 2.0f;
    [Tooltip("★Z軸（視界の傾き）の揺れ幅")]
    public float zAxisWobbleAmount = 30.0f; // 大きく傾くように設定

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

        if (lighterSystem != null) lighterSystem.canUseLighter = false;
        if (flashlightSystem != null) flashlightSystem.canUseFlashlight = false;

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
        // --- ① ふらふら演出（XYZすべて揺らす） ---
        float timer = 0;
        Quaternion startRot = playerCamera.transform.localRotation;

        while (timer < wobbleDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / wobbleDuration;

            // 時間とともに揺れを激しくする (0.5倍 〜 1.5倍)
            float currentIntensity = Mathf.Lerp(wobbleIntensity * 0.5f, wobbleIntensity * 1.5f, progress);

            // 複雑な揺れを作るための計算
            // X軸（上下）：ゆっくり見上げる・見下ろす動き
            float x = Mathf.Sin(timer * wobbleSpeed) * currentIntensity;

            // Y軸（左右）：キョロキョロする動き（ランダム要素強め）
            float y = (Mathf.PerlinNoise(timer * 0.5f, 0) - 0.5f) * currentIntensity * 2.0f;

            // ★Z軸（傾き）：船酔いのように大きくグワングワンさせる
            // Sin波を使って、左右に大きく傾ける
            float z = Mathf.Sin(timer * wobbleSpeed * 0.8f) * zAxisWobbleAmount * progress;
            // ※ progressを掛けることで、最初は小さく、死ぬ直前に大きく傾くようにしています

            // 回転を適用
            playerCamera.transform.localRotation = startRot * Quaternion.Euler(x, y, z);

            yield return null;
        }

        // --- ② 物理演算で倒れる ---
        rb.constraints = RigidbodyConstraints.None; // 回転ロック解除

        // 倒れる力を加える（今回はランダムな方向に）
        Vector3 fallDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        rb.AddForce(fallDirection * 2.0f, ForceMode.Impulse);

        // 回転力も加えて、バタリと倒れやすくする
        rb.AddTorque(transform.right * Random.Range(-50f, 50f), ForceMode.Impulse);
        rb.AddTorque(transform.forward * Random.Range(-50f, 50f), ForceMode.Impulse); // Z軸回転も加える

        // --- ③ 地面に着くまで待機 ---
        yield return new WaitForSeconds(3.0f);

        // --- ④ 死体固定 ---
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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