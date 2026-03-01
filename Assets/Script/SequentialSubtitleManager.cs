using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SequentialSubtitleManager : MonoBehaviour
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

    [Tooltip("うっすら消えていくフェードアウトの時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("前の字幕が消えてから、次の字幕が表示されるまでの間隔（秒）")]
    public float delayBetweenSubtitles = 1.0f;

    [Header("インタラクト設定")]
    [Tooltip("エリア内に入った時に表示する「Eキー」などの案内UIオブジェクト")]
    public GameObject interactPromptUI;

    // 内部変数
    private bool hasTriggered = false;    // すでに字幕イベントが発動したか
    private bool isPlayerInRange = false; // プレイヤーがBoxCollider内にいるか

    void Start()
    {
        // 初期化：登録されたすべての画像を隠す
        if (targetImages != null)
        {
            foreach (Image img in targetImages)
            {
                if (img != null)
                {
                    // Image設定を強制的にFilledにする
                    img.type = Image.Type.Filled;
                    img.fillMethod = Image.FillMethod.Horizontal;
                    img.fillOrigin = (int)Image.OriginHorizontal.Left;
                    img.fillAmount = 0f;
                    img.gameObject.SetActive(false); // 最初は非表示

                    // 色の透明度を100%に初期化しておく
                    SetAlpha(img, 1f);
                }
            }
        }

        // 案内UI（Eキー画像など）を最初は隠しておく
        if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        // プレイヤーが範囲内にいて、まだ字幕イベントが始まっておらず、Eキーが押されたら
        if (isPlayerInRange && !hasTriggered && Input.GetKeyDown(KeyCode.E))
        {
            hasTriggered = true; // 二度と発動しないようにロックする

            // Eキーの案内表示を消す
            if (interactPromptUI != null)
            {
                interactPromptUI.SetActive(false);
            }

            // 字幕シーケンスを開始！
            StartCoroutine(PlaySequentialTypewriter());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーが範囲に入り、かつまだイベントが終わっていないならプロンプトを表示
        if (!hasTriggered && other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (interactPromptUI != null)
            {
                interactPromptUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // プレイヤーが範囲から出たらプロンプトを隠す
        if (!hasTriggered && other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactPromptUI != null)
            {
                interactPromptUI.SetActive(false);
            }
        }
    }

    // 複数の画像を順番に表示するコルーチン
    IEnumerator PlaySequentialTypewriter()
    {
        if (targetImages == null || targetImages.Length == 0) yield break;

        for (int i = 0; i < targetImages.Length; i++)
        {
            Image currentImage = targetImages[i];
            if (currentImage == null) continue;

            // 次の画像を表示するための準備
            currentImage.gameObject.SetActive(true);
            currentImage.fillAmount = 0f;
            SetAlpha(currentImage, 1f);

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
                    currentImage.fillAmount = steppedProgress;
                }
                else
                {
                    // 滑らか表示
                    currentImage.fillAmount = progress;
                }
                yield return null;
            }

            // 完全に表示完了
            currentImage.fillAmount = 1.0f;

            // 2. 指定した時間だけ表示したまま待機する
            yield return new WaitForSeconds(displayTime);

            // 3. うっすら消えていく（フェードアウト）
            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                SetAlpha(currentImage, alpha);
                yield return null;
            }

            // 完全に消して、オブジェクトを非アクティブにする
            SetAlpha(currentImage, 0f);
            currentImage.gameObject.SetActive(false);

            // 4. 次の字幕へ行く前に指定した時間（1秒）待つ
            // ※最後の字幕が終わった後は待つ必要がないので判定を入れています
            if (i < targetImages.Length - 1)
            {
                yield return new WaitForSeconds(delayBetweenSubtitles);
            }
        }
    }

    // 指定したImageのAlpha（透明度）を設定する補助関数
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