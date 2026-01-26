using UnityEngine;

public class GameSystemManager : MonoBehaviour
{
    [Header("ESCキーで開くメニュー画面")]
    [Tooltip("PauseMenuなどのUIパネルをここにセットしてください")]
    public GameObject pauseMenuPanel;

    // 内部変数
    private bool isPaused = false;

    // カーソル状態を管理するための変数
    private bool shouldShowCursor = false;

    void Start()
    {
        // 最初はメニューを隠す
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        // ゲーム開始時はカーソルを消す
        UpdateCursorState(false);
    }

    void Update()
    {
        // 1. ESCキーの処理
        // ただし、パスワード画面が開いているときはESCは「パスワード画面を閉じる」に使われるので、
        // ここでは反応しないようにする
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!PasswordDoor.IsAnyWindowOpen)
            {
                TogglePauseMenu();
            }
        }

        // 2. カーソルを表示すべきか判定する
        // 条件：ポーズメニューが開いている OR パスワード画面が開いている
        shouldShowCursor = isPaused || PasswordDoor.IsAnyWindowOpen;

        // 3. カーソルの状態を強制的に適用する
        UpdateCursorState(shouldShowCursor);
    }

    // メニューの開閉処理
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
        }

        // 時間を止める・動かす
        if (isPaused)
        {
            Time.timeScale = 0f; // 時間停止
            Debug.Log("ゲームポーズ：メニューを開きました");
        }
        else
        {
            Time.timeScale = 1f; // 時間再開
            Debug.Log("ゲーム再開：メニューを閉じました");
        }
    }

    // カーソルの表示・非表示を適用する関数
    void UpdateCursorState(bool show)
    {
        if (show)
        {
            // カーソルを出す
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // カーソルを消して中央に固定する
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}