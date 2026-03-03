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
    // ★rotationSpeed 変数を削除

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

        // ★マウス入力による回転処理（MouseButton(0) と MouseButtonDown(1) のブロック）を削除
    }

    void OpenImage()
    {
        if (imageViewerPanel == null || displayImageComponent == null) return;

        // ★追加：開いた瞬間に「見た」ことにする
        hasBeenViewed = true;

        displayImageComponent.sprite = imageToDisplay;
        displayImageComponent.rectTransform.localScale = imageScale;
        // 回転させないので、常に初期回転（identity）で表示
        displayImageComponent.rectTransform.localRotation = Quaternion.identity;

        imageViewerPanel.SetActive(true);
        if (closeGuideText != null) closeGuideText.SetActive(true);

        isViewing = true;

        if (playerController != null)
        {
            playerController.canControl = false;
            // 回転させないが、閉じる操作のためにカーソルを表示するか、
            // あるいはカーソルをロックしたままSpaceキーのみ受け付けるかは仕様によります。
            // 元のコードに合わせてカーソル表示処理は残しています。
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