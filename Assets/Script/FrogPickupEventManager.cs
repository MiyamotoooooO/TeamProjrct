using System.Collections;
using System.Collections.Generic; // ★追加
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class FrogPickupEventManager : MonoBehaviour
{
    public Collider triggerArea;
    public GameObject interactPromptUI;

    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    public Image[] startSubtitleImages;
    public GameObject quizPanel;
    public Button correctButton;
    public Button wrongButton;
    public GameObject spiderItemObject;

    public PlayerController playerController;
    public InventoryManager inventoryManager;

    private bool isPlayerInRange = false;
    private bool hasCleared = false;
    private bool isEventActive = false;

    // ★追加：元の音量を記憶するリスト
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();

        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);

        InitImages(startSubtitleImages);

        if (correctButton != null) correctButton.onClick.AddListener(OnCorrectButtonClicked);
        if (wrongButton != null) wrongButton.onClick.AddListener(OnWrongButtonClicked);
    }

    private void InitImages(Image[] images)
    {
        if (images == null) return;
        foreach (Image img in images)
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

    void Update()
    {
        if (hasCleared) return;

        if (GlobalSubtitleState.IsAnySubtitlePlaying && !isEventActive)
        {
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        if (isPlayerInRange && !isEventActive && !GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            if (interactPromptUI != null && !interactPromptUI.activeSelf) interactPromptUI.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (interactPromptUI != null) interactPromptUI.SetActive(false);
                StartCoroutine(StartQuizEvent());
            }
        }
        else if (!isPlayerInRange && interactPromptUI != null && interactPromptUI.activeSelf)
        {
            interactPromptUI.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasCleared && other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = false;
    }

    IEnumerator StartQuizEvent()
    {
        isEventActive = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        LockPlayer(true);

        yield return StartCoroutine(ShowImagesRoutine(startSubtitleImages));

        if (quizPanel != null) quizPanel.SetActive(true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void OnWrongButtonClicked()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        LockPlayer(false);
        isEventActive = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    private void OnCorrectButtonClicked()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        StartCoroutine(CorrectSequence());
    }

    IEnumerator CorrectSequence()
    {
        if (spiderItemObject != null && inventoryManager != null && playerController != null)
        {
            string cleanName = spiderItemObject.name.Replace("(Clone)", "").Trim();
            inventoryManager.PickUpItem(spiderItemObject);
            playerController.UpdateItemModel();

            if (playerController.itemGetDisplay != null)
            {
                playerController.itemGetDisplay.ShowItemGet(cleanName);
                yield return new WaitWhile(() => playerController.itemGetDisplay.isDisplaying);
            }
        }

        hasCleared = true;
        if (triggerArea != null) triggerArea.enabled = false;
        if (spiderItemObject != null) spiderItemObject.SetActive(false);

        LockPlayer(false);
        isEventActive = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    private void LockPlayer(bool isLocked)
    {
        if (playerController != null)
        {
            playerController.canControl = !isLocked;
            if (isLocked)
            {
                Rigidbody rb = playerController.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = Vector3.zero;
            }

            // ★音を消す/戻す
            SetPlayerAudioMute(isLocked);
        }
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

    IEnumerator ShowImagesRoutine(Image[] images)
    {
        if (images != null && images.Length > 0)
        {
            for (int i = 0; i < images.Length; i++)
            {
                Image currentImage = images[i];
                if (currentImage == null) continue;

                currentImage.gameObject.SetActive(true);
                currentImage.fillAmount = 0f;
                SetAlpha(currentImage, 1f);

                float timer = 0f;
                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / duration;
                    if (characterCount > 0) currentImage.fillAmount = Mathf.Floor(progress * characterCount) / characterCount;
                    else currentImage.fillAmount = progress;
                    yield return null;
                }

                currentImage.fillAmount = 1.0f;
                yield return new WaitForSeconds(displayTime);

                if (i == images.Length - 1)
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
                if (i < images.Length - 1) yield return new WaitForSeconds(delayBetweenSubtitles);
            }
        }
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img != null) { Color c = img.color; c.a = alpha; img.color = c; }
    }
}