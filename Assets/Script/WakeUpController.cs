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
    public InventryUI inventoryUI;
    public LighterSystem lighterSystem;
    public FlashlightSystem flashlightSystem;

    // private
    private bool isSleeping = false;
    private bool isWakingUp = false;

    void Awake()
    {
        // プレイヤーのTransformが設定されていなければ、このオブジェクト自身を使う（保険）
        if (playerTransform == null) playerTransform = transform;

        // CharacterControllerがあると強制移動を邪魔することがあるので、一旦オフにする
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // ロードデータがあるかチェック
        bool isLoadGame = IsLoadGame();

        if (isLoadGame)
        {
            // --- パターンA：セーブデータがある場合（続きから） ---
            if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
            {
                // 保存された位置と向きを適用
                playerTransform.position = SaveManager.Instance.currentData.playerPosition;
                playerTransform.rotation = SaveManager.Instance.currentData.playerRotation;
                Physics.SyncTransforms(); // 物理位置を即座に同期

                Debug.Log("ロード地点にスポーン: " + playerTransform.position);
            }

            // カメラを正面に向ける
            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // 状態設定
            isSleeping = false;

            // 操作許可などの初期化
            InitializeControls(true);
        }
        else
        {
            // --- パターンB：ニューゲームの場合（最初から） ---

            // 1. ベッドの位置へ強制移動
            playerTransform.position = spawnPosition;
            playerTransform.rotation = Quaternion.Euler(0, bedRotationY, 0);
            Physics.SyncTransforms(); // 物理位置を即座に同期

            Debug.Log("ニューゲーム地点(ベッド)にスポーン: " + playerTransform.position);

            // 2. カメラを真上に向ける（寝ている視点）
            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }

            // 状態設定
            isSleeping = true;

            // 操作禁止などの初期化
            InitializeControls(false);

            // ニューゲーム特有のアイテム状態設定
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
            if (inventoryUI != null)
            {
                inventoryUI.enabled = false;
            }
        }

        // 移動が終わったのでCharacterControllerをオンに戻す
        if (cc != null) cc.enabled = true;

        // カーソル設定（共通）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 寝ていて、まだ起き上がり中でない時に Bキー を押したら
        if (isSleeping && !isWakingUp)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                StartCoroutine(WakeUpSequence());
            }
        }
    }

    // 操作の有効/無効を一括設定する関数
    void InitializeControls(bool isEnabled)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = isEnabled;

        if (lighterSystem != null)
        {
            // ニューゲーム(false)の時は使用禁止、ロード(true)の時は使用許可
            lighterSystem.canUseLighter = isEnabled;
        }

        // flashlightSystemのcanUseFlashlight制御が必要ならここに追加
    }

    // 「セーブデータの続きかどうか」を判定する関数
    bool IsLoadGame()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            // シーン名が保存されている = 一度はセーブされているとみなす
            if (!string.IsNullOrEmpty(SaveManager.Instance.currentData.sceneName))
            {
                return true;
            }
        }
        return false;
    }

    // ゆっくり起き上がるコルーチン
    IEnumerator WakeUpSequence()
    {
        isWakingUp = true;
        Debug.Log("起き上がります...");

        float elapsed = 0f;
        Quaternion startRot = playerCamera.localRotation; // 真上（-90度）
        Quaternion endRot = Quaternion.Euler(0f, 0f, 0f); // 正面（0度）

        while (elapsed < wakeUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / wakeUpDuration;
            // イージング（滑らかに）
            t = Mathf.SmoothStep(0f, 1f, t);

            playerCamera.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        playerCamera.localRotation = endRot;

        // 起き上がった後の状態設定
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        if (lighterSystem != null)
        {
            lighterSystem.canUseLighter = true;
        }

        if (inventoryUI != null)
        {
            inventoryUI.enabled = true;
        }

        isSleeping = false;
        Debug.Log("おはようございます！操作可能です。");
    }
}