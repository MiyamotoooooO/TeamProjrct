using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class FrogPickupEventManager : MonoBehaviour
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

    [Header("クイズUI設定")]
    [Tooltip("クイズの問題文とボタンが含まれる親パネル")]
    public GameObject quizPanel;
    [Tooltip("正解のボタン")]
    public Button correctButton;
    [Tooltip("不正解のボタン")]
    public Button wrongButton;

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

        // 画像の初期化
        InitImages(startSubtitleImages);

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
        // ★「いいえ」を押した時の処理
        if (quizPanel != null) quizPanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        // アイテムは取得せず、イベント状態だけを解除してプレイヤーを動かせるようにする
        LockPlayer(false);
        isEventActive = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    private void OnCorrectButtonClicked()
    {
        // ★「はい」を押した時の処理
        if (quizPanel != null) quizPanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        // アイテム取得シーケンスへ移行
        StartCoroutine(CorrectSequence());
    }


    // ==========================================
    // イベント：正解（はい）の場合
    // ==========================================
    IEnumerator CorrectSequence()
    {
        if (spiderItemObject != null && inventoryManager != null && playerController != null)
        {
            string cleanName = spiderItemObject.name.Replace("(Clone)", "").Trim();

            // ★ PickUpItem に実体を渡す（これでInventoryManagerがタグを確実に読める）
            inventoryManager.PickUpItem(spiderItemObject);

            // ★ PickUpItemの中でオブジェクトが破壊されてしまう場合は、これ以上処理できないので、
            // 破壊される前に手持ちモデルを更新させる
            playerController.UpdateItemModel();

            if (playerController.itemGetDisplay != null)
            {
                playerController.itemGetDisplay.ShowItemGet(cleanName);
                yield return new WaitWhile(() => playerController.itemGetDisplay.isDisplaying);
            }
        }

        // 完了処理（二度と調べられないようにする）
        hasCleared = true;
        if (triggerArea != null) triggerArea.enabled = false;

        // ★ カエルのオブジェクトがシーンに残っているなら、ここで非表示にする
        if (spiderItemObject != null)
        {
            spiderItemObject.SetActive(false);
        }

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