using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PasswordDoor : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("正解のパスワード（4文字の数字など）")]
    public string correctPassword = "1234";

    [Header("揺れ演出の設定")]
    [Tooltip("揺れる時間（秒）")]
    public float shakeDuration = 0.5f;

    [Tooltip("揺れる強さ（範囲）")]
    public float shakeMagnitude = 10f;

    [Header("割り当て")]
    [Tooltip("消えるドア本体（自分自身なら空欄でOK）")]
    public GameObject doorObject;

    [Tooltip("プレイヤーのオブジェクト（距離測定用）")]
    public Transform playerTransform;

    [Tooltip("「スペースで入力」と表示するテキスト")]
    public GameObject promptText;

    [Tooltip("パスワード入力画面のパネル")]
    public GameObject passwordPanel;

    [Tooltip("文字を入力するInputField")]
    public TMP_InputField inputField;

    [Header("閉じるボタンを登録する場所")]
    public Button closeButton;

    [Header("音の設定")]
    [Tooltip("正解したときの音")]
    public AudioClip successSound;

    [Tooltip("間違えたときの音")]
    public AudioClip errorSound;

    // private
    private bool isPlayerInside = false;
    private bool isUiActive = false;
    private MonoBehaviour playerScript; // プレイヤーの動きを止める用
    private bool isShaking = false;
    private AudioSource audioSource; // スピーカー

    void Start()
    {
        // ドア本体が設定されてなければ自分自身をセット
        if (doorObject == null) doorObject = this.gameObject;

        // パネルとテキストを初期状態で非表示に
        if (passwordPanel != null) passwordPanel.SetActive(false);
        if (promptText != null) promptText.SetActive(false);

        // Closeボタンが押されたら閉じる機能を自動でつける
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePasswordUI);
        }

        // プレイヤーを自動検索（タグがPlayerの場合）
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                Debug.Log("プレイヤーを見つけました!: " + p.name);
            }
            else
            {
                Debug.LogError("プレイヤーが見つかりません!");
            }
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // もしついてなかったら自動でつける（親切設計）
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 入力完了時（Enterキーなど）に CheckPassword を呼ぶ設定
        inputField.onSubmit.AddListener(CheckPassword);
    }

    void OnCollisionEnter(Collision collision)
    {
        // ぶつかってきたのが「Player」タグのついた相手なら
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerInside = true;

            // UIが開いていないならヒントを表示
            if (!isUiActive && promptText != null)
            {
                promptText.SetActive(true);
                Debug.Log("エリアに入りました");
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Playerが出ていったら
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerInside = false;

            // ヒントを消す
            if (promptText != null) promptText.SetActive(false);

            // もし入力画面を開いたまま離れたら、閉じる
            if (isUiActive) ClosePasswordUI();

            Debug.Log("エリアから出ました");
        }
    }

    public void OnNumberPressed(string number)
    {
        // 揺れている間は入力を受け付けない
        if (isShaking || inputField == null) return;
        inputField.text += number;
    }

    // クリア(C)ボタンが押されたら呼ばれる
    public void OnClearPressed()
    {
        if (inputField != null)
        {
            inputField.text = "";
        }
    }

    // Enterキーが押されたら呼ばれる
    public void OnEnterPressed()
    {
        if (inputField != null)
        {
            CheckPassword(inputField.text);
        }

        // 揺れている間は入力を受け付けない
        if (isShaking || inputField == null) return;
        CheckPassword(inputField.text);
    }

    void Update()
    {
        if (isPlayerInside && !isShaking) // 揺れている間は操作不可
        {
            if (!isUiActive)
            {
                if (Input.GetKeyDown(KeyCode.Space)) OpenPasswordUI();
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Escape)) ClosePasswordUI();
            }
        }

        if (playerTransform == null)
        {
            return;
        }

        if (isPlayerInside && !isShaking)
        {
            if (!isUiActive)
            {
                // エリア内にいてUIが開いていない時、スペースキーで開く
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    OpenPasswordUI();
                }
            }
            else
            {
                // UIが開いている時、ESCキーで閉じる
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ClosePasswordUI();
                }
            }
        }
    }

    // パスワード入力画面を開く
    void OpenPasswordUI()
    {
        isUiActive = true;
        // UIが開いている時はヒントを消す
        if (promptText != null) promptText.SetActive(false);
        passwordPanel.SetActive(true);

        // 入力欄をクリアしてフォーカスする（すぐに打てるように）
        inputField.text = "";
        inputField.ActivateInputField();

        // カーソルを表示してロック解除
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // プレイヤーが動かないようにスクリプトを止める
        playerScript = playerTransform.GetComponent<MonoBehaviour>();
        if (playerScript != null) playerScript.enabled = false;
    }

    // パスワード入力画面を閉じる
    public void ClosePasswordUI()
    {
        if (isShaking) return;

        isUiActive = false;
        if (promptText != null) promptText.SetActive(true);
        passwordPanel.SetActive(false);

        // カーソルを消してロック
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerScript = playerTransform.GetComponent<MonoBehaviour>();
        // プレイヤーの動きを再開
        if (playerScript != null) playerScript.enabled = true;
    }

    // パスワードの判定（Enterを押したときに呼ばれる）
    public void CheckPassword(string input)
    {
        if (input == correctPassword)
        {
            Debug.Log("正解！");
            ClosePasswordUI();
            if (promptText != null) promptText.SetActive(false);

            // 正解音を鳴らす
            if (successSound != null) audioSource.PlayOneShot(successSound);

            Destroy(doorObject); // ドアを消す
        }
        else
        {
            if (!isShaking && passwordPanel != null)
            {
                StartCoroutine(ShakePanel());
            }

            Debug.Log("不正解...");

            // 間違い音を鳴らす
            if (errorSound != null) audioSource.PlayOneShot(errorSound);

            inputField.text = ""; // 文字を消してやり直し
            inputField.ActivateInputField(); // フォーカスを戻す
        }
    }

    IEnumerator ShakePanel()
    {
        isShaking = true;
        RectTransform panelRect = passwordPanel.GetComponent<RectTransform>();
        Vector3 originalPos = panelRect.localPosition; // 元の位置を記憶

        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // ランダムな位置に少しずらす
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            panelRect.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null; // 1フレーム待つ
        }

        // 元の位置に戻す
        panelRect.localPosition = originalPos;

        // テキストをクリア
        if (inputField != null) inputField.text = "";

        isShaking = false;
    }
}