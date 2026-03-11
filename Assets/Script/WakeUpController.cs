using UnityEngine;
using System.Collections;

public class WakeUpController : MonoBehaviour
{
    [Header("スポーン設定")]
    [Tooltip("ニューゲーム時のスポーン座標（ベッドの位置）")]
    public Vector3 spawnPosition = new Vector3(0f, 2f, 0f);

    [Tooltip("ベッドで寝ている時の体の向き（Y軸回転）")]
    public float bedRotationY = 0f;

    [Header("演出設定")]
    [Tooltip("起き上がるのにかかる時間（秒）")]
    public float wakeUpDuration = 2.0f;

    [Header("割り当て")]
    public Transform playerTransform;
    public Transform playerCamera;
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour inventoryUI;

    // ※ここはもうInspectorで設定し忘れても自動で探すので大丈夫です！
    public LighterSystem lighterSystem;
    public FlashlightSystem flashlightSystem;

    [HideInInspector] public bool isSleeping = false;
    [HideInInspector] public bool isWakingUp = false;

    void Awake()
    {
        if (playerTransform == null) playerTransform = transform;

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        bool isLoadGame = IsLoadGame();

        if (isLoadGame)
        {
            if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
            {
                playerTransform.position = SaveManager.Instance.currentData.playerPosition;
                playerTransform.rotation = SaveManager.Instance.currentData.playerRotation;
                Physics.SyncTransforms();
            }

            if (playerCamera != null) playerCamera.localRotation = Quaternion.Euler(0f, 0f, 0f);

            isSleeping = false;
            InitializeControls(true);
        }
        else
        {
            playerTransform.position = spawnPosition;
            playerTransform.rotation = Quaternion.Euler(0, bedRotationY, 0);
            Physics.SyncTransforms();

            if (playerCamera != null) playerCamera.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            isSleeping = true;
            InitializeControls(false);

            if (flashlightSystem == null) flashlightSystem = FindAnyObjectByType<FlashlightSystem>();
            if (lighterSystem == null) lighterSystem = FindAnyObjectByType<LighterSystem>();

            if (flashlightSystem != null)
            {
                flashlightSystem.isFlashlightOn = true;
                flashlightSystem.ApplyState();
            }
            if (lighterSystem != null)
            {
                lighterSystem.isLighterOn = false;
                lighterSystem.ApplyState();
            }
            if (inventoryUI != null) inventoryUI.enabled = false;
        }

        if (cc != null) cc.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (isSleeping && !isWakingUp)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                StartCoroutine(WakeUpSequence());
            }
        }
    }

    void InitializeControls(bool isEnabled)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = isEnabled;

        if (lighterSystem == null) lighterSystem = FindAnyObjectByType<LighterSystem>();
        if (lighterSystem != null) lighterSystem.canUseLighter = isEnabled;
    }

    bool IsLoadGame()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            if (!string.IsNullOrEmpty(SaveManager.Instance.currentData.sceneName)) return true;
        }
        return false;
    }

    // =========================================================
    // ゲームオーバー時に呼び出される「もう一度寝かせる」専用関数
    // =========================================================
    public void SetupRespawnState()
    {
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 1. ベッドの位置へ強制移動
        playerTransform.position = spawnPosition;
        playerTransform.rotation = Quaternion.Euler(0, bedRotationY, 0);
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        // 2. カメラを真上に向ける（寝ている視点）
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        // 3. 状態を「寝ている」に戻す
        isSleeping = true;
        isWakingUp = false;

        // 操作を一時的に禁止（起き上がるまで）
        InitializeControls(false);
        if (inventoryUI != null) inventoryUI.enabled = false;

        // =========================================================
        // ★絶対消すマン：シーン内のライトシステムを自動で探し出して強制OFFにする
        // =========================================================
        FlashlightSystem[] allFlashlights = FindObjectsByType<FlashlightSystem>(FindObjectsSortMode.None);
        foreach (var fs in allFlashlights)
        {
            fs.isFlashlightOn = false;
            fs.ApplyState();
        }

        LighterSystem[] allLighters = FindObjectsByType<LighterSystem>(FindObjectsSortMode.None);
        foreach (var ls in allLighters)
        {
            ls.isLighterOn = false;
            ls.ApplyState();
        }

        // ★追加：寝ている間は、目の前にモデルが出ないように手ぶらにしておく
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            if (pc.FlashlightModel != null) pc.FlashlightModel.SetActive(false);
            if (pc.LighterModel != null) pc.LighterModel.SetActive(false);
        }
        // ===============================
        // 敵AIをリセット
        // ===============================
        EnemyChaseKiller[] enemies = FindObjectsByType<EnemyChaseKiller>(FindObjectsSortMode.None);

        foreach (var e in enemies)
        {
            e.ResetEnemyState();
        }
    }

    IEnumerator WakeUpSequence()
    {
        isWakingUp = true;

        float elapsed = 0f;
        Quaternion startRot = playerCamera.localRotation;
        Quaternion endRot = Quaternion.Euler(0f, 0f, 0f);

        while (elapsed < wakeUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / wakeUpDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            playerCamera.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        playerCamera.localRotation = endRot;

        if (playerMovementScript != null) playerMovementScript.enabled = true;

        if (lighterSystem == null) lighterSystem = FindAnyObjectByType<LighterSystem>();
        if (lighterSystem != null) lighterSystem.canUseLighter = true;

        if (inventoryUI != null) inventoryUI.enabled = true;

        // ★追加：起き上がったら、死ぬ前に持っていたアイテムを構え直す
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.UpdateItemModel();
        }

        isSleeping = false;
        isWakingUp = false;
    }
}