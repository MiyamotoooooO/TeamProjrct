/*using UnityEngine;
using UnityEngine.EventSystems; // ★追加：UIのフォーカス解除に必要

public class GameSystemManager : MonoBehaviour
{
    [Header("ESCキーで開くメニュー画面")]
    [Tooltip("PauseMenuなどのUIパネルをここにセットしてください")]
    public GameObject pauseMenuPanel;

    [Header("停止させるプレイヤー操作スクリプト")]
    [Tooltip("FPSControllerやPlayerMovementなど、移動を担当するスクリプト")]
    public MonoBehaviour playerMovementScript;

    [Tooltip("カメラ回転を担当するスクリプト（移動と別の場合のみセット。同じなら空欄でOK）")]
    public MonoBehaviour playerCameraScript;

    // 内部変数
    private bool isPaused = false;
    private bool shouldShowCursor = false;

    void Start()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        // ゲーム開始時はカーソルを消す
        UpdateCursorState(false);
    }

    void Update()
    {
        // 1. ESCキーの処理
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // パスワード画面などが開いていない時だけ反応
            if (!PasswordDoor.IsAnyWindowOpen)
            {
                TogglePauseMenu();
            }
        }

        // 2. カーソルを表示すべきか判定
        // ポーズ中 または パスワード画面が開いている時はカーソルを出す
        shouldShowCursor = isPaused || PasswordDoor.IsAnyWindowOpen;

        // 3. カーソルの状態を適用
        UpdateCursorState(shouldShowCursor);
    }

    // メニューの開閉処理
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;

        // パネルの表示切り替え
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
        }

        if (isPaused)
        {
            // --- ポーズする時 ---
            Time.timeScale = 0f; // 時間を止める
            SetPlayerControl(false); // プレイヤーを止める
            Debug.Log("ポーズ開始");
        }
        else
        {
            // --- ゲームに戻る時 ---
            Time.timeScale = 1f; // 時間を動かす

            // ★重要：UIのボタン等が選択されたままだとカーソルが消えないことがあるので解除する
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            SetPlayerControl(true); // プレイヤーを動かす

            // ★念押し：強制的にカーソルをロックする
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            Debug.Log("ポーズ解除：ゲームに戻ります");
        }
    }

    // プレイヤーのスクリプトを有効/無効化する関数
    void SetPlayerControl(bool isEnabled)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = isEnabled;
        if (playerCameraScript != null) playerCameraScript.enabled = isEnabled;
    }

    // カーソルの表示・非表示を一括管理
    void UpdateCursorState(bool show)
    {
        if (show)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // ゲーム中は常にロックし続ける（他のスクリプトが外そうとするのを防ぐ）
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}*/