using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    float x, z;

    [Header("歩く速度")]
    public float walkSpeed = 5.0f;

    [Header("走る速度")]
    public float dashSpeed = 10.0f;

    [Header("メインカメラを参照")]
    public GameObject cam;

    [Header("InventoryManagerを参照")]
    public InventoryManager inventoryManager;

    [Header("プレイヤーが操作可能かどうか")]
    public bool canControl = true;

    [Header("感度設定(x軸)")]
    public float Xsensityvity = 3f;

    [Header("感度設定(y軸)")]
    public float Ysensityvity = 3f;

    [Header("拾うアイテムのすべてのLayer")]
    public LayerMask itemLayer;

    [Header("ポーズ画面関連")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject option;
    private bool save;

    [Header("拾うUI")]
    public TMP_Text pickUpText;
    public float pickUpDistance = 3f;

    [Header("デコイ")]
    [SerializeField] private GameObject decoy;
    [SerializeField] private float decoySpawnDistance;

    [Header("--- 視点の揺れ（Head Bob） ---")]
    public float walkBobFrequency = 10.0f;
    public Vector2 walkBobAmount = new Vector2(0.05f, 0.05f);
    public float runBobFrequency = 15.0f;
    public Vector2 runBobAmount = new Vector2(0.1f, 0.15f);
    public float bobSmoothing = 10.0f;

    [Header("共通設定")]
    public float groundCheckDistance = 0.5f;

    [Header("SortStoneスクリプト")]
    [SerializeField] SortStone[] sortStones;

    [Header("SortPictureスクリプト")]
    [SerializeField] SortPicture[] sortPictures;

    [Header("--- アイテムモデル設定 ---")]
    [Header("鍵モデル")]
    public GameObject KeyModel;

    [Header("アイテムモデル")]
    public GameObject ItemModel;

    [Header("懐中電灯モデル")]
    public GameObject FlashlightModel;

    [Header("ライターモデル")]
    public GameObject LighterModel;

    [Header("アイテムの揺れる速さ")]
    public float itemBobSpeed = 6f;

    [Header("アイテムの揺れる幅")]
    public float itemBobAmount = 0.05f;

    [Header("振った際のz軸の深さ")]
    public float swingAmount = 0.1f;

    [Header("振るスピード")]
    public float swingSpeed = 1f;

    [Header("振り上げ位置")]
    public float SwingUpAmount = 0.1f;

    [Header("振り下ろし位置")]
    public float SwingDownAmount = -0.25f;

    [Header("振り上げ角度")]
    public float SwingUpRotation = -20f;

    [Header("振り下ろし角度")]
    public float SwingDownRotation = 60f;

    [Header("カメラの追従速度")]
    public float cameraSwingSpeed = 0.01f;

    [Header("振り上げ時のカメラ角度")]
    public float cameraSwingUpAngle = -6f;

    [Header("振り下ろし時のカメラ角度")]
    public float cameraSwingDownAngle = 2f;

    [Header("--- オーディオ設定 ---")]
    [Header("足音用AudioSource")]
    public AudioSource footstepAudioSource;

    [Header("歩く際のAudio")]
    public AudioClip walkSoundLoop;

    [Header("走る際のAudio")]
    public AudioClip runSoundLoop;

    [Header("吐息用AudioSource")]
    public AudioSource breathingAudioSource;

    [Header("吐息のAudio")]
    public AudioClip breathingSoundLoop;

    [Header("歩いてるときの吐息音量")]
    [Range(0f, 1f)]
    public float breathingWalkVolume = 0.3f;

    [Header("走ってるときの吐息音量")]
    [Range(0f, 1f)]
    public float breathingRunVolume = 0.5f;

    [Header("共通オーディオフェード速度")]
    public float audioFadeSpeed = 5.0f;


    // =================================================================
    // 内部変数エリア
    // =================================================================

    // カメラ制御用
    private Vector3 defaultCamPos;
    private float camBobTimer = 0f;
    Quaternion cameraRot, characterRot;
    bool cursorLock = true;
    public bool canLock = true;
    public bool isInventoryOpen = false;
    float minX = -90f, maxX = 90f;
    Rigidbody rb;

    // アイテムモデル制御用
    private Vector3 KeyModelDefaultPos;
    private Vector3 itemModelDefaultPos;
    private Vector3 flashlightModelDefaultPos;
    private Vector3 lighterModelDefaultPos;
    private Quaternion defaultRot;

    // アニメーション用
    private float itemBobTimer = 0f;
    private bool isSwinging = false;
    private float swingTimer = 0f;
    private bool isItemSwing = false;
    private float itemSwingTimer = 0f;
    private bool isCameraSwing = false;
    private float cameraSwingTimer = -2f;
    private Quaternion cameraSwingStartRot;

    // =================================================================
    // Unity イベント関数
    // =================================================================

    private void Start()
    {
        // ------------------------
        // 基本コンポーネント取得
        // ------------------------
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();

        // カメラ初期化
        if (cam != null)
        {
            cameraRot = cam.transform.localRotation;
            characterRot = transform.localRotation;
            defaultCamPos = cam.transform.localPosition;
        }

        // ------------------------
        // オーディオ初期化
        // ------------------------
        if (footstepAudioSource == null)
            footstepAudioSource = GetComponent<AudioSource>();

        if (footstepAudioSource != null)
        {
            footstepAudioSource.loop = true;
            footstepAudioSource.volume = 0;
        }

        if (breathingAudioSource != null && breathingSoundLoop != null)
        {
            breathingAudioSource.clip = breathingSoundLoop;
            breathingAudioSource.loop = true;
            breathingAudioSource.volume = 0;
            breathingAudioSource.Play();
        }

        // ------------------------
        // アイテムモデル初期位置保存
        // ------------------------
        if (KeyModel != null) KeyModelDefaultPos = KeyModel.transform.localPosition;
        if (ItemModel != null)
        {
            itemModelDefaultPos = ItemModel.transform.localPosition;
            defaultRot = ItemModel.transform.localRotation;
        }
        if (FlashlightModel != null) flashlightModelDefaultPos = FlashlightModel.transform.localPosition;
        if (LighterModel != null) lighterModelDefaultPos = LighterModel.transform.localPosition;
    }

    private void OnDisable()
    {
        if (footstepAudioSource != null) footstepAudioSource.Stop();
        if (breathingAudioSource != null) breathingAudioSource.Stop();
    }

    private void Update()
    {
        // 操作不能時の処理
        if (!canControl)
        {
            FadeOutAudio();
            return;
        }

        // インベントリが開いている時
        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // ポーズ処理
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canControl = false;
            FadeOutAudio();
            pauseMenu.SetActive(true);
            return;
        }

        // カメラ回転
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

        // アイテム操作
        if (!isInventoryOpen)
        {
            CheckPickUp(); // アイテム拾い・石・絵画の判定
            if (Input.GetKeyDown(KeyCode.Q)) DropCurrentItem();
        }

        // 移動状態の計算
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        bool isMoving = isGrounded && hasInput;
        bool isRunning = Input.GetKey(KeyCode.R);

        // オーディオ更新
        HandleFootsteps(isMoving, isRunning);
        HandleBreathing(isMoving, isRunning);

        // カメラの揺れ（Head Bob）
        HandleCameraShake();

        // アイテムの揺れとアニメーション
        UpdateItemBob(isMoving);
        UpdateKeySwing();
        UpdateItemSwing();
        UpdateCameraSwing();

        // デコイ生成（デバッグ機能？）
        if (Input.GetKeyDown(KeyCode.G))
        {
            Vector3 spawnPos = transform.position + transform.forward * decoySpawnDistance;
            Instantiate(decoy, spawnPos, Quaternion.identity);
        }

        // 攻撃・使用アクション
        if (Input.GetMouseButtonDown(0))
        {
            HandleAttackInput();
        }
    }

    private void FixedUpdate()
    {
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

    // =================================================================
    // 移動・カメラ制御メソッド
    // =================================================================

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

    // =================================================================
    // インタラクション（拾うなど）メソッド
    // =================================================================

    void CheckPickUp()
    {
        if (isInventoryOpen) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction, Color.red);

        pickUpText.enabled = false;
        string[] pickableTags = { "Item", "Key", "Flashlight", "Lighter", "Crowber" };

        if (Physics.Raycast(ray, out hit, pickUpDistance))
        {
            foreach (string t in pickableTags)
            {
                if (hit.collider.CompareTag(t))
                {
                    pickUpText.enabled = true;

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        inventoryManager.PickUpItem(hit.collider.gameObject);
                        UpdateItemModel();
                    }
                    return;
                }
            }
            if (hit.collider.CompareTag("Stone"))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    foreach (SortStone sortStone in sortStones)
                    {
                        if (ObjectInArray(hit.collider.gameObject, sortStone.stones))
                            sortStone.Stone(hit.collider.gameObject);
                    }
                }
            }
            if (hit.collider.CompareTag("Picture"))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    foreach (SortPicture sortPicture in sortPictures)
                    {
                        if (ObjectInArray(hit.collider.gameObject, sortPicture.pictures))
                            sortPicture.Picture(hit.collider.gameObject);
                    }
                }
            }
        }
    }

    private bool ObjectInArray(GameObject obj, GameObject[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == obj)
                return true;
        }
        return false;
    }

    // =================================================================
    // オーディオ制御メソッド (旧 PlayerAudioController)
    // =================================================================

    void HandleFootsteps(bool isMoving, bool isRunning)
    {
        if (footstepAudioSource == null) return;

        if (isMoving)
        {
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

    void HandleBreathing(bool isMoving, bool isRunning)
    {
        if (breathingAudioSource == null) return;

        if (isMoving)
        {
            if (!breathingAudioSource.isPlaying) breathingAudioSource.Play();

            float targetVolume = isRunning ? breathingRunVolume : breathingWalkVolume;
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, targetVolume, Time.deltaTime * audioFadeSpeed);
        }
        else
        {
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
        }
    }

    public void FadeOutAudio()
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

    // =================================================================
    // アイテムモデル・アニメーション制御メソッド (旧 PlayerItemConnection)
    // =================================================================

    public void DropCurrentItem()
    {
        if (inventoryManager == null || inventoryManager.currentItems.Count == 0) return;

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
        string tag = inventoryManager.GetItemTag(firstItem);

        // タグでモデルを切り替える
        switch (tag)
        {
            case "Key":
                if (KeyModel != null) KeyModel.SetActive(true);
                break;
            case "Crowbar":
                if (ItemModel != null) ItemModel.SetActive(true);
                break;
            case "Flashlight":
                if (FlashlightModel != null) FlashlightModel.SetActive(true);
                break;
            case "Lighter":
                if (LighterModel != null) LighterModel.SetActive(true);
                break;
            case "Item":
                if (ItemModel != null) ItemModel.SetActive(true);
                break;
            default:
                if (tag == "Lighter" && LighterModel != null) LighterModel.SetActive(true);
                else Debug.LogWarning($"未対応のタグです: {tag}");
                break;
        }
    }

    void UpdateItemBob(bool isMoving)
    {
        if (isMoving)
        {
            itemBobTimer += Time.deltaTime * itemBobSpeed;
            float bobOffsetY = Mathf.Sin(itemBobTimer) * itemBobAmount;
            float bobOffsetX = Mathf.Cos(itemBobTimer * 0.5f) * itemBobAmount;

            if (KeyModel != null && KeyModel.activeSelf) KeyModel.transform.localPosition = KeyModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (ItemModel != null && ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (FlashlightModel != null && FlashlightModel.activeSelf) FlashlightModel.transform.localPosition = flashlightModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (LighterModel != null && LighterModel.activeSelf) LighterModel.transform.localPosition = lighterModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
        }
        else
        {
            // 元の位置に戻す補間
            if (KeyModel != null && KeyModel.activeSelf) KeyModel.transform.localPosition = Vector3.Lerp(KeyModel.transform.localPosition, KeyModelDefaultPos, Time.deltaTime * 10f);
            if (ItemModel != null && ItemModel.activeSelf) ItemModel.transform.localPosition = Vector3.Lerp(ItemModel.transform.localPosition, itemModelDefaultPos, Time.deltaTime * 10f);
            if (FlashlightModel != null && FlashlightModel.activeSelf) FlashlightModel.transform.localPosition = Vector3.Lerp(FlashlightModel.transform.localPosition, flashlightModelDefaultPos, Time.deltaTime * 10f);
            if (LighterModel != null && LighterModel.activeSelf) LighterModel.transform.localPosition = Vector3.Lerp(LighterModel.transform.localPosition, lighterModelDefaultPos, Time.deltaTime * 10f);

            itemBobTimer = 0f;
        }
    }

    void UpdateKeySwing()
    {
        if (!isSwinging) return;
        swingTimer += Time.deltaTime * swingSpeed;
        float swingOffset = Mathf.Sin(swingTimer) * swingAmount;

        if (KeyModel != null && KeyModel.activeSelf) KeyModel.transform.localPosition = KeyModelDefaultPos + new Vector3(0, 0, swingOffset);
        if (ItemModel != null && ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos + new Vector3(0, 0, swingOffset);

        if (swingTimer >= Mathf.PI)
        {
            isSwinging = false;
            if (KeyModel != null && KeyModel.activeSelf) KeyModel.transform.localPosition = KeyModelDefaultPos;
            if (ItemModel != null && ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos;

            // 鍵を使用した際のアイテム消費
            if (KeyModel != null && KeyModel.activeSelf)
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
        if (!isItemSwing || ItemModel == null) return;

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

    public void HandleAttackInput()
    {
        if (KeyModel != null && KeyModel.activeSelf) PlayKeySwing();
        else if (ItemModel != null && ItemModel.activeSelf) PlayItemSwing();
    }

    public void PlayKeySwing()
    {
        isSwinging = true;
        swingTimer = 0f;
        isCameraSwing = false;
        cameraSwingTimer = 0f;
    }

    public void PlayItemSwing()
    {
        isItemSwing = true;
        itemSwingTimer = 0f;
        isCameraSwing = true;
        cameraSwingTimer = 0f;
        if (cam != null) cameraSwingStartRot = cam.transform.localRotation;
    }

    void UpdateCameraSwing()
    {
        if (!isCameraSwing || cam == null) return;

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
