using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class TrapEventSystem : MonoBehaviour
{
    [Header("--- 必要なパーツ ---")]
    [Tooltip("檻のスクリプト（CageTrap）")]
    public CageTrap cageScript;

    [Tooltip("普段使っているメインカメラ")]
    public GameObject mainCamera;

    [Tooltip("今回作った演出用カメラ")]
    public GameObject trapCamera;

    [Tooltip("プレイヤー本体（回転させるため）")]
    public GameObject playerObj;

    [Tooltip("プレイヤーの移動スクリプト（止めるため）")]
    public MonoBehaviour playerMoveScript;

    [Header("--- 時間設定 ---")]
    [Tooltip("カメラが切り替わってから、檻が落ち始めるまでの待ち時間")]
    public float delayBeforeDrop = 0.5f;

    [Tooltip("檻が落ちてから、元の視点に戻るまでの時間")]
    public float eventDuration = 2.5f;

    [Header("--- アイテム連携 ---")]
    public LighterSystem lighterSystem;
    public FlashlightSystem flashlightSystem;

    // private
    private Quaternion originalRotation;
    private List<ZombieState> frozenZombies = new List<ZombieState>();
    private bool wasLighterOn = false;
    private bool wasFlashlightOn = false;

    class ZombieState
    {
        public GameObject obj;
        public Animator anim;
        public NavMeshAgent agent;
        public Rigidbody rb;
        public float originalAnimSpeed;
        public bool wasKinematic;
    }

    void Start()
    {
        if (cageScript != null)
        {
            // 檻のゲームオブジェクトごと非表示にする
            cageScript.gameObject.SetActive(false);
        }
    }

    // トリガーから呼ばれる関数
    public void StartTrapEvent()
    {
        StartCoroutine(EventSequence());
    }

    IEnumerator EventSequence()
    {
        if (lighterSystem != null)
        {
            wasLighterOn = lighterSystem.isLighterOn;
            lighterSystem.canUseLighter = false; // ロック
            lighterSystem.TurnOff(); // ★ここ重要：フラグだけでなく直接消す！
            lighterSystem.isLighterOn = false; // 内部状態もオフにしておく
        }

        if (flashlightSystem != null)
        {
            wasFlashlightOn = flashlightSystem.isFlashlightOn;
            flashlightSystem.canUseFlashlight = false; // ロック
            // FlashlightSystemにはTurnOff関数がないので、手動で消す処理を書くか、ApplyStateを使う
            flashlightSystem.isFlashlightOn = false;
            flashlightSystem.ApplyState(); // ★直接反映させる
        }

        // 1. プレイヤーの操作を禁止（動けなくする）
        if (playerMoveScript != null) playerMoveScript.enabled = false;

        if (playerObj != null)
        {
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;        // 移動速度をゼロに
                rb.angularVelocity = Vector3.zero; // 回転速度をゼロに
                // 念のため一時的に物理演算を止める（スリープさせる）
                rb.Sleep();
            }
        }

        StopAllZombies();

        // 2. カメラを切り替え
        if (mainCamera != null) mainCamera.SetActive(false);
        if (trapCamera != null) trapCamera.SetActive(true);

        // 3. プレイヤーを振り向かせる（後ろを向く）
        if (playerObj != null)
        {
            // 今の向きを保存
            originalRotation = playerObj.transform.rotation;
            // くるっと180度回転
            playerObj.transform.Rotate(0, 180, 0);
        }

        // 4. 「あっ！」と思わせるタメを作る
        yield return new WaitForSeconds(delayBeforeDrop);

        if (cageScript != null)
        {
            // まず表示する
            cageScript.gameObject.SetActive(true);
            // その後、落とす
            cageScript.ActivateTrap();
        }

        // 5. 檻を落とす！
        if (cageScript != null) cageScript.ActivateTrap();

        // 6. 檻が落ちきるまで（演出が終わるまで）待つ
        yield return new WaitForSeconds(eventDuration);

        // 7. カメラを元に戻す
        if (trapCamera != null) trapCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        // 8. プレイヤーの向きを元に戻す（前を向く）
        if (playerObj != null)
        {
            playerObj.transform.rotation = originalRotation;
        }

        if (lighterSystem != null)
        {
            lighterSystem.canUseLighter = true; // ロック解除

            if (wasLighterOn)
            {
                // 元々ついていたならつける
                lighterSystem.isLighterOn = true;
                lighterSystem.TurnOn();
            }
            else
            {
                // ★元々消えていたなら、念には念を入れて「消す！」と命令する
                lighterSystem.isLighterOn = false;
                lighterSystem.TurnOff();
            }
        }

        if (flashlightSystem != null)
        {
            flashlightSystem.canUseFlashlight = true; // ロック解除

            if (wasFlashlightOn)
            {
                flashlightSystem.isFlashlightOn = true;
                flashlightSystem.ApplyState();
            }
            else
            {
                // ★こちらも念入りに消す
                flashlightSystem.isFlashlightOn = false;
                flashlightSystem.ApplyState();
            }
        }

        ResumeAllZombies();

        // 9. 操作を許可（動けるようにする）
        if (playerMoveScript != null) playerMoveScript.enabled = true;
    }

    void StopAllZombies()
    {
        frozenZombies.Clear();
        int zombieLayer = LayerMask.NameToLayer("Zombie"); // "Zombie"レイヤーの番号を取得

        // シーン上のすべてのオブジェクトを検索（少し重いが確実）
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (var obj in allObjects)
        {
            // Zombieレイヤーのものだけ処理する
            if (obj.layer == zombieLayer)
            {
                ZombieState state = new ZombieState();
                state.obj = obj;
                state.anim = obj.GetComponent<Animator>();
                state.agent = obj.GetComponent<NavMeshAgent>();
                state.rb = obj.GetComponent<Rigidbody>();

                // アニメーションを止める（速度0にする）
                if (state.anim != null)
                {
                    state.originalAnimSpeed = state.anim.speed;
                    state.anim.speed = 0;
                }

                // 移動（NavMeshAgent）を止める
                if (state.agent != null && state.agent.isOnNavMesh)
                {
                    state.agent.isStopped = true;
                    state.agent.velocity = Vector3.zero;
                }

                // 物理演算を止める
                if (state.rb != null)
                {
                    state.wasKinematic = state.rb.isKinematic;
                    state.rb.isKinematic = true; // 完全に固定
                }

                // リストに追加して覚えておく
                frozenZombies.Add(state);
            }
        }
    }

    void ResumeAllZombies()
    {
        foreach (var state in frozenZombies)
        {
            if (state.obj == null) continue; // もし消滅していたら無視

            // アニメーション速度を戻す
            if (state.anim != null) state.anim.speed = state.originalAnimSpeed;

            // 移動を再開
            if (state.agent != null && state.agent.isOnNavMesh) state.agent.isStopped = false;

            // 物理演算を戻す
            if (state.rb != null)
            {
                state.rb.isKinematic = state.wasKinematic;
                state.rb.WakeUp();
            }
        }
        frozenZombies.Clear();
    }
}