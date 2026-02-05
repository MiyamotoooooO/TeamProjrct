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
    public GameObject ItemModel;
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
    private Vector3 flashlightModelDefaultPos;
    private Vector3 lighterModelDefaultPos;
    private Quaternion defaultRot;

    private float itemBobTimer = 0f;
    private bool isSwinging = false;
    private float swingTimer = 0f;
    private bool isItemSwing = false;
    private float itemSwingTimer = 0f;
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

        if (KeyModel != null) KeyModelDefaultPos = KeyModel.transform.localPosition;
        if (ItemModel != null)
        {
            itemModelDefaultPos = ItemModel.transform.localPosition;
            defaultRot = ItemModel.transform.localRotation;
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
            // デバッグ用ログ：今の状態を表示
            Debug.Log($"Tabキーが押されました。操作可能か: {canControl} / インベントリが開いているか: {isInventoryOpen}");

            // 条件チェック：
            // 「インベントリが閉じている」かつ「操作禁止」なら、絶対に開かせない
            if (!isInventoryOpen && !canControl)
            {
                Debug.Log("❌ 操作禁止中なので、インベントリを開くのをブロックしました。");
                return; // ここで処理終了！
            }

            // ここまで来たら開閉OK
            ToggleInventory();
        }

        if (!canControl)
        {
            FadeOutAudio();
            return;
        }

        // インベントリ開閉時のカーソル制御
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
            panelAlpha(1f); // 90ではなく1.0fがマックスです
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

            // ★キー入力でスロット切り替え
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
        // 「今閉じている」かつ「操作禁止（寝ている/イベント中）」なら、ここで強制終了！
        if (!isInventoryOpen && !canControl)
        {
            return;
        }

        isInventoryOpen = !isInventoryOpen;

        if (inventoryBlurVolume != null)
        {
            inventoryBlurVolume.SetActive(isInventoryOpen);

            // Volume ではなく PostProcessVolume を使う
            PostProcessVolume vol = inventoryBlurVolume.GetComponent<PostProcessVolume>();
            if (vol != null)
            {
                vol.weight = isInventoryOpen ? 1f : 0f;
            }
        }

        // インベントリ画面の表示/非表示
        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(isInventoryOpen);
        }

        // 隠したいオブジェクト（真ん中の点など）の表示/非表示
        if (uiToHideWhenInventoryOpen != null)
        {
            uiToHideWhenInventoryOpen.SetActive(!isInventoryOpen);
        }

        // カーソル制御
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

        /*isInventoryOpen = !isInventoryOpen;

        // インベントリ画面の表示/非表示
        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(isInventoryOpen);
        }

        // 隠したいオブジェクトの表示/非表示
        if (uiToHideWhenInventoryOpen != null)
        {
            // インベントリが開いているなら隠す、閉じてるなら出す
            uiToHideWhenInventoryOpen.SetActive(!isInventoryOpen);
        }

        // カーソル制御
        if (isInventoryOpen)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }*/
    }

    public void SetBlurState(bool isActive)
    {
        if (inventoryBlurVolume != null)
        {
            inventoryBlurVolume.SetActive(isActive);

            // ▼▼▼ パターンA：PostProcessVolumeを使っている場合 ▼▼▼
            var vol = inventoryBlurVolume.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
            if (vol != null)
            {
                vol.weight = isActive ? 1f : 0f;
            }
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            // ▼▼▼ パターンB：Volumeを使っている場合（URPなど） ▼▼▼
            /*
            var vol = inventoryBlurVolume.GetComponent<UnityEngine.Rendering.Volume>();
            if (vol != null)
            {
                vol.weight = isActive ? 1f : 0f;
            }
            */
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
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

    // --- メソッド群 ---

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
            case "Crowbar": if (ItemModel != null) ItemModel.SetActive(true); break;
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
            if (FlashlightModel != null && FlashlightModel.activeSelf) FlashlightModel.transform.localPosition = flashlightModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (LighterModel != null && LighterModel.activeSelf) LighterModel.transform.localPosition = lighterModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
        }
        else
        {
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
        isSwinging = true; swingTimer = 0f; isCameraSwing = false; cameraSwingTimer = 0f;
    }
    public void PlayItemSwing()
    {
        isItemSwing = true; itemSwingTimer = 0f; isCameraSwing = true; cameraSwingTimer = 0f;
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


/*using TMPro;
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

        if (isInventoryOpen)
        {
            // カーソルを表示してロック解除
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // これ以降の処理（カメラ回転やアイテム使用）をさせないためにここでリターン
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canControl = false;
            FadeOutAndPause();
            pauseMenu.SetActive(true);
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
        //if (Input.GetKey(KeyCode.Escape))
        //{
          //  canControl = false;
            //FadeOutAndPause();
            //pauseMenu.SetActive(true);
        //}

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
        }
    }

    void CheckHitStone()
    {
        //playerの見ている方向にRayを飛ばす
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction, Color.red);

        if (Physics.Raycast(ray, out hit, pickUpDistance))
        {
            if (hit.collider.CompareTag("Stone"))
            {
                //左クリックされたなら
                if (Input.GetMouseButtonDown(0))
                {
                    //SortStoneの関数を呼ぶ
                    sortStone.Stone(hit.collider.gameObject);
                }
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
        //string firstItem = inventoryManager.GetItemTag(firstItem);
        string tag = inventoryManager.GetItemTag(firstItem);

        // --- ★原因究明用のログ（解決したら消してOK）---
        // 拾ったアイテムのレイヤー番号 vs Lighterレイヤーの正解番号
        Debug.Log($"【判定中】アイテム名: {firstItem}");
        Debug.Log($" → アイテムのタグ: {tag}");

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
                if (ItemModel != null) FlashlightModel.SetActive(true);
                break;
            case "Lighter":
                if (LighterModel != null) LighterModel.SetActive(true);
                break;
            default:
                Debug.LogWarning($"未対応のタグです: {tag}");
                break;
        }
        // ----------------------------------------------

        // 2. 判定処理

        Debug.Log($"[判定中]アイテム名: {firstItem}");
        Debug.Log($" → アイテムのタグ: {tag}");
        // ★ライター判定 (ここが通るかどうか、上のログの番号が一致しているか見てください)
        if (tag == "Lighter")
        {
            Debug.Log("　→ 判定成功！ライターを表示します。");
            if (LighterModel != null) LighterModel.SetActive(true);
        }
        // もしレイヤーがダメでも、名前で救済する処理（保険）
        else if (tag == "Lighter")
        {
            Debug.Log("　→ レイヤーは違いましたが、名前でライターと判断しました。");
            if (LighterModel != null) LighterModel.SetActive(true);
        }
        // --- その他のアイテム ---
        else if (tag == "Key")
        {
            if (KeyModel != null) KeyModel.SetActive(true);
        }
        else if (tag == "Flashlight")
        {
            if (FlashlightModel != null) FlashlightModel.SetActive(true);
        }
        else if (tag == "Crowbar" || tag == "Item")
        {
            if (ItemModel != null) ItemModel.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"未対応のタグです: {tag}");
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
}*/