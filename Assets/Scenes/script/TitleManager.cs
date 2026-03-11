using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI; // ★追加：Buttonをスクリプトからいじるため
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("ゲームシーンの名前")]
    [Tooltip("遷移先のゲームシーンの名前を正確に入力してください")]
    public string gameSceneName = "Kodoku";

    [Header("UI設定")]
    [Tooltip("「セーブデータがありません」のテキストオブジェクト")]
    public TMP_Text noSaveDataText;

    [Tooltip("消える時に下に落ちる距離")]
    public float dropDistance = 50f;

    // ★追加：ボタンを一時的に無効化するための枠
    [Header("ボタン設定")]
    [Tooltip("LOAD GAMEのボタン本体（Buttonコンポーネントが付いている親オブジェクト）")]
    public Button loadGameButton;

    // 連打された時に演出をリセットするための変数
    private Coroutine warningCoroutine = null;
    private Vector3 originalPos;

    void Start()
    {
        // 警告テキストを最初は確実に隠す
        if (noSaveDataText != null)
        {
            noSaveDataText.gameObject.SetActive(false);
            // 最初の正しい位置を記憶しておく
            originalPos = noSaveDataText.GetComponent<RectTransform>().anchoredPosition;
        }

        // タイトル画面ではマウスカーソルを表示して自由に動かせるようにする
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ===================================
    // START GAME ボタンを押した時の処理
    // ===================================
    public void OnClickStartGame()
    {
        PlayerPrefs.SetInt("IsLoadGame", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    // ===================================
    // LOAD GAME ボタンを押した時の処理
    // ===================================
    public void OnClickLoadGame()
    {
        // セーブデータが存在するかチェック（SaveManager経由が確実）
        if (PlayerPrefs.GetInt("HasSaveData", 0) == 1)
        {
            // ★ ロードフラグを立てる（1 = ロード、0 = ニューゲーム）
            PlayerPrefs.SetInt("IsLoadGame", 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // セーブデータがない場合の警告演出
            if (warningCoroutine != null) StopCoroutine(warningCoroutine);
            warningCoroutine = StartCoroutine(ShowNoSaveText());
        }
    }

    // 警告メッセージを表示して、落ちながら消えるコルーチン
    private IEnumerator ShowNoSaveText()
    {
        if (noSaveDataText == null) yield break;

        // ★追加：演出が始まった瞬間にボタンを押せなくする（灰色になります）
        if (loadGameButton != null)
        {
            loadGameButton.interactable = false;
        }

        RectTransform rectTransform = noSaveDataText.GetComponent<RectTransform>();

        // 位置と透明度を100%（完全表示）にリセット
        rectTransform.anchoredPosition = originalPos;
        Color color = noSaveDataText.color;
        color.a = 1f;
        noSaveDataText.color = color;

        // 画面に表示！
        noSaveDataText.gameObject.SetActive(true);

        // そのまま2秒間待機
        yield return new WaitForSeconds(2.0f);

        // 1秒かけて下に落ちながらうっすら消える
        float fadeDuration = 1.5f;
        float timer = 0f;
        Vector3 targetPos = originalPos + new Vector3(0, -dropDistance, 0);

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            // 透明度を1から0に下げる
            color.a = Mathf.Lerp(1f, 0f, t);
            noSaveDataText.color = color;

            // 位置を下にずらす
            rectTransform.anchoredPosition = Vector3.Lerp(originalPos, targetPos, t);

            yield return null;
        }

        // 完全に消えたら非表示にする
        noSaveDataText.gameObject.SetActive(false);

        // ★追加：完全に消え終わったら、ボタンを再び押せるように復活させる
        if (loadGameButton != null)
        {
            loadGameButton.interactable = true;
        }

        warningCoroutine = null;
    }
}