using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

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

    [Tooltip("インベントリ用のBlurVolume")]
    public GameObject inventoryBlurVolume;

    [Header("プレイヤーが操作可能かどうか")]
    public bool canControl = true;

    [Header("感度設定")]
    public float Xsensityvity = 3f;
    public float Ysensityvity = 3f;

    [Header("UI関連")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject option;
    [SerializeField] private Image backGround;
    [SerializeField] private Image panel;
    public TMP_Text pickUpText;
    public float pickUpDistance = 3f;

    [Header("インベントリ開閉設定")]
    [Tooltip("インベントリ画面の親オブジェクト")]
    public GameObject inventoryUIPanel;
    [Tooltip("インベントリを開いている間、隠したいオブジェクト")]
    public GameObject uiToHideWhenInventoryOpen;

    [Header("デコイ")]
    [SerializeField] private GameObject decoy;
    [SerializeField] private float decoySpawnDistance;

    [Header("視点の揺れ")]
    public float walkBobFrequency = 10.0f;
    public Vector2 walkBobAmount = new Vector2(0.05f, 0.05f);
    public float runBobFrequency = 15.0f;
    public Vector2 runBobAmount = new Vector2(0.1f, 0.15f);
    public float bobSmoothing = 10.0f;

    [Header("共通設定")]
    public float groundCheckDistance = 0.5f;

    [Header("他スクリプト")]
    [SerializeField] SortStone[] sortStones;
    [SerializeField] SortPicture[] sortPictures;
    [SerializeField] BugSpawner bugSpawner;

    [Header("--- アイテムモデル設定 ---")]
    public GameObject KeyModel;
    public GameObject ItemModel; // 汎用アイテム
    public GameObject CrowbarModel; // ★追加：バール専用モデル
    public GameObject FlashlightModel;
    public GameObject LighterModel;

    [Header("アイテムアニメーション")]
    public float itemBobSpeed = 6f;
    public float itemBobAmount = 0.05f;
    public float swingAmount = 0.1f;
    public float swingSpeed = 1f;
    public float SwingUpAmount = 0.1f;
    public float SwingDownAmount = -0.25f;
    public float SwingUpRotation = -20f;
    public float SwingDownRotation = 60f;
    public float cameraSwingSpeed = 0.01f;
    public float cameraSwingUpAngle = -6f;
    public float cameraSwingDownAngle = 2f;

    [Header("--- オーディオ設定 ---")]
    public AudioSource footstepAudioSource;
    public AudioClip walkSoundLoop;
    public AudioClip runSoundLoop;
    public AudioSource breathingAudioSource;
    public AudioClip breathingSoundLoop;
    [Range(0f, 1f)] public float breathingWalkVolume = 0.3f;
    [Range(0f, 1f)] public float breathingRunVolume = 0.5f;
    public float audioFadeSpeed = 5.0f;

    // 内部変数
    private Vector3 defaultCamPos;
    private float camBobTimer = 0f;
    Quaternion cameraRot, characterRot;
    bool cursorLock = true;
    public bool canLock = true;
    public bool isInventoryOpen = false;
    float minX = -90f, maxX = 90f;
    Rigidbody rb;

    private Vector3 KeyModelDefaultPos;
    private Vector3 itemModelDefaultPos;
    private Vector3 crowbarModelDefaultPos; // ★追加
    private Vector3 flashlightModelDefaultPos;
    private Vector3 lighterModelDefaultPos;

    private Quaternion itemDefaultRot;
    private Quaternion crowbarDefaultRot; // ★追加
    private Quaternion defaultRot;

    private float itemBobTimer = 0f;
    private bool isSwinging = false; // Key用
    private float swingTimer = 0f;

    private bool isItemSwing = false; // 汎用アイテム用
    private float itemSwingTimer = 0f;

    private bool isCrowbarSwing = false; // ★追加：バール用
    private float crowbarSwingTimer = 0f; // ★追加

    private bool isCameraSwing = false;
    private float cameraSwingTimer = -2f;
    private Quaternion cameraSwingStartRot;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (inventoryBlurVolume != null)
        {
            inventoryBlurVolume.SetActive(false);
        }

        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();

        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(false);
        }

        if (cam != null)
        {
            cameraRot = cam.transform.localRotation;
            characterRot = transform.localRotation;
            defaultCamPos = cam.transform.localPosition;
        }

        if (footstepAudioSource == null) footstepAudioSource = GetComponent<AudioSource>();
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

        // 初期位置と回転の保存
        if (KeyModel != null) KeyModelDefaultPos = KeyModel.transform.localPosition;

        if (ItemModel != null)
        {
            itemModelDefaultPos = ItemModel.transform.localPosition;
            itemDefaultRot = ItemModel.transform.localRotation;
        }

        // ★追加：バールの初期位置保存
        if (CrowbarModel != null)
        {
            crowbarModelDefaultPos = CrowbarModel.transform.localPosition;
            crowbarDefaultRot = CrowbarModel.transform.localRotation;
        }

        if (FlashlightModel != null) flashlightModelDefaultPos = FlashlightModel.transform.localPosition;
        if (LighterModel != null) lighterModelDefaultPos = LighterModel.transform.localPosition;

        Invoke(nameof(UpdateItemModel), 0.1f);
    }

    private void OnDisable()
    {
        if (footstepAudioSource != null) footstepAudioSource.Stop();
        if (breathingAudioSource != null) breathingAudioSource.Stop();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log($"Tabキーが押されました。操作可能か: {canControl} / インベントリが開いているか: {isInventoryOpen}");

            if (!isInventoryOpen && !canControl)
            {
                Debug.Log("❌ 操作禁止中なので、インベントリを開くのをブロックしました。");
                return;
            }
            ToggleInventory();
        }

        if (!canControl)
        {
            FadeOutAudio();
            return;
        }

        if (isInventoryOpen)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canControl = false;
            FadeOutAudio();
            panelAlpha(1f);
            pauseMenu.SetActive(true);
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

            if (Input.GetKeyDown(KeyCode.Alpha1)) inventoryManager.ChangeSelectedSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) inventoryManager.ChangeSelectedSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) inventoryManager.ChangeSelectedSlot(2);
        }

        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        bool isMoving = isGrounded && hasInput;
        bool isRunning = Input.GetKey(KeyCode.R);

        HandleFootsteps(isMoving, isRunning);
        HandleBreathing(isMoving, isRunning);
        HandleCameraShake();

        UpdateItemBob(isMoving);
        UpdateKeySwing();
        UpdateItemSwing();
        UpdateCrowbarSwing(); // ★追加：バールの動き更新
        UpdateCameraSwing();

        if (Input.GetKeyDown(KeyCode.G))
        {
            Vector3 spawnPos = transform.position + transform.forward * decoySpawnDistance;
            Instantiate(decoy, spawnPos, Quaternion.identity);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            bugSpawner.SpawnBugs();
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleAttackInput();
        }
    }

    void ToggleInventory()
    {
        if (!isInventoryOpen && !canControl)
        {
            return;
        }

        isInventoryOpen = !isInventoryOpen;

        if (inventoryBlurVolume != null)
        {
            inventoryBlurVolume.SetActive(isInventoryOpen);

            // 環境に合わせて PostProcessVolume か Volume を選んでください
            var vol = inventoryBlurVolume.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
            if (vol != null)
            {
                vol.weight = isInventoryOpen ? 1f : 0f;
            }
        }

        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(isInventoryOpen);
        }

        if (uiToHideWhenInventoryOpen != null)
        {
            uiToHideWhenInventoryOpen.SetActive(!isInventoryOpen);
        }

        if (isInventoryOpen)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }

    public void SetBlurState(bool isActive)
    {
        if (inventoryBlurVolume != null)
        {
            inventoryBlurVolume.SetActive(isActive);
            var vol = inventoryBlurVolume.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
            if (vol != null)
            {
                vol.weight = isActive ? 1f : 0f;
            }
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
        if (cursorLock) { UnityEngine.Cursor.lockState = CursorLockMode.Locked; UnityEngine.Cursor.visible = false; }
        else { UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true; }
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
        bool saveState = false;
        switch (command)
        {
            case "Title": SceneManager.LoadScene("TitleScene"); break;
            case "Option": option.SetActive(true); pauseMenu.SetActive(false); backGround.fillAmount = 0; break;
            case "Save": saveState = true; Debug.Log("SaveGame"); break;
            case "Return": canControl = true; pauseMenu.SetActive(false); backGround.fillAmount = 0; panelAlpha(0); break;
            case "Pause": option.SetActive(false); pauseMenu.SetActive(true); break;
        }
    }

    public void backgroundTrue(float pos)
    {
        backGround.gameObject.transform.position = new Vector3(960, pos + 540, 0);
        StartCoroutine(animBackGround());
    }

    public void backgroundFalse()
    {
        backGround.fillAmount = 0;
    }

    private IEnumerator animBackGround()
    {
        backGround.gameObject.SetActive(true);
        backGround.fillAmount = 0;
        while (backGround.fillAmount < 1f)
        {
            backGround.fillAmount += 5 * Time.deltaTime;
            yield return null;
        }
    }

    private void panelAlpha(float alpha)
    {
        if (panel != null)
        {
            Color c = panel.color;
            c.a = Mathf.Clamp01(alpha);
            panel.color = c;
        }
    }

    void CheckPickUp()
    {
        if (isInventoryOpen) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        pickUpText.enabled = false;
        string[] pickableTags = { "Item", "Key", "Flashlight", "Lighter", "Crowbar" };

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
                        if (ObjectInArray(hit.collider.gameObject, sortStone.stones)) sortStone.Stone(hit.collider.gameObject);
                }
            }
            if (hit.collider.CompareTag("Picture"))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    foreach (SortPicture sortPicture in sortPictures)
                        if (ObjectInArray(hit.collider.gameObject, sortPicture.pictures)) sortPicture.Picture(hit.collider.gameObject);
                }
            }
        }
    }

    private bool ObjectInArray(GameObject obj, GameObject[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == obj) return true;
        }
        return false;
    }

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
            else if (!footstepAudioSource.isPlaying) footstepAudioSource.Play();
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
            if (footstepAudioSource.volume < 0.01f) footstepAudioSource.Pause();
        }
        if (breathingAudioSource != null && breathingAudioSource.isPlaying)
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
    }

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
        if (KeyModel != null) KeyModel.SetActive(false);
        if (ItemModel != null) ItemModel.SetActive(false);
        if (CrowbarModel != null) CrowbarModel.SetActive(false); // ★追加
        if (FlashlightModel != null) FlashlightModel.SetActive(false);
        if (LighterModel != null) LighterModel.SetActive(false);

        if (inventoryManager == null || inventoryManager.currentItems.Count == 0) return;

        int targetIndex = inventoryManager.equippedIndex;
        if (targetIndex >= inventoryManager.currentItems.Count) return;

        string itemName = inventoryManager.currentItems[targetIndex];

        if (string.IsNullOrEmpty(itemName)) return;

        string tag = inventoryManager.GetItemTag(itemName);

        switch (tag)
        {
            case "Key": if (KeyModel != null) KeyModel.SetActive(true); break;
            case "Crowbar": if (CrowbarModel != null) CrowbarModel.SetActive(true); break; // ★変更：バールなら専用モデルを表示
            case "Flashlight": if (FlashlightModel != null) FlashlightModel.SetActive(true); break;
            case "Lighter": if (LighterModel != null) LighterModel.SetActive(true); break;
            case "Item": if (ItemModel != null) ItemModel.SetActive(true); break;
            default: Debug.LogWarning($"タグ '{tag}' に対応するモデルなし"); break;
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

            // ★追加：バールのBob
            if (CrowbarModel != null && CrowbarModel.activeSelf) CrowbarModel.transform.localPosition = crowbarModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);

            if (FlashlightModel != null && FlashlightModel.activeSelf) FlashlightModel.transform.localPosition = flashlightModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (LighterModel != null && LighterModel.activeSelf) LighterModel.transform.localPosition = lighterModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
        }
        else
        {
            if (KeyModel != null && KeyModel.activeSelf) KeyModel.transform.localPosition = Vector3.Lerp(KeyModel.transform.localPosition, KeyModelDefaultPos, Time.deltaTime * 10f);
            if (ItemModel != null && ItemModel.activeSelf) ItemModel.transform.localPosition = Vector3.Lerp(ItemModel.transform.localPosition, itemModelDefaultPos, Time.deltaTime * 10f);

            // ★追加：バールの位置リセット
            if (CrowbarModel != null && CrowbarModel.activeSelf) CrowbarModel.transform.localPosition = Vector3.Lerp(CrowbarModel.transform.localPosition, crowbarModelDefaultPos, Time.deltaTime * 10f);

            if (FlashlightModel != null && FlashlightModel.activeSelf) FlashlightModel.transform.localPosition = Vector3.Lerp(FlashlightModel.transform.localPosition, flashlightModelDefaultPos, Time.deltaTime * 10f);
            if (LighterModel != null && LighterModel.activeSelf) LighterModel.transform.localPosition = Vector3.Lerp(LighterModel.transform.localPosition, lighterModelDefaultPos, Time.deltaTime * 10f);
            itemBobTimer = 0f;
        }
    }

    public void UpdateKeySwing()
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
            }
        }
    }

    /*void UpdateKeySwing()
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
    }*/

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

    /*void UpdateItemSwing()
    {
        if (!isItemSwing || ItemModel == null) return;
        itemSwingTimer += Time.deltaTime * swingSpeed;
        if (itemSwingTimer < 0.3f)
        {
            float t = itemSwingTimer / 0.3f;
            ItemModel.transform.localPosition = Vector3.Lerp(itemModelDefaultPos, itemModelDefaultPos + new Vector3(0, SwingUpAmount, 0), t);
            ItemModel.transform.localRotation = Quaternion.Lerp(itemDefaultRot, Quaternion.Euler(SwingUpRotation, 0, 0), t);
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
            ItemModel.transform.localRotation = itemDefaultRot;
            isItemSwing = false;
            itemSwingTimer = 0f;
        }
    }*/
    void UpdateCrowbarSwing()
    {
        if (!isCrowbarSwing || CrowbarModel == null) return;

        // スピード調整（数値を大きくすると早くなります）
        crowbarSwingTimer += Time.deltaTime * swingSpeed;

        // アニメーションのフェーズ管理
        // 0.0 ～ 0.25 : 振りかぶり（タメ）
        // 0.25 ～ 0.4 : フルスイング（インパクト）
        // 0.4 ～ 1.0 : 元に戻る（フォロースルー）

        if (crowbarSwingTimer < 0.25f)
        {
            // --- ① 振りかぶり（Wind Up） ---
            // 勢いをつけるために、手前に引いて上に持ち上げる
            float t = crowbarSwingTimer / 0.25f;
            t = Mathf.SmoothStep(0f, 1f, t); // 滑らかに

            // 位置：手前（-Z）かつ少し上（+Y）に引く
            Vector3 windUpPos = crowbarModelDefaultPos + new Vector3(0.2f, 0.2f, -0.3f);
            CrowbarModel.transform.localPosition = Vector3.Lerp(crowbarModelDefaultPos, windUpPos, t);

            // 回転：X90度から、X0度（真上～後ろ）付近まで起こす
            // デフォルト(90, -90, 90) → 振りかぶり(10, -80, 100) ※少しひねりを入れるとリアル
            Quaternion windUpRot = Quaternion.Euler(10f, -80f, 100f);
            CrowbarModel.transform.localRotation = Quaternion.Lerp(crowbarDefaultRot, windUpRot, t);
        }
        else if (crowbarSwingTimer < 0.4f)
        {
            // --- ② フルスイング（Smash） ---
            // 一気に振り下ろす（ここを一番速くする）
            float t = (crowbarSwingTimer - 0.25f) / 0.15f; // 0.15秒で振り抜く
            // t = t * t; // 加速させる（リニアより迫力が出る）

            // 位置：前に突き出す（+Z）かつ下げる（-Y）
            Vector3 windUpPos = crowbarModelDefaultPos + new Vector3(0.2f, 0.2f, -0.3f);
            Vector3 smashPos = crowbarModelDefaultPos + new Vector3(0f, -0.3f, 0.5f);
            CrowbarModel.transform.localPosition = Vector3.Lerp(windUpPos, smashPos, t);

            // 回転：X0度から、X160度（地面側）まで一気に回す
            // 振りかぶり(10, -80, 100) → 振り下ろし(170, -90, 90)
            Quaternion windUpRot = Quaternion.Euler(10f, -80f, 100f);
            Quaternion smashRot = Quaternion.Euler(170f, -90f, 90f);
            CrowbarModel.transform.localRotation = Quaternion.Lerp(windUpRot, smashRot, t);
        }
        else if (crowbarSwingTimer < 1f)
        {
            // --- ③ 元に戻る（Recovery） ---
            // 余韻を残しつつ元の位置へ
            float t = (crowbarSwingTimer - 0.4f) / 0.6f;
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 smashPos = crowbarModelDefaultPos + new Vector3(0f, -0.3f, 0.5f);
            CrowbarModel.transform.localPosition = Vector3.Lerp(smashPos, crowbarModelDefaultPos, t);

            Quaternion smashRot = Quaternion.Euler(170f, -90f, 90f);
            CrowbarModel.transform.localRotation = Quaternion.Lerp(smashRot, crowbarDefaultRot, t);
        }
        else
        {
            // 終了
            CrowbarModel.transform.localPosition = crowbarModelDefaultPos;
            CrowbarModel.transform.localRotation = crowbarDefaultRot;
            isCrowbarSwing = false;
            crowbarSwingTimer = 0f;
        }
    }

    public void HandleAttackInput()
    {
        if (KeyModel != null && KeyModel.activeSelf) PlayKeySwing();
        else if (ItemModel != null && ItemModel.activeSelf) PlayItemSwing();
        else if (CrowbarModel != null && CrowbarModel.activeSelf) PlayCrowbarSwing(); // ★追加
    }

    public void PlayKeySwing()
    {
        if (isSwinging) return;
        isSwinging = true;
        swingTimer = 0f;

        canLock = false;
        rb.velocity = Vector3.zero;
        isCameraSwing = false;
        cameraSwingTimer = 0f;
    }


    /*public void PlayKeySwing()
    {
        isSwinging = true; swingTimer = 0f; isCameraSwing = false; cameraSwingTimer = 0f;
    }*/
    public void PlayItemSwing()
    {
        isItemSwing = true; itemSwingTimer = 0f; isCameraSwing = true; cameraSwingTimer = 0f;
        if (cam != null) cameraSwingStartRot = cam.transform.localRotation;
    }

    // ★追加：バール用スイング開始
    public void PlayCrowbarSwing()
    {
        isCrowbarSwing = true; crowbarSwingTimer = 0f; isCameraSwing = true; cameraSwingTimer = 0f;
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
        if (currentX <= downLimit) allowedDownAngle = 0f;
        else { float margin = Mathf.InverseLerp(-90f, downLimit, currentX); allowedDownAngle *= margin; }

        float angle = 0f;
        if (cameraSwingTimer < 0.5f) { float t = cameraSwingTimer / 0.3f; t = Mathf.SmoothStep(0f, 1f, t); angle = Mathf.Lerp(0, cameraSwingUpAngle, t); }
        else if (cameraSwingTimer < 0.9f) { float t = (cameraSwingTimer - 0.3f) / 0.6f; t = Mathf.SmoothStep(0f, 1f, t); angle = Mathf.Lerp(cameraSwingUpAngle, allowedDownAngle, t); }
        else if (cameraSwingTimer < 1.5f) { float t = (cameraSwingTimer - 1f) / 0.6f; t = Mathf.SmoothStep(0f, 1f, t); angle = Mathf.Lerp(allowedDownAngle, 0, t); }
        else { cam.transform.localRotation = cameraSwingStartRot; isCameraSwing = false; cameraSwingTimer = 0f; return; }
        cam.transform.localRotation = cameraSwingStartRot * Quaternion.Euler(angle, 0, 0);
    }
}