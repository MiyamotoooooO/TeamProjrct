using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class WindowSubtitleManager : MonoBehaviour
{
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
    private bool isAnimating = false; // ★ 追加

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

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

        if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        // ★ 変更：他の字幕が再生中なら、UIを隠してEキー入力も受け付けない
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
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // ★ グローバルロックON

        if (playerController != null)
        {
            playerController.canControl = false;
        }

        if (interactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactSound);
            yield return new WaitForSeconds(interactSound.length);
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

        isAnimating = false;
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