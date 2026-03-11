using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing; // ★PostProcessingを使うために追加

public class TVInteract : MonoBehaviour
{
    [Header("インタラクト設定")]
    [Tooltip("プレイヤーがこの中に入っている時だけEキーを押せます（+で複数登録可能）")]
    public Collider[] interactAreas;

    [Header("【オプション】UI設定")]
    [Tooltip("エリア内に入った時に表示する「Eキーで見る」などのUI（未設定でも動きます）")]
    public GameObject interactPromptUI;

    [Header("視点移動（ズーム）設定")]
    [Tooltip("プレイヤーのメインカメラ")]
    public Camera mainCamera;
    [Tooltip("ズーム後のカメラの位置・角度を示す空オブジェクト")]
    public Transform zoomTarget;
    [Tooltip("ズームにかかる時間（秒）")]
    public float zoomDuration = 1.0f;

    [Header("ビデオ設定")]
    [Tooltip("映像を再生するVideoPlayer")]
    public VideoPlayer videoPlayer;

    [Header("字幕設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float subtitleFadeDuration = 1.0f; // ★名前を被らないように変更
    public float delayBetweenSubtitles = 0.5f;
    [Tooltip("Eキーを押した直後に出る字幕")]
    public Image[] subtitleImages;

    // =========================================================
    // ★ここから合体したクイズの設定項目
    // =========================================================
    [Header("クイズUIとエフェクト設定")]
    [Tooltip("クイズのUI全体をまとめた親オブジェクト")]
    public GameObject quizUIContainer;

    [Tooltip("背景をぼかすPostProcessVolume（InventoryBlurVolumeを登録）")]
    public PostProcessVolume blurVolume;

    [Tooltip("問題文とボタンをまとめたグループ（空オブジェクト）")]
    public GameObject questionGroup;

    [Tooltip("解答の画像。必ず「Canvas Group」コンポーネントを付けてください")]
    public CanvasGroup answerCanvasGroup;

    [Header("クイズボタン設定")]
    [Tooltip("選択肢のボタン（＋ボタンを押して必要な数だけ登録できます）")]
    public Button[] answerButtons;

    [Header("クイズ時間設定")]
    [Tooltip("ボタンを押した後、解答を表示しておく時間（秒）")]
    public float answerDisplayTime = 2.0f;
    [Tooltip("解答とぼかしが徐々に消えていく時間（秒）")]
    public float quizFadeDuration = 1.0f; // ★名前を被らないように変更

    [Header("オブジェクトの非表示設定")]
    [Tooltip("クイズ中だけ非表示にしたいオブジェクトを登録してください（複数可）")]
    public GameObject[] objectsToHideDuringQuiz;

    [Header("参照")]
    public PlayerController playerController;

    // 内部変数
    private bool watching = false;
    private bool isTransitioning = false;
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private Transform originalCamParent;

    void Start()
    {
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        InitImages(subtitleImages);

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
        }

        // --- クイズ部分の初期化 ---
        if (quizUIContainer != null) quizUIContainer.SetActive(false);
        if (answerCanvasGroup != null)
        {
            answerCanvasGroup.gameObject.SetActive(false);
            answerCanvasGroup.alpha = 1f;
        }

        if (blurVolume != null) blurVolume.weight = 0f;

        if (answerButtons != null)
        {
            foreach (Button btn in answerButtons)
            {
                if (btn != null) btn.onClick.AddListener(OnAnswerClicked);
            }
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
        }
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
        if (isTransitioning) return;

        if (watching)
        {
            if (interactPromptUI != null && interactPromptUI.activeSelf) interactPromptUI.SetActive(false);

            if (Input.GetKeyDown(KeyCode.E))
            {
                // 手動でやめた場合はクイズを出さない(false)
                StartCoroutine(StopWatching(false));
            }
            return;
        }

        if (GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        bool isInArea = IsPlayerInAnyTrigger();

        if (interactPromptUI != null) interactPromptUI.SetActive(isInArea);

        if (isInArea)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (interactPromptUI != null) interactPromptUI.SetActive(false);
                StartCoroutine(StartWatching());
            }
        }
    }

    private bool IsPlayerInAnyTrigger()
    {
        if (playerController == null || interactAreas == null || interactAreas.Length == 0) return false;

        Collider playerCol = playerController.GetComponent<Collider>();

        foreach (var col in interactAreas)
        {
            if (col != null)
            {
                if (playerCol != null && col.bounds.Intersects(playerCol.bounds)) return true;
                else if (playerCol == null)
                {
                    Vector3 footPos = playerController.transform.position;
                    Vector3 bodyPos = footPos + (Vector3.up * 1.0f);
                    if (col.bounds.Contains(footPos) || col.bounds.Contains(bodyPos)) return true;
                }
            }
        }
        return false;
    }

    IEnumerator StartWatching()
    {
        isTransitioning = true;
        watching = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        originalCamPos = mainCamera.transform.localPosition;
        originalCamRot = mainCamera.transform.localRotation;
        originalCamParent = mainCamera.transform.parent;

        mainCamera.transform.SetParent(null);

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        StartCoroutine(ShowImagesRoutine(subtitleImages));

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
            mainCamera.transform.position = Vector3.Lerp(startPos, zoomTarget.position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, zoomTarget.rotation, t);
            yield return null;
        }

        mainCamera.transform.position = zoomTarget.position;
        mainCamera.transform.rotation = zoomTarget.rotation;

        if (videoPlayer != null) videoPlayer.Play();

        isTransitioning = false;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        if (watching && !isTransitioning)
        {
            // 最後まで見終わった場合はクイズを出す(true)
            StartCoroutine(StopWatching(true));
        }
    }

    IEnumerator StopWatching(bool showQuiz)
    {
        isTransitioning = true;

        if (videoPlayer != null) videoPlayer.Stop();

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            // 時間停止中でも動くように unscaledDeltaTime を使う
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);

            Vector3 targetWorldPos = originalCamParent.TransformPoint(originalCamPos);
            Quaternion targetWorldRot = originalCamParent.rotation * originalCamRot;

            mainCamera.transform.position = Vector3.Lerp(startPos, targetWorldPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetWorldRot, t);
            yield return null;
        }

        mainCamera.transform.SetParent(originalCamParent);
        mainCamera.transform.localPosition = originalCamPos;
        mainCamera.transform.localRotation = originalCamRot;

        watching = false;
        isTransitioning = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;

        // ★ 視点が戻ったらクイズを開始するか判定
        if (showQuiz && quizUIContainer != null)
        {
            StartQuiz();
        }
        else
        {
            if (playerController != null) playerController.canControl = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ==========================================
    // ★ここから下は合体したクイズの処理
    // ==========================================

    private void StartQuiz()
    {
        if (playerController != null) playerController.canControl = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f; // 時間を止める

        if (quizUIContainer != null) quizUIContainer.SetActive(true);
        if (questionGroup != null) questionGroup.SetActive(true);
        if (answerCanvasGroup != null) answerCanvasGroup.gameObject.SetActive(false);

        if (blurVolume != null)
        {
            blurVolume.gameObject.SetActive(true);
            blurVolume.weight = 1f;
        }

        if (objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in objectsToHideDuringQuiz)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    public void OnAnswerClicked()
    {
        StartCoroutine(AnswerSequence());
    }

    private IEnumerator AnswerSequence()
    {
        if (questionGroup != null) questionGroup.SetActive(false);
        if (answerCanvasGroup != null)
        {
            answerCanvasGroup.gameObject.SetActive(true);
            answerCanvasGroup.alpha = 1f;
        }

        // 時間が止まっているので Realtime で待つ
        yield return new WaitForSecondsRealtime(answerDisplayTime);

        float elapsed = 0f;
        while (elapsed < quizFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsed / quizFadeDuration);

            if (answerCanvasGroup != null) answerCanvasGroup.alpha = currentAlpha;
            if (blurVolume != null) blurVolume.weight = currentAlpha;

            yield return null;
        }

        if (answerCanvasGroup != null) answerCanvasGroup.alpha = 0f;
        if (blurVolume != null) blurVolume.weight = 0f;

        EndQuiz();
    }

    private void EndQuiz()
    {
        if (quizUIContainer != null) quizUIContainer.SetActive(false);

        if (objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in objectsToHideDuringQuiz)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        Time.timeScale = 1f;

        if (playerController != null)
        {
            playerController.canControl = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 完全に終わったら、このテレビを二度と調べられないようにスクリプトをOFFにする
        this.enabled = false;
    }

    // ==========================================
    // 字幕の処理
    // ==========================================
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
                    while (timer < subtitleFadeDuration)
                    {
                        timer += Time.deltaTime;
                        float alpha = Mathf.Lerp(1f, 0f, timer / subtitleFadeDuration);
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