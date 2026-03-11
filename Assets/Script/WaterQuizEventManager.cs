using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
public class WaterSubtitleData
{
    public Image subtitleImage;
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
}

[RequireComponent(typeof(AudioSource))]
public class WaterQuizEventManager : MonoBehaviour
{
    [Header("トリガー設定")]
    public Collider triggerArea;
    public GameObject interactPromptUI;

    [Header("必須アイテム設定")]
    public GameObject requiredItem;

    [Header("字幕データ")]
    public WaterSubtitleData[] startSubtitles;
    public WaterSubtitleData[] correctSubtitles2;

    [Header("全体共通設定")]
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("クイズUI設定")]
    public GameObject quizPanel;
    public Button correctButton;
    public Button wrongButton;

    [Header("暗転設定")]
    public PostProcessVolume blackFadeVolume;
    public float blackFadeDuration = 1.5f;

    [Header("音声設定")]
    public AudioSource audioSource;
    public AudioClip sound1;
    public AudioClip sound2;
    public float sound2PlayTime = 3.0f;
    public float sound2FadeTime = 2.0f;

    [Header("アイテム入手/削除設定")]
    public GameObject itemObject;
    [Tooltip("【重要】名前が違っても消せるように、現在手に持っているアイテムを優先的に消します")]
    public GameObject rustKeyToRemove;

    [Header("参照設定")]
    public PlayerController playerController;
    public InventoryManager inventoryManager;

    private bool isPlayerInRange = false;
    private bool hasCleared = false;
    private bool isEventActive = false;
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);
        if (blackFadeVolume != null) blackFadeVolume.weight = 0f;
        InitSubtitles(startSubtitles);
        InitSubtitles(correctSubtitles2);
        if (correctButton != null) correctButton.onClick.AddListener(OnCorrectButtonClicked);
        if (wrongButton != null) wrongButton.onClick.AddListener(OnWrongButtonClicked);
    }

    private void InitSubtitles(WaterSubtitleData[] subs)
    {
        if (subs == null) return;
        foreach (var data in subs) if (data.subtitleImage != null) { data.subtitleImage.type = Image.Type.Filled; data.subtitleImage.fillMethod = Image.FillMethod.Horizontal; data.subtitleImage.fillAmount = 0f; data.subtitleImage.gameObject.SetActive(false); SetAlpha(data.subtitleImage, 1f); }
    }

    void Update()
    {
        if (hasCleared) return;
        if (GlobalSubtitleState.IsAnySubtitlePlaying && !isEventActive) { if (interactPromptUI != null) interactPromptUI.SetActive(false); return; }
        if (isPlayerInRange && !isEventActive && !GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            if (IsHoldingRequiredItem())
            {
                if (interactPromptUI != null && !interactPromptUI.activeSelf) interactPromptUI.SetActive(true);
                if (Input.GetKeyDown(KeyCode.E)) { if (interactPromptUI != null) interactPromptUI.SetActive(false); StartCoroutine(StartQuizEvent()); }
            }
            else if (interactPromptUI != null) interactPromptUI.SetActive(false);
        }
        else if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    private bool IsHoldingRequiredItem()
    {
        if (requiredItem == null) return true;
        if (inventoryManager == null) return false;
        string reqName = requiredItem.name.Replace("(Clone)", "").Trim();
        int targetIndex = inventoryManager.equippedIndex;
        if (targetIndex >= 0 && targetIndex < inventoryManager.currentItems.Count)
        {
            // カッコやスペースがあっても判定できるように Contains を使用
            return inventoryManager.currentItems[targetIndex].Contains(reqName);
        }
        return false;
    }

    private void OnTriggerEnter(Collider other) { if (!hasCleared && other.CompareTag("Player")) isPlayerInRange = true; }
    private void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) isPlayerInRange = false; }

    IEnumerator StartQuizEvent()
    {
        isEventActive = true; GlobalSubtitleState.IsAnySubtitlePlaying = true;
        Time.timeScale = 0f;
        LockPlayer(true);
        yield return StartCoroutine(ShowSubRoutine(startSubtitles));
        if (quizPanel != null) quizPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    private void OnWrongButtonClicked() { if (quizPanel != null) quizPanel.SetActive(false); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; StartCoroutine(WrongSequence()); }
    private void OnCorrectButtonClicked() { if (quizPanel != null) quizPanel.SetActive(false); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; StartCoroutine(CorrectSequence()); }

    IEnumerator WrongSequence() { Time.timeScale = 1f; LockPlayer(false); isEventActive = false; GlobalSubtitleState.IsAnySubtitlePlaying = false; yield return null; }

    IEnumerator CorrectSequence()
    {
        // 1. 暗転
        if (blackFadeVolume != null)
        {
            float elapsed = 0f;
            while (elapsed < blackFadeDuration) { elapsed += Time.unscaledDeltaTime; blackFadeVolume.weight = Mathf.Lerp(0f, 1f, elapsed / blackFadeDuration); yield return null; }
            blackFadeVolume.weight = 1f;
        }

        // 2. 音声（中略）
        if (audioSource != null)
        {
            if (sound1 != null) { audioSource.clip = sound1; audioSource.volume = 1f; audioSource.Play(); yield return new WaitForSecondsRealtime(sound1.length); }
            if (sound2 != null)
            {
                audioSource.clip = sound2; audioSource.loop = true; audioSource.volume = 1f; audioSource.Play();
                yield return new WaitForSecondsRealtime(sound2PlayTime);
                float fade = 0f; while (fade < sound2FadeTime) { fade += Time.unscaledDeltaTime; audioSource.volume = Mathf.Lerp(1f, 0f, fade / sound2FadeTime); yield return null; }
                audioSource.Stop(); audioSource.volume = 1f; audioSource.loop = false;
            }
            if (sound1 != null) { audioSource.clip = sound1; audioSource.volume = 1f; audioSource.Play(); yield return new WaitForSecondsRealtime(sound1.length); }
        }

        // 3. 明転
        if (blackFadeVolume != null)
        {
            float elapsed = 0f;
            while (elapsed < blackFadeDuration) { elapsed += Time.unscaledDeltaTime; blackFadeVolume.weight = Mathf.Lerp(1f, 0f, elapsed / blackFadeDuration); yield return null; }
            blackFadeVolume.weight = 0f;
        }

        // ==========================================
        // ★修正：絶対に消去するロジック
        // ==========================================
        if (inventoryManager != null)
        {
            // 方法A：今手に持っているスロットを「問答無用で空」にする（最も確実）
            int currentIndex = inventoryManager.equippedIndex;
            string itemInHand = inventoryManager.currentItems[currentIndex];

            Debug.Log($"[System] 現在の手持ちアイテム: {itemInHand} を削除します。");
            inventoryManager.currentItems[currentIndex] = "";

            // 方法B：万が一のために、インベントリ全体から rust_key (1) 系統を掃除する
            if (rustKeyToRemove != null)
            {
                string targetName = rustKeyToRemove.name.Replace("(Clone)", "").Trim();
                for (int i = 0; i < inventoryManager.currentItems.Count; i++)
                {
                    if (inventoryManager.currentItems[i].Contains(targetName))
                    {
                        inventoryManager.currentItems[i] = "";
                    }
                }
            }

            // 画面をリフレッシュ（これでインベントリ画面からも消える）
            inventoryManager.UpdateInventoryUI();
            inventoryManager.UpdateHUDSlotSelection();
            if (playerController != null) playerController.UpdateItemModel();
        }
        // ==========================================

        yield return StartCoroutine(ShowSubRoutine(correctSubtitles2));

        if (itemObject != null && inventoryManager != null)
        {
            string cleanName = itemObject.name.Replace("(Clone)", "").Trim();
            inventoryManager.PickUpItem(itemObject);
            playerController.UpdateItemModel();
            if (playerController.itemGetDisplay != null)
            {
                playerController.itemGetDisplay.ShowItemGet(cleanName);
                yield return new WaitWhile(() => playerController.itemGetDisplay.isDisplaying);
            }
        }

        hasCleared = true; triggerArea.enabled = false;
        Time.timeScale = 1f; LockPlayer(false); isEventActive = false; GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    private void LockPlayer(bool isLocked)
    {
        if (playerController != null)
        {
            playerController.canControl = !isLocked; playerController.canLock = !isLocked;
            if (isLocked) { Rigidbody rb = playerController.GetComponent<Rigidbody>(); if (rb != null) rb.velocity = Vector3.zero; }
            SetPlayerAudioMute(isLocked);
        }
    }

    private void SetPlayerAudioMute(bool isMuted)
    {
        if (playerController != null)
        {
            AudioSource[] audios = playerController.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource a in audios)
            {
                if (isMuted) { if (!originalVolumes.ContainsKey(a)) originalVolumes[a] = a.volume; a.Pause(); a.volume = 0f; }
                else { if (originalVolumes.ContainsKey(a)) a.volume = originalVolumes[a]; a.UnPause(); }
            }
        }
    }

    IEnumerator ShowSubRoutine(WaterSubtitleData[] subs)
    {
        if (subs == null) yield break;
        foreach (var d in subs)
        {
            Image img = d.subtitleImage; if (img == null) continue;
            img.gameObject.SetActive(true); img.fillAmount = 0f; SetAlpha(img, 1f);
            float t = 0f; while (t < d.duration) { t += Time.unscaledDeltaTime; img.fillAmount = (d.characterCount > 0) ? Mathf.Floor(t / d.duration * d.characterCount) / d.characterCount : t / d.duration; yield return null; }
            img.fillAmount = 1f; yield return new WaitForSecondsRealtime(d.displayTime);
            img.gameObject.SetActive(false);
        }
    }
    private void SetAlpha(Image img, float a) { if (img != null) { Color c = img.color; c.a = a; img.color = c; } }
}