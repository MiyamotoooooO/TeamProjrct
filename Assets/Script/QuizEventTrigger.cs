using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing; // ★PostProcessingを使うために追加

public class QuizEventTrigger : MonoBehaviour
{
    [Header("UIとエフェクト設定")]
    [Tooltip("クイズのUI全体をまとめた親オブジェクト（ボタンや画像をまとめた空オブジェクト等）")]
    public GameObject quizUIContainer;

    [Tooltip("★背景をぼかすPostProcessVolume（InventoryBlurVolumeを登録）")]
    public PostProcessVolume blurVolume;

    [Tooltip("問題文とボタンをまとめたグループ（空オブジェクト）")]
    public GameObject questionGroup;

    [Tooltip("解答の画像。必ず「Canvas Group」コンポーネントを付けてください")]
    public CanvasGroup answerCanvasGroup;

    [Header("ボタン設定")]
    [Tooltip("選択肢のボタン（＋ボタンを押して必要な数だけ登録できます）")]
    public Button[] answerButtons;

    [Header("時間設定")]
    [Tooltip("ボタンを押した後、解答を表示しておく時間（秒）")]
    public float answerDisplayTime = 2.0f;
    [Tooltip("解答とぼかしが徐々に消えていく時間（秒）")]
    public float fadeDuration = 1.0f;

    [Header("★オブジェクトの非表示設定")]
    [Tooltip("クイズ中だけ非表示にしたいオブジェクトを登録してください（複数可）")]
    public GameObject[] objectsToHideDuringQuiz;

    private bool isEventStarted = false;
    private PlayerController playerController;

    void Start()
    {
        // 最初はUIを非表示にしておく
        if (quizUIContainer != null) quizUIContainer.SetActive(false);
        if (answerCanvasGroup != null)
        {
            answerCanvasGroup.gameObject.SetActive(false);
            answerCanvasGroup.alpha = 1f; // 透明度を100%にしておく
        }

        // ★最初はぼかしを 0 (なし) にしておく
        if (blurVolume != null)
        {
            blurVolume.weight = 0f;
        }

        // 登録されたすべてのボタンに同じ関数（OnAnswerClicked）を割り当てる
        if (answerButtons != null)
        {
            foreach (Button btn in answerButtons)
            {
                if (btn != null)
                {
                    btn.onClick.AddListener(OnAnswerClicked);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // プレイヤーが触れたら1回だけイベント開始
        if (!isEventStarted && other.CompareTag("Player"))
        {
            StartEvent(other.gameObject);
        }
    }

    private void StartEvent(GameObject playerObj)
    {
        isEventStarted = true;

        // 1. プレイヤーの操作と視点移動をロックする
        playerController = playerObj.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.canControl = false;
        }

        // 2. マウスカーソルを表示してクリックできるようにする
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. ゲームの世界の時間を止める（ゾンビもピタッと止まります）
        Time.timeScale = 0f;

        // 4. クイズUIを表示する
        if (quizUIContainer != null) quizUIContainer.SetActive(true);
        if (questionGroup != null) questionGroup.SetActive(true);
        if (answerCanvasGroup != null) answerCanvasGroup.gameObject.SetActive(false);

        // 5. ★背景のぼかしをONにしてWeightを1にする
        if (blurVolume != null)
        {
            blurVolume.gameObject.SetActive(true); // オブジェクト自体がOFFならONにする
            blurVolume.weight = 1f;
        }

        // ★追加：指定されたオブジェクトを非表示にする
        if (objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in objectsToHideDuringQuiz)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    // ボタンが押されたら呼ばれる関数
    public void OnAnswerClicked()
    {
        StartCoroutine(AnswerSequence());
    }

    private IEnumerator AnswerSequence()
    {
        // 1. 問題文とボタンを隠し、解答画像を表示する
        if (questionGroup != null) questionGroup.SetActive(false);
        if (answerCanvasGroup != null)
        {
            answerCanvasGroup.gameObject.SetActive(true);
            answerCanvasGroup.alpha = 1f;
        }

        // 2. 指定した時間だけ待つ
        yield return new WaitForSecondsRealtime(answerDisplayTime);

        // 3. ★解答画像と「ぼかし(Weight)」を同時に徐々に消していく
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            // 解答の透明度を下げる
            if (answerCanvasGroup != null) answerCanvasGroup.alpha = currentAlpha;

            // ぼかしのWeightを下げる
            if (blurVolume != null) blurVolume.weight = currentAlpha;

            yield return null;
        }

        // 完全に0にする
        if (answerCanvasGroup != null) answerCanvasGroup.alpha = 0f;
        if (blurVolume != null) blurVolume.weight = 0f;

        // 4. イベント終了処理へ
        EndEvent();
    }

    private void EndEvent()
    {
        // UIを完全に消す
        if (quizUIContainer != null) quizUIContainer.SetActive(false);

        // ★追加：非表示にしていたオブジェクトを元に戻す
        if (objectsToHideDuringQuiz != null)
        {
            foreach (GameObject obj in objectsToHideDuringQuiz)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // 止めていた時間を元に戻す
        Time.timeScale = 1f;

        // プレイヤーの操作と視点移動を再開する
        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.UpdateCursorLock(); // カーソルを再び隠す
        }

        // このトリガー（透明な壁）を消滅させ、二度とイベントが起きないようにする
        Destroy(gameObject);
    }
}