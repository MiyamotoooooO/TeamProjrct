using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FrogPickupEventManager : MonoBehaviour
{
    [Header("トリガー設定")]
    [Tooltip("カエル自身、またはカエル周辺を覆うBoxCollider（IsTriggerをオン）")]
    public Collider triggerArea;
    [Tooltip("エリア内に入った時に表示する「Eキー」などの案内UI")]
    public GameObject interactPromptUI;

    [Header("字幕：時間・フェード共通設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;

    [Header("字幕データ（順番に表示）")]
    [Tooltip("Eキーを押した直後に出る字幕（「拾いますか？」など）")]
    public Image[] startSubtitleImages;

    [Header("選択肢UI設定")]
    [Tooltip("選択肢のボタンが含まれる親パネル")]
    public GameObject choicePanel;
    [Tooltip("「はい」のボタン")]
    public Button yesButton;
    [Tooltip("「いいえ」のボタン")]
    public Button noButton;

    [Header("参照設定")]
    public PlayerController playerController;

    // 内部変数
    private bool isPlayerInRange = false;
    private bool hasCleared = false; // 拾い終わったか
    private bool isEventActive = false; // イベント進行中か

    // リスポーン時などに二度と再生されないようにするリスト
    public static List<string> clearedFrogEvents = new List<string>();

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        // すでに拾い終わっているなら機能ごとオフ（自身を消去）
        if (clearedFrogEvents.Contains(gameObject.name))
        {
            hasCleared = true;
            gameObject.SetActive(false);
            return;
        }

        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);

        // 画像の初期化
        InitImages(startSubtitleImages);

        // ボタンのクリックイベント登録
        if (yesButton != null) yesButton.onClick.AddListener(OnYesButtonClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoButtonClicked);
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
                StartCoroutine(StartChoiceEvent());
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
    // イベント：選択肢開始
    // ==========================================
    IEnumerator StartChoiceEvent()
    {
        isEventActive = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // グローバルロックON

        LockPlayer(true);

        // 1. 開始時の字幕を表示
        yield return StartCoroutine(ShowImagesRoutine(startSubtitleImages));

        // 2. 選択肢UIを表示してカーソルを表示する
        if (choicePanel != null) choicePanel.SetActive(true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        // ※ ここでコルーチンは一時終了し、ボタンが押されるのを待つ
    }

    // ==========================================
    // ボタンクリック時の処理
    // ==========================================
    private void OnNoButtonClicked()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        // 何もせずに終わる
        LockPlayer(false);
        isEventActive = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    private void OnYesButtonClicked()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        StartCoroutine(YesSequence());
    }

    // ==========================================
    // イベント：「はい」を押した場合
    // ==========================================
    IEnumerator YesSequence()
    {
        // 1. ★ PlayerControllerのCheckPickUpと同じ処理を行う ★
        if (playerController != null && playerController.inventoryManager != null)
        {
            // このスクリプトがついているオブジェクト自体の名前を取得（Clone等を消す）
            string cleanName = gameObject.name.Replace("(Clone)", "").Trim();

            // このオブジェクト自体をインベントリに拾わせる！
            playerController.inventoryManager.PickUpItem(gameObject);
            playerController.UpdateItemModel();

            // 入手演出を表示
            if (playerController.itemGetDisplay != null)
            {
                playerController.itemGetDisplay.ShowItemGet(cleanName);

                // クルクル回る演出が終わるまで待つ
                yield return new WaitWhile(() => playerController.itemGetDisplay.isDisplaying);
            }
        }

        // 2. 完了処理（二度と調べられないようにする）
        hasCleared = true;

        // 死んでも復活しないように歴史に刻む
        if (!clearedFrogEvents.Contains(gameObject.name))
        {
            clearedFrogEvents.Add(gameObject.name);
        }

        LockPlayer(false);
        isEventActive = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false; // グローバルロックOFF

        // （PickUpItem の中で Destroy(gameObject) されるため、このオブジェクト自体が消滅します）
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