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

    [Header("自動消去・フェード設定")]
    [Tooltip("すべて表示された後、消え始めるまでの待機時間（秒）")]
    public float displayTime = 3.0f;

    [Tooltip("うっすら消えていくフェードアウトの時間（秒）")]
    public float fadeDuration = 1.0f;

    // 内部変数
    private bool hasTriggered = false; // すでに発動したか

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

            // 色の透明度を100%に初期化しておく
            SetAlpha(1f);
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
                StartCoroutine(PlayTypewriterSequence());
            }
        }
    }

    // 徐々に表示 ➔ 待機 ➔ フェードアウトする一連のコルーチン
    IEnumerator PlayTypewriterSequence()
    {
        targetImage.fillAmount = 0f;
        SetAlpha(1f); // 念のためアルファ値を1(不透明)にしておく

        float timer = 0f;

        // 1. タイプライター風に徐々に表示
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

        // 完全に表示完了
        targetImage.fillAmount = 1.0f;

        // 2. 指定した時間だけ表示したまま待機する
        yield return new WaitForSeconds(displayTime);

        // 3. うっすら消えていく（フェードアウト）
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // 1(不透明)から0(透明)へ徐々に数値を下げる
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        // 完全に消して、オブジェクトを非アクティブにする
        SetAlpha(0f);
        targetImage.gameObject.SetActive(false);
    }

    // Alpha（透明度）を設定する補助関数
    private void SetAlpha(float alpha)
    {
        if (targetImage != null)
        {
            Color c = targetImage.color;
            c.a = alpha;
            targetImage.color = c;
        }
    }
}