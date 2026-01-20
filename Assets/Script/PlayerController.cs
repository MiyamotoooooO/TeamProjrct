using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 物理挙動（Rigidbody）を必須にする
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float walkSpeed = 5.0f; // 通常時の速度
    public float dashSpeed = 10.0f; // ダッシュ時の速度

    // 元の speed は walkSpeed に置き換えました

    [Header("メインカメラを参照")]
    public GameObject cam;

    [Header("インベントリ管理")]
    public InventoryManager inventoryManager;

    [Header("アイテムを拾える距離")]
    public float pickUpDistance = 3f;

    [Header("プレイヤーが操作可能かどうか")]
    public bool canControl = true;

    [Header("感度設定")]
    public float Xsensityvity = 3f;
    public float Ysensityvity = 3f;

    [Header("アイテムレイヤー")]
    public LayerMask itemLayer;

    [Header("ポーズ画面関連")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject option;
    private bool save;

    [Header("デコイ")]
    [SerializeField] private GameObject decoy;

    [Header("デコイ生成位置までの距離")]
    [SerializeField] private float decoySpawnDistance;

    // 内部変数
    Quaternion cameraRot, characterRot;
    bool cursorLock = true;
    float minX = -90f, maxX = 90f;
    Rigidbody rb;

    private void Start()
    {
        cameraRot = cam.transform.localRotation;
        characterRot = transform.localRotation;

        // Rigidbodyを取得
        rb = GetComponent<Rigidbody>();
        // 物理演算で転ばないように回転を固定
        rb.freezeRotation = true;

        if (inventoryManager == null)
        {
            inventoryManager = Object.FindAnyObjectByType<InventoryManager>();
        }
    }

    private void Update()
    {
        // 操作不可なら処理を中断
        if (!canControl) return;

        RotateCamera();
        UpdateCursorLock();
        CheckPickUp();

        //Escapeキーが押された
        if (Input.GetKey(KeyCode.Escape))
        {
            //Playerの動きを停止&ポーズメニューを表示
            canControl = false;
            pauseMenu.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            //デコイを召喚
            Vector3 spawnPos = transform.position + transform.forward * decoySpawnDistance;
            GameObject copyDecoy = Instantiate(decoy, spawnPos, Quaternion.identity);
        }
    }

    private void FixedUpdate()
    {
        MoveCharacter();
    }

    // 移動処理（ここをダッシュ対応に変更）
    void MoveCharacter()
    {
        // 操作不可なら停止させる
        if (!canControl)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        // 入力を取得 (-1.0 〜 1.0)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // カメラの向きから水平成分だけ取り出す（空を飛ばないように）
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // 進みたい方向を計算
        Vector3 moveDir = (forward * v + right * h).normalized;

        // ダッシュ判定
        // Spaceキーを押している間は dashSpeed、離していれば walkSpeed を使う
        float currentSpeed = Input.GetKey(KeyCode.R) ? dashSpeed : walkSpeed;

        // Rigidbodyの速度を更新
        rb.velocity = new Vector3(moveDir.x * currentSpeed, rb.velocity.y, moveDir.z * currentSpeed);
    }

    // 視点移動
    void RotateCamera()
    {
        float xRot = Input.GetAxis("Mouse X") * Ysensityvity;
        float yRot = Input.GetAxis("Mouse Y") * Xsensityvity;

        cameraRot *= Quaternion.Euler(-yRot, 0, 0);
        characterRot *= Quaternion.Euler(0, xRot, 0);

        cameraRot = ClampRotation(cameraRot);

        cam.transform.localRotation = cameraRot;
        transform.localRotation = characterRot;
    }

    public void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cursorLock = false;
        }
        else if (Input.GetMouseButton(0))
        {
            cursorLock = true;
        }

        if (cursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false; // カーソルも見えなくする
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public Quaternion ClampRotation(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1f;

        float angleX = Mathf.Atan(q.x) * Mathf.Rad2Deg * 2f;
        angleX = Mathf.Clamp(angleX, minX, maxX);
        q.x = Mathf.Tan(angleX * Mathf.Deg2Rad * 0.5f);

        return q;
    }

    void CheckPickUp()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickUpDistance, itemLayer))
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("アイテムを拾った：" + hit.collider.name);

                if (inventoryManager != null)
                {
                    inventoryManager.PickUpItem(hit.collider.gameObject);
                }
            }
        }
    }

    public void SyncRotationToCurrent()
    {
        cameraRot = cam.transform.localRotation;
        characterRot = transform.localRotation;
    }

    //ポーズ画面でボタンから各動作を呼び出す関数
    public void pause(string command)
    {
        switch (command)
        {
            case "Title"://タイトルに戻る
                if (save)
                    SceneManager.LoadScene("TitleScene");
                else
                    Debug.Log("NoSave");
                break;
            case "Option"://オブション変更画面へ
                option.SetActive(true);
                pauseMenu.SetActive(false);
                break;
            case "Save"://セーブ
                save = true;
                Debug.Log("SaveGame");
                break;
            case "Return"://ゲームに戻る
                save = false;
                //Debug.Log("PlayerStart");
                canControl = true;
                pauseMenu.SetActive(false);
                break;
            case "Pause"://オプション画面からポーズ画面へ
                option.SetActive(false);
                pauseMenu.SetActive(true);
                break;
        }
    }
}



/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

// 物理挙動（Rigidbody）を必須にする
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    float x, z;

    [Header("移動速度")]
    public float speed = 0.1f;
    public float dashSpeed = 10.0f; // ダッシュ時の速度

    [Header("メインカメラを参照")]
    public GameObject cam;

    [Header("インベントリ管理")]
    public InventoryManager inventoryManager;

    [Header("アイテムを拾える距離")]
    public float pickUpDistance = 3f;

    [Header("プレイヤーが操作可能かどうか")]
    public bool canControl = true;

    Quaternion cameraRot, characterRot;
    float Xsensityvity = 3f, Ysensityvity = 3f;
    public LayerMask itemLayer;

    bool cursorLock = true;

    float minX = -90f, maxX = 90f;

    Rigidbody rb;

    private void Start()
    {
        cameraRot = cam.transform.localRotation;
        characterRot = transform.localRotation;

        // Rigidbodyを取得
        rb = GetComponent<Rigidbody>();
        // 物理演算で転ばないように回転を固定
        rb.freezeRotation = true;

        if (inventoryManager == null)
        {
            inventoryManager = Object.FindAnyObjectByType<InventoryManager>();
        }
    }

    private void Update()
    {
        // 操作不可なら処理を中断
        if (!canControl) return;

        float xRot = Input.GetAxis("Mouse X") * Ysensityvity;
        float yRot = Input.GetAxis("Mouse Y") * Xsensityvity;

        cameraRot *= Quaternion.Euler(-yRot, 0, 0);
        characterRot *= Quaternion.Euler(0, xRot, 0);

        cameraRot = ClampRotation(cameraRot);

        cam.transform.localRotation = cameraRot;
        transform.localRotation = characterRot;

        UpdateCursorLock();

        CheckPickUp();
    }

    private void FixedUpdate()
    {
        // 操作不可なら停止させる
        if (!canControl)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        x = Input.GetAxisRaw("Horizontal") * speed;
        z = Input.GetAxisRaw("Vertical") * speed;

        // カメラの向きから水平成分だけ取り出す
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // 水平移動のみ適応
        transform.position += forward * z + right * x;

        // 斜め移動が速くならないように正規化
        Vector3 moveDir = transform.forward * z + transform.right * x;

        float currentSpeed = Input.GetKey(KeyCode.R) ? dashSpeed : speed;

        // Rigidbodyの速度を更新
        rb.velocity = new Vector3(moveDir.x * currentSpeed, rb.velocity.y, moveDir.z * currentSpeed);
    }

    public void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cursorLock = false;
        }
        else if (Input.GetMouseButton(0))
        {
            cursorLock = true;
        }

        if (cursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (!cursorLock)
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public Quaternion ClampRotation(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1f;

        float angleX = Mathf.Atan(q.x) * Mathf.Rad2Deg * 2f;

        angleX = Mathf.Clamp(angleX, minX, maxX);

        q.x = Mathf.Tan(angleX * Mathf.Deg2Rad * 0.5f);

        return q;
    }

    void CheckPickUp()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickUpDistance, itemLayer))
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("アイテムを拾った：" + hit.collider.name);

                if (inventoryManager != null)
                {
                    inventoryManager.PickUpItem(hit.collider.gameObject);
                }
            }
        }
    }

    // 外部から無理やり視点を同期させる関数
    public void SyncRotationToCurrent()
    {
        cameraRot = cam.transform.localRotation;
        characterRot = transform.localRotation;
    }
}*/

