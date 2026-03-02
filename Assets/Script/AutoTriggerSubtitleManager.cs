using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AutoTriggerSubtitleManager : MonoBehaviour
{
    [Header("表示設定")]
    [Tooltip("順番に表示させたいUIのImage画像（＋ボタンで何個でも登録できます）")]
    public Image[] targetImages;

    [Tooltip("1つの画像を表示にかける時間（秒）")]
    public float duration = 2.0f;

    [Tooltip("文字数（画像を何段階で表示するか）")]
    public int characterCount = 8;

    [Header("自動消去・フェード・間隔設定")]
    [Tooltip("すべて表示された後、消え始めるまでの待機時間（秒）")]
    public float displayTime = 3.0f;

    [Tooltip("最後の字幕がうっすら消えていくフェードアウトの時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("前の字幕が消えてから、次の字幕が表示されるまでの間隔（秒）")]
    public float delayBetweenSubtitles = 0.5f;

    // 内部変数
    private bool hasTriggered = false;

    void Start()
    {
        if (targetImages != null)
        {
            foreach (Image img in targetImages)
            {
                if (img != null)
                {
                    img.type = Image.Type.Filled;
                    img.fillMethod = Image.FillMethod.Horizontal;
                    img.fillOrigin = (int)Image.OriginHorizontal.Left;
                    img.fillAmount = 0f;
                    img.gameObject.SetActive(false);
                    SetAlpha(img, 1f);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            // ★ 変更：他の字幕が再生中なら、発動を予約せずに完全に無視する
            if (GlobalSubtitleState.IsAnySubtitlePlaying) return;

            hasTriggered = true;
            StartCoroutine(PlaySequentialTypewriter());
        }
    }

    IEnumerator PlaySequentialTypewriter()
    {
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // ★ グローバルロックON

        if (targetImages == null || targetImages.Length == 0) yield break;

        for (int i = 0; i < targetImages.Length; i++)
        {
            Image currentImage = targetImages[i];
            if (currentImage == null) continue;

            currentImage.gameObject.SetActive(true);
            currentImage.fillAmount = 0f;
            SetAlpha(currentImage, 1f);

            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;

                if (characterCount > 0)
                {
                    float steppedProgress = Mathf.Floor(progress * characterCount) / characterCount;
                    currentImage.fillAmount = steppedProgress;
                }
                else
                {
                    currentImage.fillAmount = progress;
                }
                yield return null;
            }

            currentImage.fillAmount = 1.0f;
            yield return new WaitForSeconds(displayTime);

            if (i == targetImages.Length - 1)
            {
                timer = 0f;
                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                    SetAlpha(currentImage, alpha);
                    yield return null;
                }
            }

            SetAlpha(currentImage, 0f);
            currentImage.gameObject.SetActive(false);

            if (i < targetImages.Length - 1)
            {
                yield return new WaitForSeconds(delayBetweenSubtitles);
            }
        }

        GlobalSubtitleState.IsAnySubtitlePlaying = false; // ★ グローバルロックOFF
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}