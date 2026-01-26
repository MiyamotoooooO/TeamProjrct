using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PasswordDoor : MonoBehaviour
{
    // ★追加：どこからでも「今パスワード画面が開いてるか」を知れるようにする変数
    public static bool IsAnyWindowOpen = false;

    [Header("パスワード設定")]
    public string correctPassword = "1234";
    public LockDoorController targetDoor;

    [Header("プレイヤー操作スクリプト（自動で止めるので空欄でもOK）")]
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour playerCameraScript;

    [Header("UI設定")]
    public GameObject passwordPanel;
    public TMP_InputField inputField;
    public GameObject promptText;
    public Button closeButton;

    [Header("音の設定")]
    public AudioClip successSound;
    public AudioClip errorSound;

    [Header("演出設定")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 10f;

    // private
    private bool isPlayerInside = false;
    private bool isUiActive = false;
    private bool isShaking = false;
    private bool isCleared = false;
    private AudioSource audioSource;

    void Start()
    {
        // 念のため初期化時はFalseに
        IsAnyWindowOpen = false;

        if (passwordPanel != null) passwordPanel.SetActive(false);
        if (promptText != null) promptText.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(ClosePasswordUI);
        if (inputField != null) inputField.onSubmit.AddListener(CheckPassword);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnDestroy()
    {
        // オブジェクトが消えるときはフラグを下ろす（バグ防止）
        if (isUiActive) IsAnyWindowOpen = false;
    }

    void Update()
    {
        if (isCleared) return;

        if (isPlayerInside && !isShaking)
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
    }

    public void CheckPassword(string input)
    {
        if (input == correctPassword)
        {
            Debug.Log("正解！");
            if (successSound != null) audioSource.PlayOneShot(successSound);
            if (targetDoor != null) targetDoor.UnlockDoor();

            ClosePasswordUI();

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            isCleared = true;
            if (promptText != null) promptText.SetActive(false);
        }
        else
        {
            if (!isShaking && passwordPanel != null) StartCoroutine(ShakePanel());
            if (errorSound != null) audioSource.PlayOneShot(errorSound);

            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }
        }
    }

    void OpenPasswordUI()
    {
        isUiActive = true;
        // ★フラグをオンにする
        IsAnyWindowOpen = true;

        if (promptText != null) promptText.SetActive(false);
        passwordPanel.SetActive(true);

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }

        // ※ここでのカーソル表示処理は削除し、GameManagerに任せます
        // Cursor.visible = true; ... (削除)

        // プレイヤー停止
        SetPlayerControl(false);
    }

    public void ClosePasswordUI()
    {
        if (isShaking) return;

        isUiActive = false;
        // ★フラグをオフにする
        IsAnyWindowOpen = false;

        if (promptText != null && !isCleared) promptText.SetActive(true);
        passwordPanel.SetActive(false);

        // ※ここでのカーソル非表示処理も削除し、GameManagerに任せます
        // Cursor.visible = false; ... (削除)

        // プレイヤー再開
        SetPlayerControl(true);
    }

    // プレイヤーの操作をON/OFFする関数
    void SetPlayerControl(bool enable)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = enable;
        if (playerCameraScript != null) playerCameraScript.enabled = enable;

        // もしInspectorで指定されていなくても、タグから自動で探して止める
        if (playerMovementScript == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                // 一般的なFPSコントローラーの名前を探して止める
                var inputs = p.GetComponent("StarterAssetsInputs") as MonoBehaviour;
                if (inputs != null) inputs.enabled = enable;

                var controller = p.GetComponent("FirstPersonController") as MonoBehaviour;
                if (controller != null) controller.enabled = enable;
            }
        }
    }

    // --- ボタン操作用 ---
    public void OnNumberPressed(string number)
    {
        if (isShaking || inputField == null) return;
        inputField.text += number;
    }
    public void OnClearPressed()
    {
        if (inputField != null) inputField.text = "";
    }
    public void OnEnterPressed()
    {
        if (inputField != null) CheckPassword(inputField.text);
    }

    IEnumerator ShakePanel()
    {
        isShaking = true;
        RectTransform panelRect = passwordPanel.GetComponent<RectTransform>();
        Vector3 originalPos = panelRect.localPosition;
        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            panelRect.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        panelRect.localPosition = originalPos;
        isShaking = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCleared) return;
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (!isUiActive && promptText != null) promptText.SetActive(true);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (promptText != null) promptText.SetActive(false);
            if (isUiActive) ClosePasswordUI();
        }
    }
}