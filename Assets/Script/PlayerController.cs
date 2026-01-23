using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    float x, z;

    [Header("移動設定")]
    public float walkSpeed = 5.0f;
    public float dashSpeed = 10.0f;

    [Header("メインカメラを参照")]
    public GameObject cam;

    [Header("インベントリ管理")]
    public InventoryManager inventoryManager;

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

    [Header("拾うUI")]
    public TMP_Text pickUpText;
    public float pickUpDistance = 3f;

    [Header("モデル設定")]
    public GameObject KeyModel;
    public GameObject ItemModel;
    public float bobSpeed = 6f;
    public float bobAmount = 0.05f;
    public float swingAmount = 0.1f;
    public float swingSpeed = 1f;
    public float SwingUpAmount = 0.1f;
    public float SwingDownAmount = -0.25f;
    public float SwingSpeed = 6f;

    [Header("デコイ")]
    [SerializeField] private GameObject decoy;
    [SerializeField] private float decoySpawnDistance;

    [Header("--- 足音設定 ---")]
    public AudioSource footstepAudioSource;
    [Tooltip("歩いている時のループ音源")]
    public AudioClip walkSoundLoop;
    [Tooltip("走っている時のループ音源")]
    public AudioClip runSoundLoop;
    [Tooltip("地面判定の距離")]
    public float groundCheckDistance = 0.5f;

    // ★追加：フェードの速さ（大きいほど早く音が消える）
    [Tooltip("音のフェード速度（プツン音防止）")]
    public float audioFadeSpeed = 10.0f;

    // 内部変数
    private Vector3 KeyModelDefaultPos;
    private Vector3 itemModelDefaultPos;
    private float bobTimer = 0f;
    private bool isSwinging = false;
    private float swingTimer = 0f;
    private bool isItemSwing = false;
    private float itemSwingTimer = 0f;

    Quaternion cameraRot, characterRot;
    bool cursorLock = true;
    public bool canLock = true;
    public bool isInventoryOpen = false;
    float minX = -90f, maxX = 90f;
    Rigidbody rb;

    private void Start()
    {
        cameraRot = cam.transform.localRotation;
        characterRot = transform.localRotation;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (footstepAudioSource == null)
            footstepAudioSource = GetComponent<AudioSource>();

        footstepAudioSource.loop = true;
        footstepAudioSource.volume = 0; // 最初は音量0にしておく

        if (inventoryManager == null)
            inventoryManager = Object.FindAnyObjectByType<InventoryManager>();

        KeyModelDefaultPos = KeyModel.transform.localPosition;
        itemModelDefaultPos = ItemModel.transform.localPosition;
    }

    private void OnDisable()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
        }
    }

    private void Update()
    {
        // 操作不可ならフェードアウトさせて一時停止
        if (!canControl)
        {
            FadeOutAndPause();
            return;
        }

        if (canLock)
        {
            float xRot = Input.GetAxis("Mouse X") * Ysensityvity;
            float yRot = Input.GetAxis("Mouse Y") * Xsensityvity;
            cameraRot *= Quaternion.Euler(-yRot, 0, 0);
            characterRot *= Quaternion.Euler(0, xRot, 0);
            cameraRot = ClampRotation(cameraRot);
            cam.transform.localRotation = cameraRot;
            transform.localRotation = characterRot;
        }

        RotateCamera();
        UpdateCursorLock();

        if (!isInventoryOpen)
        {
            CheckPickUp();
            if (Input.GetKeyDown(KeyCode.Q)) DropCurrentItem();
        }

        UpdateItemBob();
        UpdateKeySwing();
        UpdateItemSwing();

        HandleFootstepsAudio();

        if (Input.GetKeyDown(KeyCode.G))
        {
            Vector3 spawnPos = transform.position + transform.forward * decoySpawnDistance;
            Instantiate(decoy, spawnPos, Quaternion.identity);
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            canControl = false;
            FadeOutAndPause(); // ポーズ時もフェードアウト
            pauseMenu.SetActive(true);
        }

        if (!canControl)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        MoveCharacter();

        if (isInventoryOpen)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }
        x = Input.GetAxisRaw("Horizontal") * walkSpeed;
        z = Input.GetAxisRaw("Vertical") * walkSpeed;
    }

    void MoveCharacter()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 moveDir = (forward * v + right * h).normalized;
        float currentSpeed = Input.GetKey(KeyCode.R) ? dashSpeed : walkSpeed;

        rb.velocity = new Vector3(moveDir.x * currentSpeed, rb.velocity.y, moveDir.z * currentSpeed);
    }

    // ★★★ 足音管理（フェード機能付き） ★★★
    void HandleFootstepsAudio()
    {
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        bool isMoving = isGrounded && hasInput;

        if (isMoving)
        {
            // 動いている場合
            bool isRunning = Input.GetKey(KeyCode.R);
            AudioClip targetClip = isRunning ? runSoundLoop : walkSoundLoop;

            // クリップの切り替え（歩き⇔走り）
            if (footstepAudioSource.clip != targetClip)
            {
                footstepAudioSource.clip = targetClip;
                footstepAudioSource.time = 0; // 切り替え時はリセット
                footstepAudioSource.Play();
            }
            else
            {
                // 同じクリップで停止中なら再開
                if (!footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.Play();
                }
            }

            // ★フェードイン：音量を徐々に 1 に近づける
            footstepAudioSource.volume = Mathf.Lerp(footstepAudioSource.volume, 1.0f, Time.deltaTime * audioFadeSpeed);
        }
        else
        {
            // 止まっている場合：フェードアウトしてからPause
            FadeOutAndPause();
        }
    }

    // ★フェードアウト処理用の関数
    void FadeOutAndPause()
    {
        if (footstepAudioSource.isPlaying)
        {
            // 音量を徐々に 0 に近づける
            footstepAudioSource.volume = Mathf.Lerp(footstepAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);

            // 音量がほぼゼロ（0.01以下）になったら完全に一時停止する
            if (footstepAudioSource.volume < 0.01f)
            {
                footstepAudioSource.Pause();
                footstepAudioSource.volume = 0; // 念のため0にする
            }
        }
    }

    // --- 以下、既存の関数群 ---
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
        if (Input.GetKeyDown(KeyCode.Escape)) cursorLock = false;
        else if (Input.GetMouseButton(0)) cursorLock = true;

        if (cursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public Quaternion ClampRotation(Quaternion q)
    {
        q.x /= q.w; q.y /= q.w; q.z /= q.w; q.w = 1f;
        float angleX = Mathf.Atan(q.x) * Mathf.Rad2Deg * 2f;
        angleX = Mathf.Clamp(angleX, minX, maxX);
        q.x = Mathf.Tan(angleX * Mathf.Deg2Rad * 0.5f);
        return q;
    }

    public void SyncRotationToCurrent()
    {
        cameraRot = cam.transform.localRotation;
        characterRot = transform.localRotation;
    }

    public void pause(string command)
    {
        switch (command)
        {
            case "Title": if (save) SceneManager.LoadScene("TitleScene"); else Debug.Log("NoSave"); break;
            case "Option": option.SetActive(true); pauseMenu.SetActive(false); break;
            case "Save": save = true; Debug.Log("SaveGame"); break;
            case "Return": save = false; canControl = true; pauseMenu.SetActive(false); break;
            case "Pause": option.SetActive(false); pauseMenu.SetActive(true); break;
        }
    }

    void CheckPickUp()
    {
        if (isInventoryOpen) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        pickUpText.enabled = false;
        if (Physics.Raycast(ray, out hit, pickUpDistance, itemLayer))
        {
            pickUpText.enabled = true;
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (inventoryManager != null) { inventoryManager.PickUpItem(hit.collider.gameObject); UpdateItemModel(); }
            }
        }
    }

    void DropCurrentItem()
    {
        if (inventoryManager.currentItems.Count == 0) return;
        string itemName = inventoryManager.currentItems[0];
        Vector3 dropPos = transform.position + transform.forward * 1f;
        inventoryManager.DropItem(itemName, dropPos);
        UpdateItemModel();
    }

    public void UpdateItemModel()
    {
        KeyModel.SetActive(false);
        ItemModel.SetActive(false);
        if (inventoryManager.currentItems.Count == 0) return;
        string firstItem = inventoryManager.currentItems[0];
        int layer = inventoryManager.GetItemLayer(firstItem);
        if (layer == LayerMask.NameToLayer("Key")) KeyModel.SetActive(true);
        else if (layer == LayerMask.NameToLayer("Item")) ItemModel.SetActive(true);
    }

    void UpdateItemBob()
    {
        bool isMoving = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffsetY = Mathf.Sin(bobTimer) * bobAmount;
            float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmount;
            if (KeyModel.activeSelf) KeyModel.transform.localPosition = KeyModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
        }
        else
        {
            if (KeyModel.activeSelf) KeyModel.transform.localPosition = Vector3.Lerp(KeyModel.transform.localPosition, KeyModelDefaultPos, Time.deltaTime * 10f);
            if (ItemModel.activeSelf) ItemModel.transform.localPosition = Vector3.Lerp(ItemModel.transform.localPosition, itemModelDefaultPos, Time.deltaTime * 10f);
            bobTimer = 0f;
        }
    }

    void UpdateKeySwing()
    {
        if (!isSwinging) return;
        swingTimer += Time.deltaTime * swingSpeed;
        float swingOffset = Mathf.Sin(swingTimer) * swingAmount;
        if (KeyModel.activeSelf) KeyModel.transform.localPosition = KeyModelDefaultPos + new Vector3(0, 0, swingOffset);
        if (ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos + new Vector3(0, 0, swingOffset);
        if (swingTimer >= Mathf.PI)
        {
            isSwinging = false;
            if (KeyModel.activeSelf) KeyModel.transform.localPosition = KeyModelDefaultPos;
            if (ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos;
        }
    }

    void UpdateItemSwing()
    {
        if (!isItemSwing) return;
        itemSwingTimer += Time.deltaTime * SwingSpeed;
        if (itemSwingTimer < 0.3f)
        {
            float t = itemSwingTimer / 0.3f;
            ItemModel.transform.localPosition = Vector3.Lerp(itemModelDefaultPos, itemModelDefaultPos + new Vector3(0, SwingUpAmount, 0), t);
        }
        else if (itemSwingTimer < 1f)
        {
            float t = (itemSwingTimer - 0.3f) / 0.7f;
            ItemModel.transform.localPosition = Vector3.Lerp(itemModelDefaultPos + new Vector3(0, SwingUpAmount, 0), itemModelDefaultPos + new Vector3(0, SwingDownAmount, 0), t);
        }
        else
        {
            ItemModel.transform.localPosition = itemModelDefaultPos;
            isItemSwing = false;
            itemSwingTimer = 0f;
        }
    }

    public void PlayKeySwing() { isSwinging = true; swingTimer = 0f; }
    public void PlayItemSwing() { isItemSwing = true; itemSwingTimer = 0f; }
}