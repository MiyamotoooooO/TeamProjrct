using UnityEngine;

public class SearchPoint : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("手に入るアイテムの名前")]
    public string itemName;

    [Tooltip("「調べる」と表示する文字（近づいた時だけ出る）")]
    public GameObject promptSpriteObject;

    [Tooltip("★ここに追加：遠くからでも見える光るエフェクト（キラキラ）")]
    public GameObject glowEffectObject;

    [Tooltip("一度調べたら消えるかどうか")]
    public bool oneTimeOnly = true;

    [Header("キー設定")]
    public KeyCode interactKey = KeyCode.E;

    // 内部変数
    private bool isPlayerNearby = false;
    private InventoryManager inventoryManager;
    private Transform mainCameraTransform;

    void Start()
    {
        inventoryManager = FindAnyObjectByType<InventoryManager>();

        // 文字は隠す
        if (promptSpriteObject != null)
        {
            promptSpriteObject.SetActive(false);
        }

        // ★光るエフェクトは最初からONにしておく（遠くから見えるように）
        if (glowEffectObject != null)
        {
            glowEffectObject.SetActive(true);
        }

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // 文字の向きをカメラに合わせる
        if (isPlayerNearby && promptSpriteObject != null && mainCameraTransform != null)
        {
            promptSpriteObject.transform.LookAt(
                promptSpriteObject.transform.position + mainCameraTransform.rotation * Vector3.forward,
                mainCameraTransform.rotation * Vector3.up
            );
        }

        // ★光るエフェクトもカメラに向ける（もし2D画像を使うなら必要）
        /*
        if (glowEffectObject != null && mainCameraTransform != null)
        {
            glowEffectObject.transform.LookAt(
                glowEffectObject.transform.position + mainCameraTransform.rotation * Vector3.forward,
                mainCameraTransform.rotation * Vector3.up
            );
        }
        */

        // 近くにいてボタンを押したら
        if (isPlayerNearby && Input.GetKeyDown(interactKey))
        {
            GetItem();
        }
    }

    void GetItem()
    {
        if (inventoryManager != null)
        {
            inventoryManager.AddItem(itemName);

            if (oneTimeOnly)
            {
                // 文字も光も消す
                if (promptSpriteObject != null) promptSpriteObject.SetActive(false);
                if (glowEffectObject != null) glowEffectObject.SetActive(false);

                this.enabled = false;
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (promptSpriteObject != null) promptSpriteObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (promptSpriteObject != null) promptSpriteObject.SetActive(false);
        }
    }
}