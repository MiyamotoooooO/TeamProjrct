using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ItemConsumeEventManager : MonoBehaviour
{
    [Header("アイテム消費設定")]
    [Tooltip("このアイテムを持っている時だけイベントが発動し、消費されます（アイテムのプレハブ等をアタッチしてください）")]
    public GameObject targetItem;

    [Header("字幕：時間・フェード共通設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("字幕データ")]
    [Tooltip("アイテムを消費した時に順番に表示する画像（＋ボタンで追加）")]
    public Image[] subtitleImages;

    [Header("参照設定")]
    public PlayerController playerController;

    // 内部変数
    private bool hasTriggered = false;

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        // 画像の初期化
        if (subtitleImages != null)
        {
            foreach (Image img in subtitleImages)
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
        // プレイヤーが触れて、かつまだイベントが起きていない場合
        if (!hasTriggered && other.CompareTag("Player"))
        {
            // 他の字幕が再生中なら無視
            if (GlobalSubtitleState.IsAnySubtitlePlaying) return;

            if (playerController != null && playerController.inventoryManager != null && targetItem != null)
            {
                // ★ アタッチされたオブジェクトの名前を取得（Cloneなどの余計な文字があれば消す）
                string itemName = targetItem.name.Replace("(Clone)", "").Trim();

                // インベントリにその名前のアイテムを持っているかチェック
                if (playerController.inventoryManager.HasItem(itemName))
                {
                    hasTriggered = true; // イベント発動フラグを立てる

                    // アイテムをインベントリから削除し、UIや手持ちモデルを更新
                    playerController.inventoryManager.RemoveItem(itemName);
                    playerController.inventoryManager.UpdateInventoryUI();
                    playerController.UpdateItemModel();

                    // 字幕イベント開始
                    StartCoroutine(PlayConsumeEvent());
                }
            }
            else if (targetItem == null)
            {
                Debug.LogWarning("ItemConsumeEventManager: Target Item がアタッチされていません！");
            }
        }
    }

    IEnumerator PlayConsumeEvent()
    {
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // グローバルロックON

        // ★ 時間を止めて敵もプレイヤーも動けなくする
        Time.timeScale = 0f;
        if (playerController != null)
        {
            playerController.canControl = false;
            // 念のため速度をゼロに
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // --- 字幕の表示処理 ---
        if (subtitleImages != null && subtitleImages.Length > 0)
        {
            for (int i = 0; i < subtitleImages.Length; i++)
            {
                Image currentImage = subtitleImages[i];
                if (currentImage == null) continue;

                currentImage.gameObject.SetActive(true);
                currentImage.fillAmount = 0f;
                SetAlpha(currentImage, 1f);

                float timer = 0f;

                // 時間が止まっているので Time.unscaledDeltaTime を使う
                while (timer < duration)
                {
                    timer += Time.unscaledDeltaTime;
                    float progress = timer / duration;

                    if (characterCount > 0)
                    {
                        currentImage.fillAmount = Mathf.Floor(progress * characterCount) / characterCount;
                    }
                    else
                    {
                        currentImage.fillAmount = progress;
                    }
                    yield return null;
                }

                currentImage.fillAmount = 1.0f;

                // 時間が止まっているので WaitForSecondsRealtime を使う
                yield return new WaitForSecondsRealtime(displayTime);

                // 最後の字幕だけフェードアウト
                if (i == subtitleImages.Length - 1)
                {
                    timer = 0f;
                    while (timer < fadeDuration)
                    {
                        timer += Time.unscaledDeltaTime;
                        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                        SetAlpha(currentImage, alpha);
                        yield return null;
                    }
                }

                SetAlpha(currentImage, 0f);
                currentImage.gameObject.SetActive(false);

                if (i < subtitleImages.Length - 1)
                {
                    yield return new WaitForSecondsRealtime(delayBetweenSubtitles);
                }
            }
        }

        // --- イベント終了処理 ---
        // ★ 時間を元に戻して敵も動けるようにする
        Time.timeScale = 1f;

        if (playerController != null)
        {
            playerController.canControl = true;
        }

        // このトリガーを二度と踏めないように無効化する
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

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