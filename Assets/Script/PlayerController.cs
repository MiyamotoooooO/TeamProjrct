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
    public float SwingUpRotation = -20f; // 振り上げ量
    public float SwingDownRotation = 60f; // 振り下ろし量
    private Quaternion defaultRot;
    Quaternion cameraSwingStartRot;


    [Header("デコイ")]
    [SerializeField] private GameObject decoy;
    [SerializeField] private float decoySpawnDistance;

    [Header("--- 足音設定 ---")]
    public AudioSource footstepAudioSource;
    [Tooltip("歩いている時のループ音源")]
    public AudioClip walkSoundLoop;
    [Tooltip("走っている時のループ音源")]
    public AudioClip runSoundLoop;

    [Header("--- 吐息（ブレス）設定 ---")]
    [Tooltip("吐息用のAudioSource")]
    public AudioSource breathingAudioSource;
    [Tooltip("吐息のループ音源")]
    public AudioClip breathingSoundLoop;
    [Tooltip("歩き時の吐息音量")]
    [Range(0f, 1f)]
    public float breathingWalkVolume = 0.3f;
    [Tooltip("走り時の吐息音量")]
    [Range(0f, 1f)]
    public float breathingRunVolume = 0.5f;

    [Header("--- 視点の揺れ（Head Bob） ---")]
    [Tooltip("歩いている時の揺れる速さ")]
    public float walkBobFrequency = 10.0f;
    [Tooltip("歩いている時の揺れ幅（X, Y）")]
    public Vector2 walkBobAmount = new Vector2(0.05f, 0.05f);

    [Tooltip("走っている時の揺れる速さ")]
    public float runBobFrequency = 15.0f;
    [Tooltip("走っている時の揺れ幅（X, Y）... 強くするならここを大きく")]
    public Vector2 runBobAmount = new Vector2(0.1f, 0.15f);

    [Tooltip("揺れの滑らかさ")]
    public float bobSmoothing = 10.0f;

    [Header("共通オーディオ設定")]
    [Tooltip("地面判定の距離")]
    public float groundCheckDistance = 0.5f;
    [Tooltip("音のフェード速度")]
    public float audioFadeSpeed = 5.0f;

    // 内部変数
    private Vector3 KeyModelDefaultPos;
    private Vector3 itemModelDefaultPos;
    private float bobTimer = 0f;
    private bool isSwinging = false;
    private float swingTimer = 0f;
    private bool isItemSwing = false;
    private float itemSwingTimer = 0f;

    private bool isCameraSwing = false;
    private float cameraSwingTimer = -2f;
    public float cameraSwingSpeed = 0.01f; // 速さ
    public float cameraSwingUpAngle = -6f; // 上に傾ける角度
    public float cameraSwingDownAngle = 2f; // 下に傾ける角度
    public float swingUpAngle = 4f; // 振りかぶりの角度
    public float swingDownAngle = -12f; // 振り下ろしの角度
    private Quaternion cameraDefaultRot;

    // ★カメラ揺れ用変数
    private Vector3 defaultCamPos;
    private float camBobTimer = 0f;

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
        footstepAudioSource.volume = 0;

        if (breathingAudioSource != null && breathingSoundLoop != null)
        {
            breathingAudioSource.clip = breathingSoundLoop;
            breathingAudioSource.loop = true;
            breathingAudioSource.volume = 0;
            breathingAudioSource.Play();
        }

        if (inventoryManager == null)
            inventoryManager = Object.FindAnyObjectByType<InventoryManager>();

        KeyModelDefaultPos = KeyModel.transform.localPosition;
        itemModelDefaultPos = ItemModel.transform.localPosition;
        defaultRot = ItemModel.transform.localRotation;
        cameraDefaultRot = cam.transform.localRotation;

        // ★カメラの初期位置を保存
        defaultCamPos = cam.transform.localPosition;
    }

    private void OnDisable()
    {
        if (footstepAudioSource != null) footstepAudioSource.Stop();
        if (breathingAudioSource != null) breathingAudioSource.Stop();
    }

    private void Update()
    {
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
        UpdateCameraSwing();
        HandleFootstepsAudio();
        HandleBreathingAudio();

        // ★追加：カメラの揺れ処理
        HandleCameraShake();

        if (Input.GetKeyDown(KeyCode.G))
        {
            Vector3 spawnPos = transform.position + transform.forward * decoySpawnDistance;
            Instantiate(decoy, spawnPos, Quaternion.identity);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (KeyModel.activeSelf)
            {
                PlayKeySwing();
            }
            else if (ItemModel.activeSelf)
            {
                PlayItemSwing();
            }
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            canControl = false;
            FadeOutAndPause();
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

    // --- ★追加：カメラの揺れ（Head Bob）処理 ---
    void HandleCameraShake()
    {
        // 接地していて、かつ移動キー入力があるか
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);

        if (isGrounded && hasInput)
        {
            // 走っているか判定
            bool isRunning = Input.GetKey(KeyCode.R);

            // パラメータの切り替え
            float currentFrequency = isRunning ? runBobFrequency : walkBobFrequency;
            Vector2 currentAmount = isRunning ? runBobAmount : walkBobAmount;

            // タイマーを進める（サインカーブ用）
            camBobTimer += Time.deltaTime * currentFrequency;

            // 位置のオフセット計算
            // Y軸：上下の揺れ（sin波）
            float yOffset = Mathf.Sin(camBobTimer) * currentAmount.y;
            // X軸：左右の揺れ（cos波で少しゆっくりにすると8の字を描くような揺れになる）
            float xOffset = Mathf.Cos(camBobTimer * 0.5f) * currentAmount.x;

            // 目標位置を計算
            Vector3 targetPos = defaultCamPos + new Vector3(xOffset, yOffset, 0);

            // 滑らかに移動させる
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, targetPos, Time.deltaTime * bobSmoothing);
        }
        else
        {
            // 止まっている時は初期位置に戻す
            camBobTimer = 0;
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, defaultCamPos, Time.deltaTime * bobSmoothing);
        }
    }

    // --- 音声関連 ---
    void HandleFootstepsAudio()
    {
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        bool isMoving = isGrounded && hasInput;

        if (isMoving)
        {
            bool isRunning = Input.GetKey(KeyCode.R);
            AudioClip targetClip = isRunning ? runSoundLoop : walkSoundLoop;

            if (footstepAudioSource.clip != targetClip)
            {
                footstepAudioSource.clip = targetClip;
                footstepAudioSource.time = 0;
                footstepAudioSource.Play();
            }
            else
            {
                if (!footstepAudioSource.isPlaying) footstepAudioSource.Play();
            }
            footstepAudioSource.volume = Mathf.Lerp(footstepAudioSource.volume, 1.0f, Time.deltaTime * audioFadeSpeed);
        }
        else
        {
            if (footstepAudioSource.isPlaying)
            {
                footstepAudioSource.volume = Mathf.Lerp(footstepAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
                if (footstepAudioSource.volume < 0.01f)
                {
                    footstepAudioSource.Pause();
                    footstepAudioSource.volume = 0;
                }
            }
        }
    }

    void HandleBreathingAudio()
    {
        if (breathingAudioSource == null) return;

        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        bool isMoving = isGrounded && hasInput;

        if (isMoving)
        {
            if (!breathingAudioSource.isPlaying) breathingAudioSource.Play();
            bool isRunning = Input.GetKey(KeyCode.R);
            float targetVolume = isRunning ? breathingRunVolume : breathingWalkVolume;
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, targetVolume, Time.deltaTime * audioFadeSpeed);
        }
        else
        {
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
        }
    }

    void FadeOutAndPause()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.volume = Mathf.Lerp(footstepAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
            if (footstepAudioSource.volume < 0.01f)
            {
                footstepAudioSource.Pause();
                footstepAudioSource.volume = 0;
            }
        }
        if (breathingAudioSource != null && breathingAudioSource.isPlaying)
        {
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
        }
    }

    // --- 既存関数 ---
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
        if (cursorLock) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
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

        // 揺れ動きの適用
        if (KeyModel.activeSelf) KeyModel.transform.localPosition = KeyModelDefaultPos + new Vector3(0, 0, swingOffset);
        if (ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos + new Vector3(0, 0, swingOffset);

        // --- 動作終了時の処理 ---
        if (swingTimer >= Mathf.PI)
        {
            isSwinging = false;

            // 位置を初期位置に戻す
            if (KeyModel.activeSelf) KeyModel.transform.localPosition = KeyModelDefaultPos;
            if (ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos;

            // 鍵が表示されていた場合（鍵を使った場合）
            if (KeyModel.activeSelf)
            {
                KeyModel.SetActive(false); // まず見た目を消す

                // ★追加：インベントリデータから削除する処理
                if (inventoryManager != null && inventoryManager.currentItems.Count > 0)
                {
                    // 現在持っているアイテム（リストの先頭）を削除
                    inventoryManager.currentItems.RemoveAt(0);

                    // インベントリの状態が変わったので、モデルの表示更新処理を呼ぶ
                    UpdateItemModel();
                }
            }
        }
    }

    void UpdateItemSwing()
    {
        if (!isItemSwing) return;
        itemSwingTimer += Time.deltaTime * swingSpeed;
        if (itemSwingTimer < 0.3f)
        {
            float t = itemSwingTimer / 0.3f;
            ItemModel.transform.localPosition = Vector3.Lerp(itemModelDefaultPos, itemModelDefaultPos + new Vector3(0, SwingUpAmount, 0), t);
            ItemModel.transform.localRotation = Quaternion.Lerp(defaultRot, Quaternion.Euler(SwingUpRotation, 0, 0), t);
        }
        else if (itemSwingTimer < 1f)
        {
            float t = (itemSwingTimer - 0.3f) / 0.7f;
            ItemModel.transform.localPosition = Vector3.Lerp(itemModelDefaultPos + new Vector3(0, SwingUpAmount, 0), itemModelDefaultPos + new Vector3(0, SwingDownAmount, 0), t);
            ItemModel.transform.localRotation = Quaternion.Lerp(Quaternion.Euler(SwingUpRotation, 0, 0), Quaternion.Euler(SwingDownRotation, 0, 0), t);
        }
        else
        {
            ItemModel.transform.localPosition = itemModelDefaultPos;
            ItemModel.transform.localRotation = defaultRot;
            isItemSwing = false;
            itemSwingTimer = 0f;
        }
    }

    public void PlayKeySwing() { isSwinging = true; swingTimer = 0f; isCameraSwing = false; cameraSwingTimer = 0f; }
    public void PlayItemSwing() { isItemSwing = true; itemSwingTimer = 0f; isCameraSwing = true; cameraSwingTimer = 0f; cameraSwingStartRot = cam.transform.localRotation; }

    void UpdateCameraSwing()
    {
        if (!isCameraSwing) return;

        cameraSwingTimer += Time.deltaTime * swingSpeed;

        float currentX = cam.transform.localEulerAngles.x;
        if (currentX > 180f) currentX -= 360f;

        float downLimit = -85f;
        float allowedDownAngle = cameraSwingDownAngle;

        if (currentX <= downLimit)
        {
            allowedDownAngle = 0f;
        }
        else
        {
            float margin = Mathf.InverseLerp(-90f, downLimit, currentX);
            allowedDownAngle *= margin;
        }

        float angle = 0f;

        if (cameraSwingTimer < 0.5f)
        {
            float t = cameraSwingTimer / 0.3f;
            t = Mathf.SmoothStep(0f, 1f, t);
            angle = Mathf.Lerp(0, cameraSwingUpAngle, t);
        }
        else if (cameraSwingTimer < 0.9f)
        {
            float t = (cameraSwingTimer - 0.3f) / 0.6f;
            t = Mathf.SmoothStep(0f, 1f, t);
            angle = Mathf.Lerp(cameraSwingUpAngle, allowedDownAngle, t);
        }
        else if (cameraSwingTimer < 1.5f)
        {
            float t = (cameraSwingTimer - 1f) / 0.6f;
            t = Mathf.SmoothStep(0f, 1f, t);
            angle = Mathf.Lerp(allowedDownAngle, 0, t);
        }
        else
        {
            cam.transform.localRotation = cameraSwingStartRot;
            isCameraSwing = false;
            cameraSwingTimer = 0f;
            return;
        }
        cam.transform.localRotation = cameraSwingStartRot * Quaternion.Euler(angle, 0, 0);
    }
}
