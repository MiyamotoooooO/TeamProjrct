using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TypewriterEffect : MonoBehaviour
{
    [Header("表示設定")]
    [Tooltip("表示させたいUIのImage画像")]
    public Image targetImage;

    [Tooltip("表示にかける時間（秒）")]
    public float duration = 2.0f;

    [Tooltip("文字数（画像を何段階で表示するか）")]
    public int characterCount = 8;

    // 内部変数
    private bool hasTriggered = false; // すでに発動したか
    private bool isTyping = false;     // 今文字を表示している途中か
    private bool isFullDisplayed = false; // 最後まで表示されたか
    private Coroutine typingCoroutine; // アニメーション管理用

    void Start()
    {
        // 初期化：画像を隠す
        if (targetImage != null)
        {
            // Image設定を強制的にFilledにする
            targetImage.type = Image.Type.Filled;
            targetImage.fillMethod = Image.FillMethod.Horizontal;
            targetImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            targetImage.fillAmount = 0f;
            targetImage.gameObject.SetActive(false); // 最初は非表示
        }
    }

    void Update()
    {
        // 画像が表示されていない時は何もしない
        if (targetImage == null || !targetImage.gameObject.activeSelf) return;

        // Spaceキーが押された時の処理
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

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーが触れたら1回だけ発動
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            if (targetImage != null)
            {
                targetImage.gameObject.SetActive(true);
                typingCoroutine = StartCoroutine(PlayTypewriter());
            }
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

    // テキストを消す関数
    void CloseText()
    {
        if (targetImage != null)
        {
            targetImage.gameObject.SetActive(false); // 非表示にする
            targetImage.fillAmount = 0f;
        }
        isTyping = false;
        isFullDisplayed = false;

        // もし1回きりでなく、何度でもTriggerに触れたら表示したい場合は
        // hasTriggered = false; // この行のコメントを外してください
    }
}