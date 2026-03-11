using System.Collections;
using System.Collections.Generic; // ★追加
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
public class QuizData
{
    public GameObject quizUIContainer;
    public GameObject questionGroup;
    public CanvasGroup answerCanvasGroup;
    public Button[] answerButtons;
    public GameObject[] objectsToHideDuringQuiz;
}

public class AreaTimedQuizManager : MonoBehaviour
{
    public QuizData[] quizList;
    public float intervalTime = 180f;
    public BoxCollider[] activeAreas;
    public PostProcessVolume blurVolume;
    public float answerDisplayTime = 2.0f;
    public float fadeDuration = 1.0f;

    private int currentQuizIndex = 0;
    private float timer = 0f;
    private bool isQuizActive = false;

    private PlayerController playerController;
    private WakeUpController wakeUpController;

    // ★追加：元の音量を記憶しておくためのリスト
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        wakeUpController = FindAnyObjectByType<WakeUpController>();

        foreach (var quiz in quizList)
        {
            if (quiz.quizUIContainer != null) quiz.quizUIContainer.SetActive(false);
            if (quiz.answerCanvasGroup != null)
            {
                quiz.answerCanvasGroup.gameObject.SetActive(false);
                quiz.answerCanvasGroup.alpha = 1f;
            }

            if (quiz.answerButtons != null)
            {
                foreach (Button btn in quiz.answerButtons) if (btn != null) btn.onClick.AddListener(OnAnswerClicked);
            }
        }

        if (blurVolume != null) blurVolume.weight = 0f;
    }

    void Update()
    {
        if (currentQuizIndex >= quizList.Length || isQuizActive) return;
        if (wakeUpController != null && (wakeUpController.isSleeping || wakeUpController.isWakingUp)) return;

        if (playerController != null && IsPlayerInAnyArea())
        {
            timer += Time.deltaTime;
            if (timer >= intervalTime) StartQuiz();
        }
    }

    private bool IsPlayerInAnyArea()
    {
        if (activeAreas == null || activeAreas.Length == 0) return false;
        foreach (var area in activeAreas)
        {
            if (area != null && area.bounds.Contains(playerController.transform.position)) return true;
        }
        return false;
    }

    private void StartQuiz()
    {
        isQuizActive = true;
        timer = 0f;

        QuizData currentQuiz = quizList[currentQuizIndex];

        if (playerController != null) playerController.canControl = false;

        // ★強力版ミュートを実行
        SetPlayerAudioMute(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        if (currentQuiz.quizUIContainer != null) currentQuiz.quizUIContainer.SetActive(true);
        if (currentQuiz.questionGroup != null) currentQuiz.questionGroup.SetActive(true);
        if (currentQuiz.answerCanvasGroup != null) currentQuiz.answerCanvasGroup.gameObject.SetActive(false);

        if (blurVolume != null)
        {
            blurVolume.gameObject.SetActive(true);
            blurVolume.weight = 1f;
        }

        if (currentQuiz.objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in currentQuiz.objectsToHideDuringQuiz) if (obj != null) obj.SetActive(false);
        }
    }

    public void OnAnswerClicked()
    {
        StartCoroutine(AnswerSequence());
    }

    private IEnumerator AnswerSequence()
    {
        QuizData currentQuiz = quizList[currentQuizIndex];

        if (currentQuiz.questionGroup != null) currentQuiz.questionGroup.SetActive(false);
        if (currentQuiz.answerCanvasGroup != null)
        {
            currentQuiz.answerCanvasGroup.gameObject.SetActive(true);
            currentQuiz.answerCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(answerDisplayTime);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            if (currentQuiz.answerCanvasGroup != null) currentQuiz.answerCanvasGroup.alpha = currentAlpha;
            if (blurVolume != null) blurVolume.weight = currentAlpha;
            yield return null;
        }

        if (currentQuiz.answerCanvasGroup != null) currentQuiz.answerCanvasGroup.alpha = 0f;
        if (blurVolume != null) blurVolume.weight = 0f;

        EndQuiz();
    }

    private void EndQuiz()
    {
        QuizData currentQuiz = quizList[currentQuizIndex];

        if (currentQuiz.quizUIContainer != null) currentQuiz.quizUIContainer.SetActive(false);

        if (currentQuiz.objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in currentQuiz.objectsToHideDuringQuiz) if (obj != null) obj.SetActive(true);
        }

        Time.timeScale = 1f;

        // ★強力版ミュートを解除
        SetPlayerAudioMute(false);

        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.UpdateCursorLock();
        }

        currentQuizIndex++;
        isQuizActive = false;
    }

    // ===============================================
    // ★超強力版：プレイヤーの音を一時停止し、音量も0にする関数
    // ===============================================
    private void SetPlayerAudioMute(bool isMuted)
    {
        if (playerController != null)
        {
            AudioSource[] audios = playerController.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource audio in audios)
            {
                if (isMuted)
                {
                    if (!originalVolumes.ContainsKey(audio))
                    {
                        originalVolumes[audio] = audio.volume;
                    }
                    audio.Pause();
                    audio.volume = 0f;
                }
                else
                {
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