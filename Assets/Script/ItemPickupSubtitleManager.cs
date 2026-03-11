using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ★ 新しく作った「字幕1つ1つのデータをまとめる箱」
[System.Serializable]
public class PickupSubtitleData
{
    [Tooltip("表示するUIのImage画像")]
    public Image subtitleImage;

    [Tooltip("この字幕が全部出るまでにかかる時間（秒）")]
    public float duration = 0.8f;

    [Tooltip("文字数（何段階で表示するか。0なら滑らか）")]
    public int characterCount = 8;

    [Tooltip("文字が全部出た後に表示したままにする時間（秒）")]
    public float displayTime = 1.0f;
}

public class ItemPickupSubtitleManager : MonoBehaviour
{
    [Header("トリガー設定")]
    [Tooltip("この名前のアイテムを拾ったら字幕を開始します（例：Detergent）")]
    public string targetItemName = "Detergent";

    [Header("字幕表示設定")]
    [Tooltip("順番に表示させたい字幕の設定（＋ボタンで何個でも登録できます）")]
    public PickupSubtitleData[] subtitles; // ★ 画像だけでなく色々な設定をまとめた配列に変更

    [Header("フェード・待機設定（全体共通）")]
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

        // 画像群の初期化
        if (subtitles != null)
        {
            foreach (var data in subtitles)
            {
                if (data.subtitleImage != null)
                {
                    data.subtitleImage.type = Image.Type.Filled;
                    data.subtitleImage.fillMethod = Image.FillMethod.Horizontal;
                    data.subtitleImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                    data.subtitleImage.fillAmount = 0f;
                    data.subtitleImage.gameObject.SetActive(false);
                    SetAlpha(data.subtitleImage, 1f);
                }
            }
        }
    }

    void Update()
    {
        // 他の字幕が再生中なら待機する
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
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // グローバルロックON

        // 拾った瞬間に、移動も視点も完全にロックする！
        if (playerController != null)
        {
            playerController.canControl = false; // 移動ロック
            playerController.canLock = false;    // 視点（カメラ）移動も完全にロック

            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // アイテム入手演出（クルクル回るUIなど）が終わるのを待つ
        if (itemGetDisplay != null)
        {
            yield return new WaitWhile(() => itemGetDisplay.isDisplaying);
        }

        // 演出が終わったら字幕表示ループ開始
        if (subtitles != null && subtitles.Length > 0)
        {
            for (int i = 0; i < subtitles.Length; i++)
            {
                PickupSubtitleData currentData = subtitles[i];
                Image currentImage = currentData.subtitleImage;

                if (currentImage == null) continue;

                currentImage.gameObject.SetActive(true);
                currentImage.fillAmount = 0f;
                SetAlpha(currentImage, 1f);

                float timer = 0f;

                // 1. タイプライター表示
                while (timer < currentData.duration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / currentData.duration;

                    if (currentData.characterCount > 0)
                    {
                        float steppedProgress = Mathf.Floor(progress * currentData.characterCount) / currentData.characterCount;
                        currentImage.fillAmount = steppedProgress;
                    }
                    else
                    {
                        currentImage.fillAmount = progress;
                    }
                    yield return null;
                }

                currentImage.fillAmount = 1.0f;

                // 2. 表示キープ
                yield return new WaitForSeconds(currentData.displayTime);

                // 3. 最後の字幕だけフェードアウト
                if (i == subtitles.Length - 1)
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

                // 4. 次の字幕への待機
                if (i < subtitles.Length - 1)
                {
                    yield return new WaitForSeconds(delayBetweenSubtitles);
                }
            }
        }

        // 字幕がすべて終わったら、プレイヤーの移動と視点を再び解き放つ！
        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.canLock = true;
        }

        GlobalSubtitleState.IsAnySubtitlePlaying = false; // グローバルロックOFF
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