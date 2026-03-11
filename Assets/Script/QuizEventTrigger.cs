using System.Collections;
using System.Collections.Generic; // ★追加：Dictionaryを使うため
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class QuizEventTrigger : MonoBehaviour
{
    [Header("UIとエフェクト設定")]
    public GameObject quizUIContainer;
    public PostProcessVolume blurVolume;
    public GameObject questionGroup;
    public CanvasGroup answerCanvasGroup;

    [Header("ボタン設定")]
    public Button[] answerButtons;

    [Header("時間設定")]
    public float answerDisplayTime = 2.0f;
    public float fadeDuration = 1.0f;

    [Header("オブジェクトの非表示設定")]
    public GameObject[] objectsToHideDuringQuiz;

    private bool isEventStarted = false;
    private PlayerController playerController;

    // ★追加：元の音量を記憶しておくためのリスト
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        if (quizUIContainer != null) quizUIContainer.SetActive(false);
        if (answerCanvasGroup != null)
        {
            answerCanvasGroup.gameObject.SetActive(false);
            answerCanvasGroup.alpha = 1f;
        }
        if (blurVolume != null) blurVolume.weight = 0f;

        if (answerButtons != null)
        {
            foreach (Button btn in answerButtons) if (btn != null) btn.onClick.AddListener(OnAnswerClicked);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isEventStarted && other.CompareTag("Player")) StartEvent(other.gameObject);
    }

    private void StartEvent(GameObject playerObj)
    {
        isEventStarted = true;

        playerController = playerObj.GetComponent<PlayerController>();
        if (playerController != null) playerController.canControl = false;

        // ★変更：強力版のミュートを実行
        SetPlayerAudioMute(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

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
            foreach (GameObject obj in objectsToHideDuringQuiz) if (obj != null) obj.SetActive(false);
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

        yield return new WaitForSecondsRealtime(answerDisplayTime);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            if (answerCanvasGroup != null) answerCanvasGroup.alpha = currentAlpha;
            if (blurVolume != null) blurVolume.weight = currentAlpha;
            yield return null;
        }

        if (answerCanvasGroup != null) answerCanvasGroup.alpha = 0f;
        if (blurVolume != null) blurVolume.weight = 0f;

        EndEvent();
    }

    private void EndEvent()
    {
        if (quizUIContainer != null) quizUIContainer.SetActive(false);

        if (objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in objectsToHideDuringQuiz) if (obj != null) obj.SetActive(true);
        }

        Time.timeScale = 1f;

        // ★変更：強力版のミュート解除を実行
        SetPlayerAudioMute(false);

        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.UpdateCursorLock();
        }

        Destroy(gameObject);
    }

    // ===============================================
    // ★超強力版：プレイヤーの音を一時停止し、音量も0にする関数
    // ===============================================
    private void SetPlayerAudioMute(bool isMuted)
    {
        if (playerController != null)
        {
            // プレイヤーの中にある全ての音源を取得
            AudioSource[] audios = playerController.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource audio in audios)
            {
                if (isMuted)
                {
                    // 元の音量を記憶しておく
                    if (!originalVolumes.ContainsKey(audio))
                    {
                        originalVolumes[audio] = audio.volume;
                    }
                    audio.Pause();     // 強制的に一時停止
                    audio.volume = 0f; // 念のため音量もゼロに
                }
                else
                {
                    // 音量を元に戻して再生再開
                    if (originalVolumes.ContainsKey(audio))
                    {
                        audio.volume = originalVolumes[audio];
                    }
                    audio.UnPause();
                }
            }
        }
    }
}