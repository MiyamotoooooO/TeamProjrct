using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class TrapEventSystem : MonoBehaviour
{
    [Header("このイベントの名前を記載")]
    public string eventID = "UniqueTrapEvent";

    [Header("CageTrapを参照")]
    public CageTrap cageScript;

    [Header("CageFallenPointを参照")]
    public Transform fallenPoint;

    [Header("普段使っているメインカメラ")]
    public GameObject mainCamera;

    [Header("演出用カメラ")]
    public GameObject trapCamera;

    [Header("Player本体")]
    public GameObject playerObj;

    [Header("Playerの移動スクリプト")]
    public MonoBehaviour playerMoveScript;

    [Header("落下する演出が発動するまでの時間")]
    public float delayBeforeDrop = 0.5f;

    [Header("このイベントの全体の長さ")]
    public float eventDuration = 2.5f;

    [Header("LighterSystemを参照")]
    public LighterSystem lighterSystem;

    [Header("FlashlightSystemを参照")]
    public FlashlightSystem flashlightSystem;

    // private
    private Quaternion originalRotation; // 罠にかかる直前のプレイヤーの体の向き
    private List<ZombieState> frozenZombies = new List<ZombieState>(); // 動きを止めたゾンビたちのリスト
    private bool wasLighterOn = false; // イベント前にライターがついていたかどうかのフラグ
    private bool wasFlashlightOn = false; // イベント前に懐中電灯がついていたかどうかのフラグ

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
        // セーブデータ確認
        if (!string.IsNullOrEmpty(eventID) && SaveManager.Instance != null && SaveManager.Instance.IsEventCompleted(eventID))
        {
            if (cageScript != null)
            {
                // 1. 檻を表示する
                cageScript.gameObject.SetActive(true);

                // 2. 物理的に落とすのではなく、落ちた後の場所にワープさせる
                if (fallenPoint != null)
                {
                    cageScript.transform.position = fallenPoint.position;
                    cageScript.transform.rotation = fallenPoint.rotation;
                    Debug.Log("ロード時：檻を落下後の位置に配置しました");
                }
                else
                {
                    // 落下地点が設定されてない場合は、仕方ないので通常通り落とす
                    cageScript.ActivateTrap();
                }

                // 3. 檻の物理演算が暴れないように固定する
                Rigidbody cageRb = cageScript.GetComponent<Rigidbody>();
                if (cageRb != null)
                {
                    cageRb.isKinematic = false; // 重力を有効にする
                }
            }
            // 演出は再生せず終了
            return;
        }

        // まだイベントが起きていない場合
        if (cageScript != null)
        {
            // 演出待ちのため非表示
            cageScript.gameObject.SetActive(false);
        }
    }

    // トリガーから呼ばれる関数
    public void StartTrapEvent()
    {
        // 既に終わっている場合は再生しない
        if (SaveManager.Instance != null && SaveManager.Instance.IsEventCompleted(eventID)) return;

        StartCoroutine(EventSequence());
    }

    IEnumerator EventSequence()
    {
        // アイテム使用停止
        if (lighterSystem != null)
        {
            wasLighterOn = lighterSystem.isLighterOn;
            lighterSystem.canUseLighter = false;
            //lighterSystem.TurnOff();
            lighterSystem.isLighterOn = false;
        }
        if (flashlightSystem != null)
        {
            wasFlashlightOn = flashlightSystem.isFlashlightOn;
            //flashlightSystem.canUseFlashlight = false;
            flashlightSystem.isFlashlightOn = false;
            flashlightSystem.ApplyState();
        }

        // 1. プレイヤー操作禁止
        if (playerMoveScript != null) playerMoveScript.enabled = false;
        if (playerObj != null)
        {
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null) { rb.velocity = Vector3.zero; rb.Sleep(); }
        }

        StopAllZombies();

        // 2. カメラ切り替え
        if (mainCamera != null) mainCamera.SetActive(false);
        if (trapCamera != null) trapCamera.SetActive(true);

        // 3. 振り向き
        if (playerObj != null)
        {
            originalRotation = playerObj.transform.rotation;
            playerObj.transform.Rotate(0, 180, 0);
        }

        // 4. タメ
        yield return new WaitForSeconds(delayBeforeDrop);

        // 5. 檻を落とす
        if (cageScript != null)
        {
            cageScript.gameObject.SetActive(true);
            cageScript.ActivateTrap();
        }

        // 6. 演出待ち
        yield return new WaitForSeconds(eventDuration);

        // 7. カメラ戻す
        if (trapCamera != null) trapCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        // 8. 向きを戻す
        if (playerObj != null)
        {
            playerObj.transform.rotation = originalRotation;
        }

        // アイテム復帰
        if (lighterSystem != null)
        {
            lighterSystem.canUseLighter = true;
            //if (wasLighterOn) { lighterSystem.isLighterOn = true; lighterSystem.TurnOn(); }
        }
        if (flashlightSystem != null)
        {
            //flashlightSystem.canUseFlashlight = true;
            if (wasFlashlightOn) { flashlightSystem.isFlashlightOn = true; flashlightSystem.ApplyState(); }
        }

        ResumeAllZombies();

        // 9. 操作許可
        if (playerMoveScript != null) playerMoveScript.enabled = true;

        // セーブ記録
        if (!string.IsNullOrEmpty(eventID) && SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkEventAsCompleted(eventID);
        }
    }

    void StopAllZombies()
    {
        frozenZombies.Clear();
        int zombieLayer = LayerMask.NameToLayer("Zombie");
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (var obj in allObjects)
        {
            if (obj.layer == zombieLayer)
            {
                ZombieState state = new ZombieState();
                state.obj = obj;
                state.anim = obj.GetComponent<Animator>();
                state.agent = obj.GetComponent<NavMeshAgent>();
                state.rb = obj.GetComponent<Rigidbody>();

                if (state.anim != null) { state.originalAnimSpeed = state.anim.speed; state.anim.speed = 0; }
                if (state.agent != null && state.agent.isOnNavMesh) { state.agent.isStopped = true; state.agent.velocity = Vector3.zero; }
                if (state.rb != null) { state.wasKinematic = state.rb.isKinematic; state.rb.isKinematic = true; }
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
            if (state.rb != null) { state.rb.isKinematic = state.wasKinematic; state.rb.WakeUp(); }
        }
        frozenZombies.Clear();
    }
}