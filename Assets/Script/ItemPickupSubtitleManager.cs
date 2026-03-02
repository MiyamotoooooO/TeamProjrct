using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemPickupSubtitleManager : MonoBehaviour
{
    [Header("トリガー設定")]
    [Tooltip("この名前のアイテムを拾ったら字幕を開始します（例：Detergent）")]
    public string targetItemName = "Detergent";

    [Header("表示設定")]
    [Tooltip("順番に表示させたいUIのImage画像（＋ボタンで何個でも登録できます）")]
    public Image[] targetImages;

    [Tooltip("1つの画像を表示にかける時間（秒）")]
    public float duration = 0.8f;

    [Tooltip("文字数（画像を何段階で表示するか）")]
    public int characterCount = 8;

    [Header("時間・フェード設定")]
    [Tooltip("すべて表示された後、消え始めるまでの待機時間（秒）")]
    public float displayTime = 1.0f;

    [Tooltip("最後の字幕がうっすら消えていくフェードアウトの時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("前の字幕が消えてから、次の字幕が表示されるまでの間隔（秒）")]
    public float delayBetweenSubtitles = 0.5f;

    [Header("参照設定")]
    [Tooltip("プレイヤーの操作を止めるために使用します")]
    public PlayerController playerController;

    [Tooltip("アイテムを取得したかを監視するために使用します")]
    public InventoryManager inventoryManager;

    [Tooltip("★アイテム入手演出が終わるのを待つために使用します")]
    public ItemGetDisplay itemGetDisplay;

    // 内部変数
    private bool hasTriggered = false;

    // リスポーン時などに二度と再生されないように、再生済みのアイテム名を記憶しておくリスト
    private static List<string> viewedSubtitleItems = new List<string>();

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (itemGetDisplay == null) itemGetDisplay = FindAnyObjectByType<ItemGetDisplay>();

        if (viewedSubtitleItems.Contains(targetItemName))
        {
            hasTriggered = true;
        }

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

    void Update()
    {
        // ★ 変更：他の字幕が再生中なら待機する
        if (GlobalSubtitleState.IsAnySubtitlePlaying) return;

        if (!hasTriggered && inventoryManager != null)
        {
            if (inventoryManager.currentItems.Contains(targetItemName))
            {
                hasTriggered = true;

                if (!viewedSubtitleItems.Contains(targetItemName))
                {
                    viewedSubtitleItems.Add(targetItemName);
                }

                StartCoroutine(PlayPickupSequence());
            }
        }
    }

    IEnumerator PlayPickupSequence()
    {
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // ★ グローバルロックON

        if (itemGetDisplay != null)
        {
            yield return new WaitWhile(() => itemGetDisplay.isDisplaying);
        }

        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        if (targetImages != null && targetImages.Length > 0)
        {
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
        }

        if (playerController != null)
        {
            playerController.canControl = true;
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