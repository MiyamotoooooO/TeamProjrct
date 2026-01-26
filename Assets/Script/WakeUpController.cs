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
    public LighterSystem lighterSystem;
    public FlashlightSystem flashlightSystem;

    // private
    private bool isSleeping = true;
    private bool isWakingUp = false;

    IEnumerator Start()
    {
        yield return null;
        // ロードデータがあるかチェック
        bool isLoadGame = IsLoadGame();

        if (isLoadGame)
        {
            // --- パターンA：セーブデータがある場合（続きから） ---

            // ★追加：SaveManagerのデータを使って位置を復元する
            if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
            {
                // CharacterControllerが邪魔をして移動できない場合があるので、一瞬オフにする
                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                // 保存された位置と向きを適用
                playerTransform.position = SaveManager.Instance.currentData.playerPosition;
                playerTransform.rotation = SaveManager.Instance.currentData.playerRotation;

                // 物理演算の位置合わせ
                Physics.SyncTransforms();

                // コントローラーをオンに戻す
                if (cc != null) cc.enabled = true;

                Debug.Log("セーブされた位置にスポーンしました: " + playerTransform.position);
            }

            // 寝ていない状態にする
            isSleeping = false;

            // カメラを正面に向ける（寝ている角度 -90 を 0 に直す）
            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // 操作を許可する
            EnableControls(true);
        }
        else
        {
            // --- パターンB：ニューゲームの場合（最初から） ---

            // 1. プレイヤーを強制的にベッドの位置へ移動
            if (playerTransform != null)
            {
                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false; // 移動のためにオフ

                playerTransform.position = spawnPosition;
                playerTransform.rotation = Quaternion.Euler(0, bedRotationY, 0);

                Physics.SyncTransforms();
                if (cc != null) cc.enabled = true; // オンに戻す
            }

            // 2. カメラを真上に向ける（寝ている視点）
            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }

            // 3. 起きるまでは動けないように操作スクリプトを止める
            EnableControls(false);

            isSleeping = true;
        }

        // 共通設定：カーソルは消す
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

    // 操作の有効/無効を切り替える便利関数
    void EnableControls(bool isEnabled)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = isEnabled;
        if (lighterSystem != null) lighterSystem.canUseLighter = isEnabled;
        if (flashlightSystem != null) flashlightSystem.canUseFlashlight = isEnabled;
    }

    // ゆっくり起き上がるコルーチン
    IEnumerator WakeUpSequence()
    {
        isWakingUp = true;
        Debug.Log("起き上がります...");

        float elapsed = 0f;
        Quaternion startRotation = playerCamera.localRotation; // 真上（-90度）
        Quaternion endRotation = Quaternion.Euler(0f, 0f, 0f); // 正面（0度）

        while (elapsed < wakeUpDuration)
        {
            float t = elapsed / wakeUpDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            playerCamera.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.localRotation = endRotation;
        EnableControls(true); // 操作許可

        isSleeping = false;
        Debug.Log("おはようございます！操作可能です。");
    }
}