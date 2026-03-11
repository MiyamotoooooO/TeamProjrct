using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ★ 新しく作った「字幕1つ1つのデータをまとめる箱」
[System.Serializable]
public class WindowSubtitleData
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

[RequireComponent(typeof(AudioSource))]
public class WindowSubtitleManager : MonoBehaviour
{
    [Header("字幕表示設定")]
    [Tooltip("順番に表示させたい字幕の設定（＋ボタンで何個でも登録できます）")]
    public WindowSubtitleData[] subtitles; // ★ 画像だけでなく色々な設定をまとめた配列に変更

    [Header("フェード・待機設定（全体共通）")]
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
    private bool hasTriggered = false;
    private bool isPlayerInRange = false;
    private AudioSource audioSource;
    private bool isAnimating = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

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

        if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        // 他の字幕が再生中なら、UIを隠してEキー入力も受け付けない
        if (GlobalSubtitleState.IsAnySubtitlePlaying && !isAnimating)
        {
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        // 範囲内にいて、グローバルでもローカルでもアニメーション中でない場合
        if (isPlayerInRange && !hasTriggered && !isAnimating && !GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            if (interactPromptUI != null && !interactPromptUI.activeSelf)
            {
                interactPromptUI.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                hasTriggered = true;
                if (interactPromptUI != null) interactPromptUI.SetActive(false);
                StartCoroutine(PlayWindowSequence());
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            isPlayerInRange = true;
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

    IEnumerator PlayWindowSequence()
    {
        isAnimating = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // グローバルロックON

        // プレイヤーの操作と視点をロック
        if (playerController != null)
        {
            playerController.canControl = false;
            playerController.canLock = false; // ★ 視点もロックしておく
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        if (interactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactSound);
            yield return new WaitForSeconds(interactSound.length);
        }

        // 字幕表示ループ
        if (subtitles != null && subtitles.Length > 0)
        {
            for (int i = 0; i < subtitles.Length; i++)
            {
                WindowSubtitleData currentData = subtitles[i];
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

        // 全て終わったら操作ロック解除
        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.canLock = true;
        }

        isAnimating = false;
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