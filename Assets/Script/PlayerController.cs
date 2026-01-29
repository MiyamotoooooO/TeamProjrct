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
    public GameObject FlashlightModel; // 懐中電灯
    public GameObject LighterModel;    // ★追加：ライターのモデル

    public float bobSpeed = 6f;
    public float bobAmount = 0.05f;
    public float swingAmount = 0.1f;
    public float swingSpeed = 1f;
    public float SwingUpAmount = 0.1f;
    public float SwingDownAmount = -0.25f;
    public float SwingUpRotation = -20f;
    public float SwingDownRotation = 60f;
    private Quaternion defaultRot;
    Quaternion cameraSwingStartRot;

    [Header("デコイ")]
    [SerializeField] private GameObject decoy;
    [SerializeField] private float decoySpawnDistance;

    [Header("--- 足音設定 ---")]
    public AudioSource footstepAudioSource;
    public AudioClip walkSoundLoop;
    public AudioClip runSoundLoop;

    [Header("--- 吐息（ブレス）設定 ---")]
    public AudioSource breathingAudioSource;
    public AudioClip breathingSoundLoop;
    [Range(0f, 1f)]
    public float breathingWalkVolume = 0.3f;
    [Range(0f, 1f)]
    public float breathingRunVolume = 0.5f;

    [Header("--- 視点の揺れ（Head Bob） ---")]
    public float walkBobFrequency = 10.0f;
    public Vector2 walkBobAmount = new Vector2(0.05f, 0.05f);
    public float runBobFrequency = 15.0f;
    public Vector2 runBobAmount = new Vector2(0.1f, 0.15f);
    public float bobSmoothing = 10.0f;

    [Header("共通オーディオ設定")]
    public float groundCheckDistance = 0.5f;
    public float audioFadeSpeed = 5.0f;

    [Header("SortStoneスクリプト")]
    [SerializeField] SortStone sortStone;

    [Header("Stoneレイヤー")]
    [SerializeField] LayerMask stoneLayer;

    // 内部変数
    private Vector3 KeyModelDefaultPos;
    private Vector3 itemModelDefaultPos;
    private Vector3 flashlightModelDefaultPos;
    private Vector3 lighterModelDefaultPos; // ★追加：ライターの位置保存用

    private float bobTimer = 0f;
    private bool isSwinging = false;
    private float swingTimer = 0f;
    private bool isItemSwing = false;
    private float itemSwingTimer = 0f;

    private bool isCameraSwing = false;
    private float cameraSwingTimer = -2f;
    public float cameraSwingSpeed = 0.01f;
    public float cameraSwingUpAngle = -6f;
    public float cameraSwingDownAngle = 2f;
    public float swingUpAngle = 4f;
    public float swingDownAngle = -12f;
    private Quaternion cameraDefaultRot;

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
        if (FlashlightModel != null) flashlightModelDefaultPos = FlashlightModel.transform.localPosition;

        // ★追加：ライターの初期位置保存
        if (LighterModel != null) lighterModelDefaultPos = LighterModel.transform.localPosition;

        defaultRot = ItemModel.transform.localRotation;
        cameraDefaultRot = cam.transform.localRotation;
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
        CheckHitStone();

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
        HandleCameraShake();

        if (Input.GetKeyDown(KeyCode.G))
        {
            Vector3 spawnPos = transform.position + transform.forward * decoySpawnDistance;
            Instantiate(decoy, spawnPos, Quaternion.identity);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (KeyModel.activeSelf) PlayKeySwing();
            else if (ItemModel.activeSelf) PlayItemSwing();
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

    void HandleCameraShake()
    {
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);

        if (isGrounded && hasInput)
        {
            bool isRunning = Input.GetKey(KeyCode.R);
            float currentFrequency = isRunning ? runBobFrequency : walkBobFrequency;
            Vector2 currentAmount = isRunning ? runBobAmount : walkBobAmount;

            camBobTimer += Time.deltaTime * currentFrequency;

            float yOffset = Mathf.Sin(camBobTimer) * currentAmount.y;
            float xOffset = Mathf.Cos(camBobTimer * 0.5f) * currentAmount.x;

            Vector3 targetPos = defaultCamPos + new Vector3(xOffset, yOffset, 0);
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, targetPos, Time.deltaTime * bobSmoothing);
        }
        else
        {
            camBobTimer = 0;
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, defaultCamPos, Time.deltaTime * bobSmoothing);
        }
    }

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

    void CheckHitStone()
    {
        //playerの見ている方向にRayを飛ばす
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction, Color.red);

        if (Physics.Raycast(ray, out hit, pickUpDistance, stoneLayer))
        {
            //左クリックされたなら
            if (Input.GetMouseButtonDown(0))
            {
                //SortStoneの関数を呼ぶ
                sortStone.Stone(hit.collider.gameObject);
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
        // 1. 初期化：一旦すべて非表示にする
        if (KeyModel != null) KeyModel.SetActive(false);
        if (ItemModel != null) ItemModel.SetActive(false);
        if (FlashlightModel != null) FlashlightModel.SetActive(false);
        if (LighterModel != null) LighterModel.SetActive(false);

        // インベントリが空なら終了
        if (inventoryManager == null || inventoryManager.currentItems.Count == 0) return;

        // アイテム情報の取得
        string firstItem = inventoryManager.currentItems[0];
        int currentLayer = inventoryManager.GetItemLayer(firstItem);
        int targetLighterLayer = LayerMask.NameToLayer("Lighter");

        // --- ★原因究明用のログ（解決したら消してOK）---
        // 拾ったアイテムのレイヤー番号 vs Lighterレイヤーの正解番号
        Debug.Log($"【判定中】アイテム名: {firstItem}");
        Debug.Log($"　→ 持っている物のレイヤー番号: {currentLayer}");
        Debug.Log($"　→ 'Lighter' の正解レイヤー番号: {targetLighterLayer}");
        // ----------------------------------------------

        // 2. 判定処理

        // ★ライター判定 (ここが通るかどうか、上のログの番号が一致しているか見てください)
        if (currentLayer == targetLighterLayer)
        {
            Debug.Log("　→ 判定成功！ライターを表示します。");
            if (LighterModel != null) LighterModel.SetActive(true);
        }
        // もしレイヤーがダメでも、名前で救済する処理（保険）
        else if (firstItem.Contains("Lighter"))
        {
            Debug.Log("　→ レイヤーは違いましたが、名前でライターと判断しました。");
            if (LighterModel != null) LighterModel.SetActive(true);
        }
        // --- その他のアイテム ---
        else if (currentLayer == LayerMask.NameToLayer("Key") || firstItem.Contains("Key"))
        {
            if (KeyModel != null) KeyModel.SetActive(true);
        }
        else if (currentLayer == LayerMask.NameToLayer("Flashlight") || firstItem.Contains("Flashlight"))
        {
            if (FlashlightModel != null) FlashlightModel.SetActive(true);
        }
        else if (currentLayer == LayerMask.NameToLayer("Item"))
        {
            if (ItemModel != null) ItemModel.SetActive(true);
        }
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

            if (FlashlightModel != null && FlashlightModel.activeSelf)
                FlashlightModel.transform.localPosition = flashlightModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);

            // ★追加：ライターの揺れ
            if (LighterModel != null && LighterModel.activeSelf)
                LighterModel.transform.localPosition = lighterModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
        }
        else
        {
            if (KeyModel.activeSelf) KeyModel.transform.localPosition = Vector3.Lerp(KeyModel.transform.localPosition, KeyModelDefaultPos, Time.deltaTime * 10f);
            if (ItemModel.activeSelf) ItemModel.transform.localPosition = Vector3.Lerp(ItemModel.transform.localPosition, itemModelDefaultPos, Time.deltaTime * 10f);

            if (FlashlightModel != null && FlashlightModel.activeSelf)
                FlashlightModel.transform.localPosition = Vector3.Lerp(FlashlightModel.transform.localPosition, flashlightModelDefaultPos, Time.deltaTime * 10f);

            // ★追加：ライターの位置戻し
            if (LighterModel != null && LighterModel.activeSelf)
                LighterModel.transform.localPosition = Vector3.Lerp(LighterModel.transform.localPosition, lighterModelDefaultPos, Time.deltaTime * 10f);

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

            if (KeyModel.activeSelf)
            {
                KeyModel.SetActive(false);
                if (inventoryManager != null && inventoryManager.currentItems.Count > 0)
                {
                    inventoryManager.currentItems.RemoveAt(0);
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
        if (currentX <= downLimit) { allowedDownAngle = 0f; }
        else { float margin = Mathf.InverseLerp(-90f, downLimit, currentX); allowedDownAngle *= margin; }

        float angle = 0f;
        if (cameraSwingTimer < 0.5f) { float t = cameraSwingTimer / 0.3f; t = Mathf.SmoothStep(0f, 1f, t); angle = Mathf.Lerp(0, cameraSwingUpAngle, t); }
        else if (cameraSwingTimer < 0.9f) { float t = (cameraSwingTimer - 0.3f) / 0.6f; t = Mathf.SmoothStep(0f, 1f, t); angle = Mathf.Lerp(cameraSwingUpAngle, allowedDownAngle, t); }
        else if (cameraSwingTimer < 1.5f) { float t = (cameraSwingTimer - 1f) / 0.6f; t = Mathf.SmoothStep(0f, 1f, t); angle = Mathf.Lerp(allowedDownAngle, 0, t); }
        else { cam.transform.localRotation = cameraSwingStartRot; isCameraSwing = false; cameraSwingTimer = 0f; return; }
        cam.transform.localRotation = cameraSwingStartRot * Quaternion.Euler(angle, 0, 0);
    }
}