using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WakeUpSubtitleManager : MonoBehaviour
{
    [Header("表示設定")]
    [Tooltip("表示させたいUIのImage画像")]
    public Image targetImage;

    [Tooltip("表示にかける時間（秒）")]
    public float duration = 2.0f;

    [Tooltip("文字数（画像を何段階で表示するか）")]
    public int characterCount = 8;

    [Header("参照設定")]
    [Tooltip("プレイヤーの操作を止めるために使用します")]
    public PlayerController playerController;

    [Tooltip("ベッドから起き上がる処理を監視するために使用します")]
    public WakeUpController wakeUpController;

    // 内部変数
    private bool isWaitingForWakeUp = false; // 起き上がるのを待っている状態か
    private bool hasTriggered = false;       // すでに字幕が表示されたか
    private bool isTyping = false;           // 今文字を表示している途中か
    private bool isFullDisplayed = false;    // 最後まで表示されたか
    private Coroutine typingCoroutine;       // アニメーション管理用

    void Start()
    {
        // アタッチし忘れ防止のため、シーン内から自動取得
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (wakeUpController == null) wakeUpController = FindAnyObjectByType<WakeUpController>();

        // 初期化：画像を隠す
        if (targetImage != null)
        {
            targetImage.type = Image.Type.Filled;
            targetImage.fillMethod = Image.FillMethod.Horizontal;
            targetImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            targetImage.fillAmount = 0f;
            targetImage.gameObject.SetActive(false); // 最初は非表示
        }

        // ゲーム開始時に「寝ている状態（ニューゲーム）」なら、起きるのを待つフラグを立てる
        if (wakeUpController != null && wakeUpController.isSleeping)
        {
            isWaitingForWakeUp = true;
        }
        else
        {
            // ロードから始まった場合など、すでに起きている場合は字幕を出さない
            hasTriggered = true;
        }
    }

    void Update()
    {
        // --- 1. 起き上がりを検知して字幕スタート ---
        if (isWaitingForWakeUp && !hasTriggered)
        {
            // isSleepingとisWakingUpが両方ともfalseになった ＝ 起き上がりアニメーションが完了した！
            if (!wakeUpController.isSleeping && !wakeUpController.isWakingUp)
            {
                isWaitingForWakeUp = false;
                StartSubtitleSequence();
            }
        }

        // --- 2. 字幕表示中のSpaceキー操作 ---
        if (targetImage != null && targetImage.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isTyping)
                {
                    // パターン1：まだ文字が出ている途中なら「スキップ」
                    SkipAnimation();
                }
                else if (isFullDisplayed)
                {
                    // パターン2：すべて表示済みなら「閉じる」
                    CloseText();
                }
            }
        }
    }

    // ★追加：字幕スタートと同時にプレイヤーの操作をロックする
    private void StartSubtitleSequence()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        // プレイヤーの操作を無効化
        if (playerController != null)
        {
            playerController.canControl = false;
        }

        if (targetImage != null)
        {
            targetImage.gameObject.SetActive(true);
            typingCoroutine = StartCoroutine(PlayTypewriter());
        }
    }

    // 徐々に表示するコルーチン
    IEnumerator PlayTypewriter()
    {
        isTyping = true;
        isFullDisplayed = false;
        targetImage.fillAmount = 0f;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            if (characterCount > 0)
            {
                // 文字数に合わせてカクカク表示
                float steppedProgress = Mathf.Floor(progress * characterCount) / characterCount;
                targetImage.fillAmount = steppedProgress;
            }
            else
            {
                // 滑らか表示
                targetImage.fillAmount = progress;
            }
            yield return null;
        }

        // ループが終わったら完了状態にする
        FinishDisplay();
    }

    // スキップ処理
    void SkipAnimation()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine); // アニメーションを強制停止
        FinishDisplay(); // 完了状態にする
    }

    // 表示完了状態にする関数
    void FinishDisplay()
    {
        if (targetImage != null) targetImage.fillAmount = 1.0f; // 完全に表示
        isTyping = false;
        isFullDisplayed = true;
    }

    // ★追加：テキストを消し、プレイヤーの操作を再び有効にする
    void CloseText()
    {
        if (targetImage != null)
        {
            targetImage.gameObject.SetActive(false); // 非表示にする
            targetImage.fillAmount = 0f;
        }
        isTyping = false;
        isFullDisplayed = false;

        // 字幕が閉じたら、プレイヤーが再び動けるようにする！
        if (playerController != null)
        {
            playerController.canControl = true;
        }
    }
}