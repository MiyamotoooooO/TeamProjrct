using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class TrapEventSystem : MonoBehaviour
{
    [Header("セーブ設定")]
    [Tooltip("このイベント固有のID（例: Trap_Cage01）。他のイベントと被らない名前にしてください")]
    public string eventID = "UniqueTrapEvent"; // ★追加：セーブ用ID

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
        // ★セーブデータ確認
        // もし「この罠イベントはもう終わったよ」という記録があれば、
        // 演出は再生せず、最初から「落ちた状態」にする
        if (!string.IsNullOrEmpty(eventID) && SaveManager.Instance != null && SaveManager.Instance.IsEventCompleted(eventID))
        {
            if (cageScript != null)
            {
                // 檻を表示する
                cageScript.gameObject.SetActive(true);
                // すぐに落とす（演出なしで、物理的にそこに存在させる）
                cageScript.ActivateTrap();
            }
            // ここで return することで、下の「非表示にする処理」や「トリガー待ち」を行わない
            // （トリガー自体を消したい場合は Destroy(GetComponent<Collider>()); などを追加してもOK）
            return;
        }

        // --- 以下、まだイベントが起きていない場合の初期化 ---
        if (cageScript != null)
        {
            // 檻のゲームオブジェクトごと非表示にする（演出待ち）
            cageScript.gameObject.SetActive(false);
        }
    }

    // トリガーから呼ばれる関数
    public void StartTrapEvent()
    {
        // 念のため、既に終わっている場合は再生しない
        if (SaveManager.Instance != null && SaveManager.Instance.IsEventCompleted(eventID)) return;

        StartCoroutine(EventSequence());
    }

    IEnumerator EventSequence()
    {
        if (lighterSystem != null)
        {
            wasLighterOn = lighterSystem.isLighterOn;
            lighterSystem.canUseLighter = false;
            lighterSystem.TurnOff();
            lighterSystem.isLighterOn = false;
        }

        if (flashlightSystem != null)
        {
            wasFlashlightOn = flashlightSystem.isFlashlightOn;
            flashlightSystem.canUseFlashlight = false;
            flashlightSystem.isFlashlightOn = false;
            flashlightSystem.ApplyState();
        }

        // 1. プレイヤーの操作を禁止
        if (playerMoveScript != null) playerMoveScript.enabled = false;

        if (playerObj != null)
        {
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }

        StopAllZombies();

        // 2. カメラを切り替え
        if (mainCamera != null) mainCamera.SetActive(false);
        if (trapCamera != null) trapCamera.SetActive(true);

        // 3. プレイヤーを振り向かせる
        if (playerObj != null)
        {
            originalRotation = playerObj.transform.rotation;
            playerObj.transform.Rotate(0, 180, 0);
        }

        // 4. タメを作る
        yield return new WaitForSeconds(delayBeforeDrop);

        if (cageScript != null)
        {
            cageScript.gameObject.SetActive(true);
            cageScript.ActivateTrap();
        }

        // 6. 演出が終わるまで待つ
        yield return new WaitForSeconds(eventDuration);

        // 7. カメラを元に戻す
        if (trapCamera != null) trapCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        // 8. プレイヤーの向きを元に戻す
        if (playerObj != null)
        {
            playerObj.transform.rotation = originalRotation;
        }

        if (lighterSystem != null)
        {
            lighterSystem.canUseLighter = true;
            if (wasLighterOn)
            {
                lighterSystem.isLighterOn = true;
                lighterSystem.TurnOn();
            }
            else
            {
                lighterSystem.isLighterOn = false;
                lighterSystem.TurnOff();
            }
        }

        if (flashlightSystem != null)
        {
            flashlightSystem.canUseFlashlight = true;
            if (wasFlashlightOn)
            {
                flashlightSystem.isFlashlightOn = true;
                flashlightSystem.ApplyState();
            }
            else
            {
                flashlightSystem.isFlashlightOn = false;
                flashlightSystem.ApplyState();
            }
        }

        ResumeAllZombies();

        // 9. 操作を許可
        if (playerMoveScript != null) playerMoveScript.enabled = true;

        // ★最後に「このイベントは終わったよ」とセーブデータに記録する
        if (!string.IsNullOrEmpty(eventID) && SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkEventAsCompleted(eventID);
            Debug.Log("罠イベント完了を記録しました: " + eventID);
        }
    }

    void StopAllZombies()
    {
        frozenZombies.Clear();
        int zombieLayer = LayerMask.NameToLayer("Zombie");

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None); // Unity2023以降対応の書き方

        foreach (var obj in allObjects)
        {
            if (obj.layer == zombieLayer)
            {
                ZombieState state = new ZombieState();
                state.obj = obj;
                state.anim = obj.GetComponent<Animator>();
                state.agent = obj.GetComponent<NavMeshAgent>();
                state.rb = obj.GetComponent<Rigidbody>();

                if (state.anim != null)
                {
                    state.originalAnimSpeed = state.anim.speed;
                    state.anim.speed = 0;
                }

                if (state.agent != null && state.agent.isOnNavMesh)
                {
                    state.agent.isStopped = true;
                    state.agent.velocity = Vector3.zero;
                }

                if (state.rb != null)
                {
                    state.wasKinematic = state.rb.isKinematic;
                    state.rb.isKinematic = true;
                }

                frozenZombies.Add(state);
            }
        }
    }

    void ResumeAllZombies()
    {
        foreach (var state in frozenZombies)
        {
            if (state.obj == null) continue;

            if (state.anim != null) state.anim.speed = state.originalAnimSpeed;
            if (state.agent != null && state.agent.isOnNavMesh) state.agent.isStopped = false;

            if (state.rb != null)
            {
                state.rb.isKinematic = state.wasKinematic;
                state.rb.WakeUp();
            }
        }
        frozenZombies.Clear();
    }
}