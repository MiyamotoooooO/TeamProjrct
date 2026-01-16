using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

// 物理挙動（Rigidbody）を必須にする
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    float x, z;

    [Header("メインカメラを参照")]
    public float speed = 0.1f;

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

        // キー入力がある場合
        if (moveDir.magnitude > 0)
        {
            // 入力方向 × スピード で速度を決定
            rb.velocity = new Vector3(moveDir.x * speed, rb.velocity.y, moveDir.z * speed);
        }
        else
        {
            // キー入力がない場合
            // 水平方向（x, z）の速度を 0 にする
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
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
}

