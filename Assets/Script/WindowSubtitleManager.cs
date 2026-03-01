using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))] // ★音を鳴らすために自動追加されます
public class WindowSubtitleManager : MonoBehaviour
{
    [Header("表示設定")]
    [Tooltip("順番に表示させたいUIのImage画像（＋ボタンで何個でも登録できます）")]
    public Image[] targetImages;

    [Tooltip("1つの画像を表示にかける時間（秒）")]
    public float duration = 2.0f;

    [Tooltip("文字数（画像を何段階で表示するか）")]
    public int characterCount = 8;

    [Header("時間・フェード設定")]
    [Tooltip("すべて表示された後、消え始めるまでの待機時間（秒）")]
    public float displayTime = 3.0f;

    [Tooltip("最後の字幕がうっすら消えていくフェードアウトの時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("前の字幕が消えてから、次の字幕が表示されるまでの間隔（秒）")]
    public float delayBetweenSubtitles = 0.5f;

    [Header("インタラクト・音設定")]
    [Tooltip("エリア内に入った時に表示する「Eキー」などの案内UIオブジェクト")]
    public GameObject interactPromptUI;

    [Tooltip("調べる（Eキー）を押した直後に鳴る効果音")]
    public AudioClip interactSound;

    [Header("参照設定")]
    [Tooltip("プレイヤーの操作を止めるために使用します")]
    public PlayerController playerController;

    // 内部変数
    private bool hasTriggered = false;    // すでにイベントが発動したか
    private bool isPlayerInRange = false; // プレイヤーがBoxCollider内にいるか
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        // 初期化：登録されたすべての画像を隠す
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

        // 案内UI（Eキー画像など）を最初は隠しておく
        if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        // プレイヤーが範囲内にいて、まだイベントが始まっておらず、Eキーが押されたら
        if (isPlayerInRange && !hasTriggered && Input.GetKeyDown(KeyCode.E))
        {
            hasTriggered = true; // 二度と発動しないようにロック

            // 案内表示を消す
            if (interactPromptUI != null) interactPromptUI.SetActive(false);

            // 字幕シーケンスを開始！
            StartCoroutine(PlayWindowSequence());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (interactPromptUI != null) interactPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
        }
    }

    // 音の再生と、字幕表示を管理するコルーチン
    IEnumerator PlayWindowSequence()
    {
        // ★ 1. プレイヤーの操作を止める
        if (playerController != null)
        {
            playerController.canControl = false;
        }

        // ★ 2. 効果音を鳴らし、鳴り終わるまで待機する
        if (interactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactSound);
            // 音の長さ（秒）だけ待つ
            yield return new WaitForSeconds(interactSound.length);
        }

        // --- ここから字幕の表示 ---
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

                // タイプライター風に徐々に表示
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

                // 指定した時間だけ表示したまま待機
                yield return new WaitForSeconds(displayTime);

                // ★ 3. 最後の字幕かどうかの判定
                if (i == targetImages.Length - 1)
                {
                    // 最後の字幕なら、うっすらフェードアウトさせる
                    timer = 0f;
                    while (timer < fadeDuration)
                    {
                        timer += Time.deltaTime;
                        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                        SetAlpha(currentImage, alpha);
                        yield return null;
                    }
                }
                else
                {
                    // 途中の字幕ならフェードアウトせず、すぐに消える（パッと切り替わる）
                    // ※ ここは待ち時間なしで瞬時に消します
                }

                // 完全に消して非表示にする
                SetAlpha(currentImage, 0f);
                currentImage.gameObject.SetActive(false);

                // 次の字幕へ行く前に指定した時間待つ（最後の場合は待たない）
                if (i < targetImages.Length - 1)
                {
                    yield return new WaitForSeconds(delayBetweenSubtitles);
                }
            }
        }

        // ★ 4. 全ての表示が終わったら、プレイヤーの操作を再び有効にする
        if (playerController != null)
        {
            playerController.canControl = true;
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