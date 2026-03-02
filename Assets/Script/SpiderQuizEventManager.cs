using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class SpiderQuizEventManager : MonoBehaviour
{
    [Header("トリガー設定")]
    [Tooltip("虫の山を覆うBoxCollider（IsTriggerをオンにしてください）")]
    public Collider triggerArea;
    [Tooltip("エリア内に入った時に表示する「Eキー」などの案内UI")]
    public GameObject interactPromptUI;

    [Header("字幕：時間・フェード共通設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("字幕データ（順番に表示）")]
    [Tooltip("Eキーを押した直後に出る字幕")]
    public Image[] startSubtitleImages;
    [Tooltip("不正解の時に出る字幕")]
    public Image[] wrongSubtitleImages;
    [Tooltip("正解した時（暗転前）に出る字幕")]
    public Image[] correctSubtitleImages1;
    [Tooltip("画面が明るくなった後に出る字幕")]
    public Image[] correctSubtitleImages2;

    [Header("クイズUI設定")]
    [Tooltip("クイズの問題文とボタンが含まれる親パネル")]
    public GameObject quizPanel;
    [Tooltip("正解のボタン")]
    public Button correctButton;
    [Tooltip("不正解のボタン")]
    public Button wrongButton;

    [Header("オブジェクト入れ替え・暗転設定")]
    [Tooltip("画面を真っ暗にするためのPostProcessVolume（Weightを操作します）")]
    public PostProcessVolume blackFadeVolume;
    [Tooltip("暗転にかかる時間（秒）")]
    public float blackFadeDuration = 1.5f;
    [Tooltip("真っ暗な状態を維持する時間（秒）")]
    public float blackWaitTime = 2.0f;

    [Tooltip("元からある綺麗に並んだクモの山（非表示にする対象）")]
    public GameObject originalSpiders;
    [Tooltip("バラバラになったクモ（表示する対象）")]
    public GameObject scatteredSpiders;

    [Header("アイテム入手設定")]
    [Tooltip("入手させる「Spider」の実体オブジェクト（シーン上のもの。非表示でOK）")]
    public GameObject spiderItemObject;

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

        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);

        // バラバラのクモは最初は隠しておく
        if (scatteredSpiders != null) scatteredSpiders.SetActive(false);

        // 暗転用のVolumeは最初はWeight=0にしておく
        if (blackFadeVolume != null) blackFadeVolume.weight = 0f;

        // 画像の初期化
        InitImages(startSubtitleImages);
        InitImages(wrongSubtitleImages);
        InitImages(correctSubtitleImages1);
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
            if (interactPromptUI != null && !interactPromptUI.activeSelf)
                interactPromptUI.SetActive(true);

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
        // 1. 不正解時の字幕を表示
        yield return StartCoroutine(ShowImagesRoutine(wrongSubtitleImages));

        // 終了処理（もう一度調べられるようにフラグを戻す）
        LockPlayer(false);
        isEventActive = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    // ==========================================
    // イベント：正解の場合
    // ==========================================
    IEnumerator CorrectSequence()
    {
        // 1. 正解時の字幕（暗転前）を表示
        yield return StartCoroutine(ShowImagesRoutine(correctSubtitleImages1));

        // 2. 画面を徐々に暗転させる（PostProcessVolumeのWeightを上げる）
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

        // 3. 真っ暗な間にクモを入れ替える
        if (originalSpiders != null) originalSpiders.SetActive(false);
        if (scatteredSpiders != null) scatteredSpiders.SetActive(true);

        // そのまま少し待機
        yield return new WaitForSeconds(blackWaitTime);

        // 4. 画面を徐々に明転させる（Weightを下げる）
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

        // 5. 正解時の字幕（暗転後）を表示
        yield return StartCoroutine(ShowImagesRoutine(correctSubtitleImages2));

        // 6. アイテム（Spider）を入手する処理
        if (spiderItemObject != null && inventoryManager != null && playerController != null)
        {
            string cleanName = spiderItemObject.name.Replace("(Clone)", "").Trim();
            inventoryManager.PickUpItem(spiderItemObject);
            playerController.UpdateItemModel();

            // 入手演出を表示
            if (playerController.itemGetDisplay != null)
            {
                playerController.itemGetDisplay.ShowItemGet(cleanName);

                // 演出が終わるまで待つ
                yield return new WaitWhile(() => playerController.itemGetDisplay.isDisplaying);
            }
        }

        // 7. 完了処理（二度と調べられないようにする）
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