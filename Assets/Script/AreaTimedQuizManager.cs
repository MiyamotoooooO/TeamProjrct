using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
public class QuizData
{
    [Tooltip("クイズのUI全体をまとめた親オブジェクト")]
    public GameObject quizUIContainer;

    [Tooltip("問題文とボタンをまとめたグループ")]
    public GameObject questionGroup;

    [Tooltip("解答の画像(Canvas Group付き)")]
    public CanvasGroup answerCanvasGroup;

    [Tooltip("選択肢のボタン（複数登録可能）")]
    public Button[] answerButtons;

    [Header("★オブジェクトの非表示設定")]
    [Tooltip("このクイズ中だけ非表示にしたいオブジェクトを登録してください")]
    public GameObject[] objectsToHideDuringQuiz;
}

public class AreaTimedQuizManager : MonoBehaviour
{
    [Header("クイズのリスト")]
    [Tooltip("実行したい順番にクイズを登録してください")]
    public QuizData[] quizList;

    [Header("タイマー・エリア設定")]
    [Tooltip("次のクイズが発生するまでの時間（秒）。3分なら 180 に設定")]
    public float intervalTime = 180f;

    [Tooltip("プレイヤーがこのBoxCollider(複数可)のどれかの中にいる時だけタイマーが進みます")]
    public BoxCollider[] activeAreas;

    [Header("共通エフェクト設定")]
    [Tooltip("背景をぼかすPostProcessVolume（InventoryBlurVolumeを登録）")]
    public PostProcessVolume blurVolume;

    [Header("クイズ中の時間設定")]
    [Tooltip("ボタンを押した後、解答を表示しておく時間（秒）")]
    public float answerDisplayTime = 2.0f;
    [Tooltip("解答とぼかしが徐々に消えていく時間（秒）")]
    public float fadeDuration = 1.0f;

    // 内部変数
    private int currentQuizIndex = 0; // 現在何番目のクイズか
    private float timer = 0f;         // タイマー
    private bool isQuizActive = false; // クイズ実行中かどうか

    private PlayerController playerController;
    private WakeUpController wakeUpController; // ★追加：ベッドの管理スクリプト

    void Start()
    {
        // プレイヤーとベッドのスクリプトを自動取得
        playerController = FindAnyObjectByType<PlayerController>();
        wakeUpController = FindAnyObjectByType<WakeUpController>(); // ★追加

        // 登録されたすべてのクイズUIを初期化し、非表示にしておく
        foreach (var quiz in quizList)
        {
            if (quiz.quizUIContainer != null) quiz.quizUIContainer.SetActive(false);
            if (quiz.answerCanvasGroup != null)
            {
                quiz.answerCanvasGroup.gameObject.SetActive(false);
                quiz.answerCanvasGroup.alpha = 1f;
            }

            // 全てのボタンにクリック時のイベントを登録
            if (quiz.answerButtons != null)
            {
                foreach (Button btn in quiz.answerButtons)
                {
                    if (btn != null) btn.onClick.AddListener(OnAnswerClicked);
                }
            }
        }

        if (blurVolume != null) blurVolume.weight = 0f;
    }

    void Update()
    {
        // 全てのクイズが終わっている、またはクイズ実行中ならタイマーを進めない
        if (currentQuizIndex >= quizList.Length || isQuizActive) return;

        // ★追加：寝ている時、または起き上がっている最中はタイマーをストップする
        if (wakeUpController != null && (wakeUpController.isSleeping || wakeUpController.isWakingUp))
        {
            return;
        }

        // プレイヤーが指定エリアのどれかにいるか判定
        if (playerController != null && IsPlayerInAnyArea())
        {
            // エリア内にいる間だけタイマーを進める
            timer += Time.deltaTime;

            // 指定時間（3分等）に達したらクイズ開始！
            if (timer >= intervalTime)
            {
                StartQuiz();
            }
        }
    }

    // プレイヤーがいずれかのBoxCollider内にいるかを判定する関数
    private bool IsPlayerInAnyArea()
    {
        if (activeAreas == null || activeAreas.Length == 0) return false;

        foreach (var area in activeAreas)
        {
            // プレイヤーの現在位置がBoxColliderの範囲内にあるかチェック
            if (area != null && area.bounds.Contains(playerController.transform.position))
            {
                return true;
            }
        }
        return false;
    }

    // クイズ開始処理
    private void StartQuiz()
    {
        isQuizActive = true;
        timer = 0f; // 次のクイズのためにタイマーをリセット

        QuizData currentQuiz = quizList[currentQuizIndex];

        // 1. プレイヤーの操作と視点移動をロック
        if (playerController != null) playerController.canControl = false;

        // 2. マウスカーソルを表示
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. ゲームの世界の時間を止める
        Time.timeScale = 0f;

        // 4. 現在のクイズUIを表示
        if (currentQuiz.quizUIContainer != null) currentQuiz.quizUIContainer.SetActive(true);
        if (currentQuiz.questionGroup != null) currentQuiz.questionGroup.SetActive(true);
        if (currentQuiz.answerCanvasGroup != null) currentQuiz.answerCanvasGroup.gameObject.SetActive(false);

        // 5. 背景のぼかしをON
        if (blurVolume != null)
        {
            blurVolume.gameObject.SetActive(true);
            blurVolume.weight = 1f;
        }

        // ★追加：指定されたオブジェクトを非表示にする
        if (currentQuiz.objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in currentQuiz.objectsToHideDuringQuiz)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    // ボタンが押されたら呼ばれる
    public void OnAnswerClicked()
    {
        StartCoroutine(AnswerSequence());
    }

    private IEnumerator AnswerSequence()
    {
        QuizData currentQuiz = quizList[currentQuizIndex];

        // 1. 問題文とボタンを隠し、解答画像を表示
        if (currentQuiz.questionGroup != null) currentQuiz.questionGroup.SetActive(false);
        if (currentQuiz.answerCanvasGroup != null)
        {
            currentQuiz.answerCanvasGroup.gameObject.SetActive(true);
            currentQuiz.answerCanvasGroup.alpha = 1f;
        }

        // 2. 指定時間待機
        yield return new WaitForSecondsRealtime(answerDisplayTime);

        // 3. 解答画像と「ぼかし」を同時に徐々に消していく
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            // 時間停止中なので unscaledDeltaTime を使用
            elapsed += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            if (currentQuiz.answerCanvasGroup != null) currentQuiz.answerCanvasGroup.alpha = currentAlpha;
            if (blurVolume != null) blurVolume.weight = currentAlpha;

            yield return null;
        }

        if (currentQuiz.answerCanvasGroup != null) currentQuiz.answerCanvasGroup.alpha = 0f;
        if (blurVolume != null) blurVolume.weight = 0f;

        // 4. 終了処理へ
        EndQuiz();
    }

    private void EndQuiz()
    {
        QuizData currentQuiz = quizList[currentQuizIndex];

        // UIを完全に消す
        if (currentQuiz.quizUIContainer != null) currentQuiz.quizUIContainer.SetActive(false);

        // ★追加：非表示にしていたオブジェクトを元に戻す
        if (currentQuiz.objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in currentQuiz.objectsToHideDuringQuiz)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // 止めていた時間を元に戻す
        Time.timeScale = 1f;

        // プレイヤーの操作を再開
        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.UpdateCursorLock();
        }

        // 次のクイズへ進める
        currentQuizIndex++;
        isQuizActive = false;
    }
}