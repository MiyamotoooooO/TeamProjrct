using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(AudioSource))]
public class WaterQuizEventManager : MonoBehaviour
{
    [Header("トリガー設定")]
    [Tooltip("イベントを覆うBoxCollider（IsTriggerをオンにしてください）")]
    public Collider triggerArea;
    [Tooltip("エリア内に入った時に表示する「Eキー」などの案内UI")]
    public GameObject interactPromptUI;

    [Header("必須アイテム設定")]
    [Tooltip("このイベントを開始するために『手に装備している』必要があるアイテム（rust_keyなどをアタッチ）")]
    public GameObject requiredItem; // ★追加：必須アイテムの設定枠

    [Header("字幕：時間・フェード共通設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("字幕データ（順番に表示）")]
    [Tooltip("Eキーを押した直後に出る字幕")]
    public Image[] startSubtitleImages;
    [Tooltip("画面が明るくなった後に出る字幕")]
    public Image[] correctSubtitleImages2;

    [Header("クイズUI設定")]
    [Tooltip("クイズの問題文とボタンが含まれる親パネル")]
    public GameObject quizPanel;
    [Tooltip("正解のボタン")]
    public Button correctButton;
    [Tooltip("不正解のボタン")]
    public Button wrongButton;

    [Header("暗転設定")]
    [Tooltip("画面を真っ暗にするためのPostProcessVolume（Weightを操作します）")]
    public PostProcessVolume blackFadeVolume;
    [Tooltip("暗転にかかる時間（秒）")]
    public float blackFadeDuration = 1.5f;

    [Header("音声設定（暗転中）")]
    [Tooltip("音を鳴らすためのAudioSource（自動で取得されます）")]
    public AudioSource audioSource;
    [Tooltip("最初に鳴る音（最後に鳴る音と同じ）")]
    public AudioClip sound1;
    [Tooltip("真ん中で鳴る音")]
    public AudioClip sound2;
    [Tooltip("音2を通常の音量で流し続ける時間（秒）")]
    public float sound2PlayTime = 3.0f;
    [Tooltip("音2をフェードアウトさせて無音にするまでの時間（秒）")]
    public float sound2FadeTime = 2.0f;

    [Header("アイテム入手設定")]
    [Tooltip("入手させる実体オブジェクト（シーン上のもの。非表示でOK）")]
    public GameObject itemObject;

    [Header("参照設定")]
    public PlayerController playerController;
    public InventoryManager inventoryManager;

    // 内部変数
    private bool isPlayerInRange = false;
    private bool hasCleared = false; // クリア済みか
    private bool isEventActive = false; // イベント進行中か

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);

        // 暗転用のVolumeは最初はWeight=0にしておく
        if (blackFadeVolume != null) blackFadeVolume.weight = 0f;

        // 画像の初期化
        InitImages(startSubtitleImages);
        InitImages(correctSubtitleImages2);

        // ボタンのクリックイベント登録
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

        // 他の字幕が再生中なら入力を受け付けない
        if (GlobalSubtitleState.IsAnySubtitlePlaying && !isEventActive)
        {
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        if (isPlayerInRange && !isEventActive && !GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            // ★ 指定したアイテムを現在手に持っているかチェック
            if (IsHoldingRequiredItem())
            {
                if (interactPromptUI != null && !interactPromptUI.activeSelf)
                    interactPromptUI.SetActive(true);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (interactPromptUI != null) interactPromptUI.SetActive(false);
                    StartCoroutine(StartQuizEvent());
                }
            }
            else
            {
                // 手に持っていない時はEキー案内を隠す
                if (interactPromptUI != null && interactPromptUI.activeSelf)
                    interactPromptUI.SetActive(false);
            }
        }
        else if (!isPlayerInRange && interactPromptUI != null && interactPromptUI.activeSelf)
        {
            interactPromptUI.SetActive(false);
        }
    }

    // ★追加：手に指定のアイテムを持っているかを判定する関数
    private bool IsHoldingRequiredItem()
    {
        // 必須アイテムがInspectorで設定されていない場合は、条件なし（誰でも押せる）とする
        if (requiredItem == null) return true;
        if (inventoryManager == null) return false;

        string reqName = requiredItem.name.Replace("(Clone)", "").Trim();

        int targetIndex = inventoryManager.equippedIndex;
        if (targetIndex >= 0 && targetIndex < inventoryManager.currentItems.Count)
        {
            // 現在選択しているスロットのアイテム名を取得
            string itemName = inventoryManager.currentItems[targetIndex];

            // 手に持っているアイテム名と必須アイテム名が一致するか
            if (itemName == reqName)
            {
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasCleared && other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    // ==========================================
    // イベント：クイズ開始
    // ==========================================
    IEnumerator StartQuizEvent()
    {
        isEventActive = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        LockPlayer(true);

        // 1. 開始時の字幕を表示
        yield return StartCoroutine(ShowImagesRoutine(startSubtitleImages));

        // 2. クイズUIを表示してカーソルを表示する
        if (quizPanel != null) quizPanel.SetActive(true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        // ※ ここでコルーチンは一時終了し、ボタンが押されるのを待つ
    }

    // ==========================================
    // ボタンクリック時の処理
    // ==========================================
    private void OnWrongButtonClicked()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        StartCoroutine(WrongSequence());
    }

    private void OnCorrectButtonClicked()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        StartCoroutine(CorrectSequence());
    }

    // ==========================================
    // イベント：不正解の場合
    // ==========================================
    IEnumerator WrongSequence()
    {
        // 不正解時の字幕は削除されたため、そのままイベントを終了させる
        LockPlayer(false);
        isEventActive = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
        yield return null;
    }

    // ==========================================
    // イベント：正解の場合
    // ==========================================
    IEnumerator CorrectSequence()
    {
        // 1. 画面を徐々に暗転させる
        if (blackFadeVolume != null)
        {
            float elapsed = 0f;
            while (elapsed < blackFadeDuration)
            {
                elapsed += Time.deltaTime;
                blackFadeVolume.weight = Mathf.Lerp(0f, 1f, elapsed / blackFadeDuration);
                yield return null;
            }
            blackFadeVolume.weight = 1f;
        }

        // 2. 音声の再生シーケンス
        if (audioSource != null)
        {
            // ① 1つ目の音を再生して、終わるまで待機
            if (sound1 != null)
            {
                audioSource.clip = sound1;
                audioSource.volume = 1f;
                audioSource.Play();
                yield return new WaitForSeconds(sound1.length);
            }

            // ② 2つ目の音を再生して、指定時間（デフォルト3秒）待機
            if (sound2 != null)
            {
                audioSource.clip = sound2;
                audioSource.loop = true;
                audioSource.volume = 1f;
                audioSource.Play();

                yield return new WaitForSeconds(sound2PlayTime);

                // ③ 2つ目の音を徐々にフェードアウト
                float fadeTimer = 0f;
                float startVol = audioSource.volume;
                while (fadeTimer < sound2FadeTime)
                {
                    fadeTimer += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(startVol, 0f, fadeTimer / sound2FadeTime);
                    yield return null;
                }

                audioSource.Stop();
                audioSource.volume = 1f;
                audioSource.loop = false;
            }

            // ④ 再度、1つ目の音を再生して、終わるまで待機
            if (sound1 != null)
            {
                audioSource.clip = sound1;
                audioSource.volume = 1f;
                audioSource.Play();
                yield return new WaitForSeconds(sound1.length);
            }
        }

        // 3. 画面を徐々に明転させる
        if (blackFadeVolume != null)
        {
            float elapsed = 0f;
            while (elapsed < blackFadeDuration)
            {
                elapsed += Time.deltaTime;
                blackFadeVolume.weight = Mathf.Lerp(1f, 0f, elapsed / blackFadeDuration);
                yield return null;
            }
            blackFadeVolume.weight = 0f;
        }

        // 4. 正解時の字幕（暗転後）を表示
        yield return StartCoroutine(ShowImagesRoutine(correctSubtitleImages2));

        // 5. アイテムを入手する処理
        if (itemObject != null && inventoryManager != null && playerController != null)
        {
            string cleanName = itemObject.name.Replace("(Clone)", "").Trim();
            inventoryManager.PickUpItem(itemObject);
            playerController.UpdateItemModel();

            // 入手演出を表示
            if (playerController.itemGetDisplay != null)
            {
                playerController.itemGetDisplay.ShowItemGet(cleanName);

                // 演出が終わるまで待つ
                yield return new WaitWhile(() => playerController.itemGetDisplay.isDisplaying);
            }
        }

        // 6. 完了処理
        hasCleared = true;
        if (triggerArea != null) triggerArea.enabled = false;

        LockPlayer(false);
        isEventActive = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    // ==========================================
    // 補助機能
    // ==========================================
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
        }
    }

    // 画像表示部分の共通コルーチン
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

                if (i < images.Length - 1)
                {
                    yield return new WaitForSeconds(delayBetweenSubtitles);
                }
            }
        }
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