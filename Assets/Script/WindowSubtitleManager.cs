using System.Collections;
using System.Collections.Generic; // ★追加
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WindowSubtitleData
{
    public Image subtitleImage;
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
}

[RequireComponent(typeof(AudioSource))]
public class WindowSubtitleManager : MonoBehaviour
{
    [Header("字幕表示設定")]
    public WindowSubtitleData[] subtitles;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("インタラクト・音設定")]
    public GameObject interactPromptUI;
    public AudioClip interactSound;

    [Header("参照設定")]
    public PlayerController playerController;

    private bool hasTriggered = false;
    private bool isPlayerInRange = false;
    private AudioSource audioSource;
    private bool isAnimating = false;

    // ★追加：元の音量を記憶するリスト
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

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
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    void Update()
    {
        if (GlobalSubtitleState.IsAnySubtitlePlaying && !isAnimating)
        {
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        if (isPlayerInRange && !hasTriggered && !isAnimating && !GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            if (interactPromptUI != null && !interactPromptUI.activeSelf) interactPromptUI.SetActive(true);

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
        if (!hasTriggered && other.CompareTag("Player")) isPlayerInRange = true;
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
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        if (playerController != null)
        {
            playerController.canControl = false;
            playerController.canLock = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // ★強力版ミュート実行
        SetPlayerAudioMute(true);

        if (interactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(interactSound);
            yield return new WaitForSeconds(interactSound.length);
        }

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
                while (timer < currentData.duration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / currentData.duration;
                    if (currentData.characterCount > 0) currentImage.fillAmount = Mathf.Floor(progress * currentData.characterCount) / currentData.characterCount;
                    else currentImage.fillAmount = progress;
                    yield return null;
                }

                currentImage.fillAmount = 1.0f;
                yield return new WaitForSeconds(currentData.displayTime);

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
                if (i < subtitles.Length - 1) yield return new WaitForSeconds(delayBetweenSubtitles);
            }
        }

        // ★強力版ミュート解除
        SetPlayerAudioMute(false);

        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.canLock = true;
        }
        isAnimating = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img != null) { Color c = img.color; c.a = alpha; img.color = c; }
    }

    // ★超強力版ミュート関数
    private void SetPlayerAudioMute(bool isMuted)
    {
        if (playerController != null)
        {
            AudioSource[] audios = playerController.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource audio in audios)
            {
                if (isMuted)
                {
                    if (!originalVolumes.ContainsKey(audio)) originalVolumes[audio] = audio.volume;
                    audio.Pause();
                    audio.volume = 0f;
                }
                else
                {
                    if (originalVolumes.ContainsKey(audio)) audio.volume = originalVolumes[audio];
                    audio.UnPause();
                }
            }
        }
    }
}