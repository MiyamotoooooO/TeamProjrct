using UnityEngine;
using UnityEngine.UI;

public class SearchPointViewer : MonoBehaviour
{
    // この手がかりを一度でも見たかどうかのフラグ
    public bool hasBeenViewed = false;

    [Header("表示設定")]
    [Tooltip("画面に表示したい画像")]
    public Sprite imageToDisplay;

    [Tooltip("画像の大きさ (1, 1, 1 が標準)")]
    public Vector3 imageScale = Vector3.one;

    [Header("UI参照（共通設定）")]
    [Tooltip("親パネル（ImageViewer）")]
    public GameObject imageViewerPanel;
    [Tooltip("画像を表示するImageコンポーネント")]
    public Image displayImageComponent;
    [Tooltip("操作ガイドのテキストオブジェクト（Space: 閉じる）")]
    public GameObject closeGuideText;

    [Header("操作設定")]
    public KeyCode openKey = KeyCode.E;
    public KeyCode closeKey = KeyCode.Space;
    public float rotationSpeed = 5.0f;

    // 内部変数
    private bool isPlayerNearby = false;
    private bool isViewing = false;
    private PlayerController playerController;

    void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();

        if (imageViewerPanel == null)
        {
            var panel = GameObject.Find("ImageViewer");
            if (panel != null) imageViewerPanel = panel;
        }

        if (imageViewerPanel != null)
        {
            if (displayImageComponent == null)
                displayImageComponent = imageViewerPanel.transform.Find("DisplayImage")?.GetComponent<Image>();

            if (closeGuideText == null)
            {
                var guide = imageViewerPanel.transform.Find("CloseGuide");
                if (guide != null) closeGuideText = guide.gameObject;
            }
        }
    }

    void Update()
    {
        if (isViewing)
        {
            HandleViewingInput();
            return;
        }

        if (!isPlayerNearby) return;

        if (Input.GetKeyDown(openKey))
        {
            OpenImage();
        }
    }

    void HandleViewingInput()
    {
        if (Input.GetKeyDown(closeKey))
        {
            CloseImage();
            return;
        }

        if (Input.GetMouseButton(0) && displayImageComponent != null)
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotY = Input.GetAxis("Mouse Y") * rotationSpeed;
            displayImageComponent.rectTransform.Rotate(Vector3.up, -rotX, Space.World);
            displayImageComponent.rectTransform.Rotate(Vector3.right, rotY, Space.World);
        }

        if (Input.GetMouseButtonDown(1) && displayImageComponent != null)
        {
            displayImageComponent.rectTransform.localRotation = Quaternion.identity;
        }
    }

    void OpenImage()
    {
        if (imageViewerPanel == null || displayImageComponent == null) return;

        // ★追加：開いた瞬間に「見た」ことにする
        hasBeenViewed = true;

        displayImageComponent.sprite = imageToDisplay;
        displayImageComponent.rectTransform.localScale = imageScale;
        displayImageComponent.rectTransform.localRotation = Quaternion.identity;

        imageViewerPanel.SetActive(true);
        if (closeGuideText != null) closeGuideText.SetActive(true);

        isViewing = true;

        if (playerController != null)
        {
            playerController.canControl = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void CloseImage()
    {
        if (imageViewerPanel == null) return;

        imageViewerPanel.SetActive(false);
        if (closeGuideText != null) closeGuideText.SetActive(false);

        isViewing = false;

        if (playerController != null)
        {
            playerController.canControl = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (isViewing) CloseImage();
        }
    }
}

/*using UnityEngine;
using UnityEngine.UI;

public class SearchPointViewer : MonoBehaviour
{
    [Header("表示設定")]
    [Tooltip("画面に表示したい画像")]
    public Sprite imageToDisplay;

    [Tooltip("画像の大きさ (1, 1, 1 が標準)")]
    public Vector3 imageScale = Vector3.one;

    [Header("UI参照（共通設定）")]
    [Tooltip("親パネル（ImageViewer）")]
    public GameObject imageViewerPanel;
    [Tooltip("画像を表示するImageコンポーネント")]
    public Image displayImageComponent;
    [Tooltip("操作ガイドのテキストオブジェクト（Space: 閉じる）")]
    public GameObject closeGuideText;

    [Header("操作設定")]
    public KeyCode openKey = KeyCode.E;
    public KeyCode closeKey = KeyCode.Space;
    public float rotationSpeed = 5.0f; // 回転の速さ

    [Header("ギミック連携")]
    [Tooltip("これを見ると通れるようになる壁（PassageBlocker）を指定")]
    public PassageBlocker targetBlocker; // ★追加：壁へのリンク

    // 内部変数
    private bool isPlayerNearby = false;
    private bool isViewing = false;
    private bool hasUnlocked = false; // ★追加：すでに解除済みかどうかのフラグ
    private PlayerController playerController;

    void Start()
    {
        // プレイヤーの操作を止めるためにスクリプトを取得
        playerController = FindAnyObjectByType<PlayerController>();

        // UIの自動検索
        if (imageViewerPanel == null)
        {
            var panel = GameObject.Find("ImageViewer");
            if (panel != null) imageViewerPanel = panel;
        }

        if (imageViewerPanel != null)
        {
            if (displayImageComponent == null)
                displayImageComponent = imageViewerPanel.transform.Find("DisplayImage")?.GetComponent<Image>();

            if (closeGuideText == null)
            {
                var guide = imageViewerPanel.transform.Find("CloseGuide");
                if (guide != null) closeGuideText = guide.gameObject;
            }
        }
    }

    void Update()
    {
        // 見ている最中の操作（閉じる・回転・リセット）
        if (isViewing)
        {
            HandleViewingInput();
            return;
        }

        // プレイヤーが近くにいないなら何もしない
        if (!isPlayerNearby) return;

        // 開く操作
        if (Input.GetKeyDown(openKey))
        {
            OpenImage();
        }
    }

    // 見ている最中の入力処理
    void HandleViewingInput()
    {
        // 1. 閉じる操作 (Spaceキー)
        if (Input.GetKeyDown(closeKey))
        {
            CloseImage();
            return;
        }

        // 2. 回転操作（左クリック長押し + ドラッグ）
        if (Input.GetMouseButton(0) && displayImageComponent != null)
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // マウスの動きに合わせて画像を回転させる
            // 上下左右にグリグリ回せるようにします
            displayImageComponent.rectTransform.Rotate(Vector3.up, -rotX, Space.World);
            displayImageComponent.rectTransform.Rotate(Vector3.right, rotY, Space.World);
        }

        // 3. 回転リセット（右クリック）
        if (Input.GetMouseButtonDown(1) && displayImageComponent != null)
        {
            // 回転をリセットして真正面に戻す
            displayImageComponent.rectTransform.localRotation = Quaternion.identity;
        }
    }

    void OpenImage()
    {
        if (imageViewerPanel == null || displayImageComponent == null) return;

        // 1. 画像をセット
        displayImageComponent.sprite = imageToDisplay;

        // 2. 大きさをセット
        displayImageComponent.rectTransform.localScale = imageScale;

        // 3. 回転をリセット
        displayImageComponent.rectTransform.localRotation = Quaternion.identity;

        // 4. UIを表示
        imageViewerPanel.SetActive(true);
        if (closeGuideText != null) closeGuideText.SetActive(true);

        // 5. フラグ更新
        isViewing = true;

        // 6. プレイヤーの動きと視点を止める
        if (playerController != null)
        {
            playerController.canControl = false;
            // カーソルを表示して動かせるようにする
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ★★★ 追加：通路のロックを解除する処理 ★★★
        if (targetBlocker != null && !hasUnlocked)
        {
            targetBlocker.UnlockPassage(); // 壁に「開け！」と命令
            hasUnlocked = true; // 何度も命令しないようにフラグを立てる
        }
    }

    void CloseImage()
    {
        if (imageViewerPanel == null) return;

        // 1. UIを隠す
        imageViewerPanel.SetActive(false);
        if (closeGuideText != null) closeGuideText.SetActive(false);

        // 2. フラグ更新
        isViewing = false;

        // 3. プレイヤーの操作を許可する
        if (playerController != null)
        {
            playerController.canControl = true;
            // カーソルをロックして消す
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (isViewing) CloseImage();
        }
    }
}*/