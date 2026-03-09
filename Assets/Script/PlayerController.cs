using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;
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

    // --- ダッシュ（スタミナ）設定 ---
    [Header("ダッシュ設定")]
    [Tooltip("ダッシュできる最大時間（秒）")]
    public float maxDashDuration = 7.0f;
    [Tooltip("ダッシュをやめた後の回復待機時間（秒）")]
    public float dashRecoveryDelay = 2.0f;
    [Tooltip("0から100まで回復するのにかかる時間（秒）")]
    public float fullRecoveryDuration = 5.0f;

    // --- Inspector表示用 ---
    [Header("ダッシュ状態（デバッグ表示）")]
    [Tooltip("現在のスタミナ残量（%）")]
    public float currentStaminaPercent = 100f;
    [SerializeField, Tooltip("現在ダッシュ継続している秒数")]
    private float currentDashTime = 0f;
    [SerializeField, Tooltip("回復開始までの残り待機秒数")]
    private float recoveryDelayTimer = 0f;

    // 内部計算用
    private float currentStamina = 1.0f; // 0.0 ~ 1.0 で管理
    private bool isDashing = false;

    [Header("メインカメラを参照")]
    public GameObject cam;

    [Header("アイテム入手演出")]
    public ItemGetDisplay itemGetDisplay;

    [Header("InventoryManagerを参照")]
    public InventoryManager inventoryManager;

    [Tooltip("インベントリ用のBlurVolume")]
    public GameObject inventoryBlurVolume;

    [Header("疲労演出（視界のぼやけ）")]
    [Tooltip("疲労時に適用するPostProcessVolume（DepthOfFieldなどを設定してください）")]
    public PostProcessVolume fatigueBlurVolume;

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

    [Header("クロスヘアUI")]
    public GameObject crosshairUI;

    [Header("インベントリ開閉設定")]
    [Tooltip("インベントリ画面の親オブジェクト")]
    public GameObject inventoryUIPanel;
    [Tooltip("インベントリを開いている間、隠したいオブジェクト")]
    public GameObject uiToHideWhenInventoryOpen;

    [Header("デコイ")]
    [SerializeField] private GameObject decoy;
    [SerializeField] private float decoySpawnDistance = 2f;

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
    public GameObject Key1Model;
    public GameObject Key2Model;
    public GameObject Key3Model;
    public GameObject Key4Model;
    public GameObject Key5Model; // ★追加
    public GameObject ItemModel;
    public GameObject CrowbarModel;
    public GameObject FlashlightModel;
    public GameObject LighterModel;
    public GameObject SpiderModel;
    public GameObject DetergentModel;
    public GameObject rust_keyModel;
    public GameObject FrogModel;

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

    [Header("攻撃音")]
    public AudioClip crowbarSwingSE;
    public AudioSource attackAudioSource;

    [Header("吐息設定")]
    public AudioSource breathingAudioSource;
    public AudioClip breathingSoundLoop;
    [Range(0f, 1f)] public float breathingWalkVolume = 0.3f;
    [Range(0f, 1f)] public float breathingRunVolume = 0.5f;

    [Tooltip("走り終わった直後、最大まで疲労していた場合の音量")]
    [Range(0f, 1f)] public float breathingMaxVolume = 1.0f;

    [Tooltip("走り終わった後、元の音量に戻るまでにかかる時間（秒）")]
    public float breathingRecoveryTime = 3.0f;

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

    private Vector3 Key1ModelDefaultPos;
    private Vector3 Key2ModelDefaultPos;
    private Vector3 Key3ModelDefaultPos;
    private Vector3 Key4ModelDefaultPos;
    private Vector3 Key5ModelDefaultPos; // ★追加
    private Vector3 itemModelDefaultPos;
    private Vector3 crowbarModelDefaultPos;
    private Vector3 flashlightModelDefaultPos;
    private Vector3 lighterModelDefaultPos;
    private Vector3 spiderModelDefaultPos;
    private Vector3 detergentModelDefaultPos;
    private Vector3 rust_keyModelDefaultPos;
    private Vector3 frogModelDefaultPos;

    private Quaternion itemDefaultRot;
    private Quaternion crowbarDefaultRot;
    private Quaternion defaultRot;

    private bool canCrowbarSwing = true;
    private float crowbarCooldown = 1.5f;

    private float itemBobTimer = 0f;
    private bool isSwinging = false;
    private float swingTimer = 0f;

    private bool isItemSwing = false;
    private float itemSwingTimer = 0f;

    private bool isCrowbarSwing = false;
    private float crowbarSwingTimer = 0f;

    private bool isCameraSwing = false;
    private float cameraSwingTimer = -2f;
    private Quaternion cameraSwingStartRot;

    private float currentAudioFatigue = 0f;

    public DoubleDoorController DoubleDoor;
    Animator animator;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (inventoryBlurVolume != null) inventoryBlurVolume.SetActive(false);
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (inventoryUIPanel != null) inventoryUIPanel.SetActive(false);

        if (fatigueBlurVolume != null)
        {
            fatigueBlurVolume.weight = 0f;
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

        // 初期位置保存
        if (Key1Model != null) Key1ModelDefaultPos = Key1Model.transform.localPosition;
        if (Key2Model != null) Key2ModelDefaultPos = Key2Model.transform.localPosition;
        if (Key3Model != null) Key3ModelDefaultPos = Key3Model.transform.localPosition;
        if (Key4Model != null) Key4ModelDefaultPos = Key4Model.transform.localPosition;
        if (Key5Model != null) Key5ModelDefaultPos = Key5Model.transform.localPosition; // ★追加

        if (ItemModel != null)
        {
            itemModelDefaultPos = ItemModel.transform.localPosition;
            itemDefaultRot = ItemModel.transform.localRotation;
        }
        if (CrowbarModel != null)
        {
            crowbarModelDefaultPos = CrowbarModel.transform.localPosition;
            crowbarDefaultRot = CrowbarModel.transform.localRotation;
        }
        if (FlashlightModel != null) flashlightModelDefaultPos = FlashlightModel.transform.localPosition;
        if (LighterModel != null) lighterModelDefaultPos = LighterModel.transform.localPosition;

        if (SpiderModel != null) spiderModelDefaultPos = SpiderModel.transform.localPosition;
        if (rust_keyModel != null) rust_keyModelDefaultPos = rust_keyModel.transform.localPosition;
        if (FrogModel != null) frogModelDefaultPos = FrogModel.transform.localPosition;

        Invoke(nameof(UpdateItemModel), 0.1f);

        animator = GetComponent<Animator>();
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
            if (!isInventoryOpen && !canControl) return;
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

        HandleDashStamina(hasInput);
        HandleFatigueEffects(isMoving, isDashing);
        HandleFootsteps(isMoving, isDashing);
        HandleCameraShake();

        UpdateItemBob(isMoving);
        UpdateItemSwing();
        UpdateCrowbarSwing();
        UpdateCameraSwing();

        if (Input.GetMouseButtonDown(0))
        {
            if (!isInventoryOpen)
            {
                HandleAttackInput();
            }
        }
    }

    void HandleDashStamina(bool hasInput)
    {
        bool wantsToDash = Input.GetKey(KeyCode.R) && hasInput;

        if (wantsToDash && currentStamina > 0)
        {
            isDashing = true;

            float drainRate = 1.0f / maxDashDuration;
            currentStamina -= drainRate * Time.deltaTime;
            if (currentStamina < 0) currentStamina = 0;

            currentDashTime += Time.deltaTime;
            recoveryDelayTimer = dashRecoveryDelay;
        }
        else
        {
            isDashing = false;
            currentDashTime = 0f;

            if (recoveryDelayTimer > 0)
            {
                recoveryDelayTimer -= Time.deltaTime;
                if (recoveryDelayTimer < 0) recoveryDelayTimer = 0;
            }
            else
            {
                if (currentStamina < 1.0f)
                {
                    float recoveryRate = 1.0f / fullRecoveryDuration;
                    currentStamina += recoveryRate * Time.deltaTime;
                    if (currentStamina > 1.0f) currentStamina = 1.0f;
                }
            }
        }

        currentStaminaPercent = currentStamina * 100f;
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

        float currentSpeed = isDashing ? dashSpeed : walkSpeed;

        rb.velocity = new Vector3(moveDir.x * currentSpeed, rb.velocity.y, moveDir.z * currentSpeed);
    }

    public void HandleAttackInput()
    {
        // ★ Key1〜5のどれかがアクティブなら振る
        if ((Key1Model != null && Key1Model.activeSelf) ||
            (Key2Model != null && Key2Model.activeSelf) ||
            (Key3Model != null && Key3Model.activeSelf) ||
            (Key4Model != null && Key4Model.activeSelf) ||
            (Key5Model != null && Key5Model.activeSelf)) // ★追加
        {
            PlayKeySwing();
        }
        else if (ItemModel != null && ItemModel.activeSelf) PlayItemSwing();
        else if (CrowbarModel != null && CrowbarModel.activeSelf) PlayCrowbarSwing();
        else if (SpiderModel != null && SpiderModel.activeSelf) UseSpiderDecoy();
    }

    public void UseSpiderDecoy()
    {
        if (inventoryManager == null) return;

        if (!inventoryManager.IsDecoyReady())
        {
            Debug.Log("まだ使えません！クールダウン中");
            return;
        }

        if (decoy != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * decoySpawnDistance;
            Instantiate(decoy, spawnPos, Quaternion.identity);
            Debug.Log("🕷 クモを設置しました！");
        }

        inventoryManager.UseDecoy();
    }

    void ToggleInventory()
    {
        if (!isInventoryOpen && !canControl) return;

        isInventoryOpen = !isInventoryOpen;

        if (crosshairUI != null) crosshairUI.SetActive(!isInventoryOpen);

        if (inventoryBlurVolume != null)
        {
            inventoryBlurVolume.SetActive(isInventoryOpen);
            var vol = inventoryBlurVolume.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
            if (vol != null) vol.weight = isInventoryOpen ? 1f : 0f;
        }

        if (inventoryUIPanel != null) inventoryUIPanel.SetActive(isInventoryOpen);
        if (uiToHideWhenInventoryOpen != null) uiToHideWhenInventoryOpen.SetActive(!isInventoryOpen);

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
            if (vol != null) vol.weight = isActive ? 1f : 0f;
        }
    }

    void HandleCameraShake()
    {
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance);
        bool hasInput = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);

        if (isGrounded && hasInput)
        {
            float currentFrequency = isDashing ? runBobFrequency : walkBobFrequency;
            Vector2 currentAmount = isDashing ? runBobAmount : walkBobAmount;

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
        switch (command)
        {
            case "Title":
                SceneManager.LoadScene("TitleScene");
                break;

            case "Option":
                option.SetActive(true);
                pauseMenu.SetActive(false);
                backGround.fillAmount = 0;
                break;

            case "Save":
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.SaveGame();
                    Debug.Log("ゲームをセーブしました！");
                }
                else
                {
                    Debug.LogError("エラー: シーン上に SaveManager が見つかりません！");
                }
                break;

            case "Return":
                canControl = true;
                pauseMenu.SetActive(false);
                backGround.fillAmount = 0;
                panelAlpha(0);
                if (crosshairUI != null) crosshairUI.SetActive(true);
                break;

            case "Pause":
                option.SetActive(false);
                pauseMenu.SetActive(true);
                if (crosshairUI != null) crosshairUI.SetActive(false);
                break;
        }
    }

    public void backgroundTrue(float pos)
    {
        if (!canControl)
        {
            backGround.gameObject.transform.position = new Vector3(960, pos + 540, 0);
            StartCoroutine(animBackGround());
        }
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
        if (itemGetDisplay != null && itemGetDisplay.isDisplaying) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        pickUpText.enabled = false;

        // ★ Key5を追加
        string[] pickableTags = { "Item", "Key", "Key1", "Key2", "Key3", "Key4", "Key5", "Flashlight", "Lighter", "Crowbar", "Spider", "Detergent", "rust_key", "Frog" };

        if (Physics.Raycast(ray, out hit, pickUpDistance))
        {
            foreach (string t in pickableTags)
            {
                if (hit.collider.CompareTag(t))
                {
                    pickUpText.enabled = true;
                    if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                    {
                        string cleanName = hit.collider.gameObject.name.Replace("(Clone)", "").Trim();

                        inventoryManager.PickUpItem(hit.collider.gameObject);
                        UpdateItemModel();

                        if (itemGetDisplay != null)
                        {
                            itemGetDisplay.ShowItemGet(cleanName);
                        }
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

    void HandleFatigueEffects(bool isMoving, bool isDashing)
    {
        if (isDashing && isMoving)
        {
            if (maxDashDuration > 0)
                currentAudioFatigue += Time.deltaTime / maxDashDuration;
        }
        else
        {
            if (breathingRecoveryTime > 0)
                currentAudioFatigue -= Time.deltaTime / breathingRecoveryTime;
            else
                currentAudioFatigue = 0f;
        }
        currentAudioFatigue = Mathf.Clamp01(currentAudioFatigue);

        if (breathingAudioSource != null)
        {
            if (isMoving && !breathingAudioSource.isPlaying) breathingAudioSource.Play();

            float targetVolume = 0f;
            if (isDashing && isMoving)
            {
                targetVolume = breathingRunVolume;
            }
            else
            {
                float baseVolume = isMoving ? breathingWalkVolume : 0f;
                targetVolume = Mathf.Lerp(baseVolume, breathingMaxVolume, currentAudioFatigue);
            }

            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, targetVolume, Time.deltaTime * audioFadeSpeed);
        }

        if (fatigueBlurVolume != null)
        {
            fatigueBlurVolume.weight = currentAudioFatigue;
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
        if (inventoryManager == null) return;
        int targetIndex = inventoryManager.equippedIndex;
        if (targetIndex < 0 || targetIndex >= inventoryManager.currentItems.Count) return;

        string itemName = inventoryManager.currentItems[targetIndex];
        if (string.IsNullOrEmpty(itemName)) return;

        // ★ 非表示対応
        if (Key1Model) Key1Model.SetActive(false);
        if (Key2Model) Key2Model.SetActive(false);
        if (Key3Model) Key3Model.SetActive(false);
        if (Key4Model) Key4Model.SetActive(false);
        if (Key5Model) Key5Model.SetActive(false); // ★追加

        if (ItemModel) ItemModel.SetActive(false);
        if (CrowbarModel) CrowbarModel.SetActive(false);
        if (FlashlightModel) FlashlightModel.SetActive(false);
        if (LighterModel) LighterModel.SetActive(false);
        if (SpiderModel) SpiderModel.SetActive(false);
        if (DetergentModel) DetergentModel.SetActive(false);
        if (rust_keyModel) rust_keyModel.SetActive(false);
        if (FrogModel) FrogModel.SetActive(false);

        Vector3 dropPos = transform.position + (transform.up * 1.3f) + (transform.forward * 2.0f);
        GameObject droppedItem = inventoryManager.DropItem(itemName, dropPos);

        if (droppedItem)
        {
            Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                Vector3 throwForce = (transform.forward * 2f) + (Vector3.down * 5f);
                rb.AddForce(throwForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }

            Collider playerCollider = GetComponent<Collider>();
            Collider itemCollider = droppedItem.GetComponent<Collider>();
            if (playerCollider != null && itemCollider != null)
            {
                Physics.IgnoreCollision(playerCollider, itemCollider, true);
                StartCoroutine(ReenableCollision(playerCollider, itemCollider));
            }
        }
        UpdateItemModel();
    }

    private IEnumerator ReenableCollision(Collider pCol, Collider iCol)
    {
        yield return new WaitForSeconds(1.0f);
        if (pCol != null && iCol != null)
        {
            Physics.IgnoreCollision(pCol, iCol, false);
        }
    }

    public void UpdateItemModel()
    {
        if (Key1Model != null) Key1Model.SetActive(false);
        if (Key2Model != null) Key2Model.SetActive(false);
        if (Key3Model != null) Key3Model.SetActive(false);
        if (Key4Model != null) Key4Model.SetActive(false);
        if (Key5Model != null) Key5Model.SetActive(false); // ★追加

        if (ItemModel != null) ItemModel.SetActive(false);
        if (CrowbarModel != null) CrowbarModel.SetActive(false);
        if (FlashlightModel != null) FlashlightModel.SetActive(false);
        if (LighterModel != null) LighterModel.SetActive(false);
        if (SpiderModel != null) SpiderModel.SetActive(false);
        if (DetergentModel != null) DetergentModel.SetActive(false);
        if (rust_keyModel != null) rust_keyModel.SetActive(false);
        if (FrogModel != null) FrogModel.SetActive(false);

        if (inventoryManager == null || inventoryManager.currentItems.Count == 0) return;

        int targetIndex = inventoryManager.equippedIndex;
        if (targetIndex >= inventoryManager.currentItems.Count) return;

        string itemName = inventoryManager.currentItems[targetIndex];
        if (string.IsNullOrEmpty(itemName)) return;

        string tag = inventoryManager.GetItemTag(itemName);

        switch (tag)
        {
            case "Key": // 古い設定の互換用
            case "Key1": if (Key1Model != null) Key1Model.SetActive(true); break;
            case "Key2": if (Key2Model != null) Key2Model.SetActive(true); break;
            case "Key3": if (Key3Model != null) Key3Model.SetActive(true); break;
            case "Key4": if (Key4Model != null) Key4Model.SetActive(true); break;
            case "Key5": if (Key5Model != null) Key5Model.SetActive(true); break; // ★追加

            case "Crowbar": if (CrowbarModel != null) CrowbarModel.SetActive(true); break;
            case "Flashlight": if (FlashlightModel != null) FlashlightModel.SetActive(true); break;
            case "Lighter": if (LighterModel != null) LighterModel.SetActive(true); break;
            case "Item": if (ItemModel != null) ItemModel.SetActive(true); break;
            case "Spider": if (SpiderModel != null) SpiderModel.SetActive(true); break;
            case "Detergent": if (DetergentModel != null) DetergentModel.SetActive(true); break;
            case "rust_key": if (rust_keyModel != null) rust_keyModel.SetActive(true); break;
            case "Frog": if (FrogModel != null) FrogModel.SetActive(true); break;
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

            if (Key1Model != null && Key1Model.activeSelf) Key1Model.transform.localPosition = Key1ModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (Key2Model != null && Key2Model.activeSelf) Key2Model.transform.localPosition = Key2ModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (Key3Model != null && Key3Model.activeSelf) Key3Model.transform.localPosition = Key3ModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (Key4Model != null && Key4Model.activeSelf) Key4Model.transform.localPosition = Key4ModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (Key5Model != null && Key5Model.activeSelf) Key5Model.transform.localPosition = Key5ModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0); // ★追加

            if (ItemModel != null && ItemModel.activeSelf) ItemModel.transform.localPosition = itemModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (CrowbarModel != null && CrowbarModel.activeSelf) CrowbarModel.transform.localPosition = crowbarModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (FlashlightModel != null && FlashlightModel.activeSelf) FlashlightModel.transform.localPosition = flashlightModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (LighterModel != null && LighterModel.activeSelf) LighterModel.transform.localPosition = lighterModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (SpiderModel != null && SpiderModel.activeSelf) SpiderModel.transform.localPosition = spiderModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (rust_keyModel != null && rust_keyModel.activeSelf) rust_keyModel.transform.localPosition = rust_keyModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
            if (FrogModel != null && FrogModel.activeSelf) FrogModel.transform.localPosition = frogModelDefaultPos + new Vector3(bobOffsetX, bobOffsetY, 0);
        }
        else
        {
            if (Key1Model != null && Key1Model.activeSelf) Key1Model.transform.localPosition = Vector3.Lerp(Key1Model.transform.localPosition, Key1ModelDefaultPos, Time.deltaTime * 10f);
            if (Key2Model != null && Key2Model.activeSelf) Key2Model.transform.localPosition = Vector3.Lerp(Key2Model.transform.localPosition, Key2ModelDefaultPos, Time.deltaTime * 10f);
            if (Key3Model != null && Key3Model.activeSelf) Key3Model.transform.localPosition = Vector3.Lerp(Key3Model.transform.localPosition, Key3ModelDefaultPos, Time.deltaTime * 10f);
            if (Key4Model != null && Key4Model.activeSelf) Key4Model.transform.localPosition = Vector3.Lerp(Key4Model.transform.localPosition, Key4ModelDefaultPos, Time.deltaTime * 10f);
            if (Key5Model != null && Key5Model.activeSelf) Key5Model.transform.localPosition = Vector3.Lerp(Key5Model.transform.localPosition, Key5ModelDefaultPos, Time.deltaTime * 10f); // ★追加

            if (ItemModel != null && ItemModel.activeSelf) ItemModel.transform.localPosition = Vector3.Lerp(ItemModel.transform.localPosition, itemModelDefaultPos, Time.deltaTime * 10f);
            if (CrowbarModel != null && CrowbarModel.activeSelf) CrowbarModel.transform.localPosition = Vector3.Lerp(CrowbarModel.transform.localPosition, crowbarModelDefaultPos, Time.deltaTime * 10f);
            if (FlashlightModel != null && FlashlightModel.activeSelf) FlashlightModel.transform.localPosition = Vector3.Lerp(FlashlightModel.transform.localPosition, flashlightModelDefaultPos, Time.deltaTime * 10f);
            if (LighterModel != null && LighterModel.activeSelf) LighterModel.transform.localPosition = Vector3.Lerp(LighterModel.transform.localPosition, lighterModelDefaultPos, Time.deltaTime * 10f);
            if (SpiderModel != null && SpiderModel.activeSelf) SpiderModel.transform.localPosition = Vector3.Lerp(SpiderModel.transform.localPosition, spiderModelDefaultPos, Time.deltaTime * 10f);
            if (rust_keyModel != null && rust_keyModel.activeSelf) rust_keyModel.transform.localPosition = Vector3.Lerp(rust_keyModel.transform.localPosition, rust_keyModelDefaultPos, Time.deltaTime * 10f);
            if (FrogModel != null && FrogModel.activeSelf) FrogModel.transform.localPosition = Vector3.Lerp(FrogModel.transform.localPosition, frogModelDefaultPos, Time.deltaTime * 10f);
            itemBobTimer = 0f;
        }
    }

    public void UpdateKeySwing()
    {
        if (!isSwinging) return;
        swingTimer += Time.deltaTime * swingSpeed;
        float swingOffset = Mathf.Sin(swingTimer) * swingAmount;

        // ★ どのアクティブな鍵でも揺らす
        if (Key1Model != null && Key1Model.activeSelf) Key1Model.transform.localPosition = Key1ModelDefaultPos + new Vector3(0, 0, swingOffset);
        if (Key2Model != null && Key2Model.activeSelf) Key2Model.transform.localPosition = Key2ModelDefaultPos + new Vector3(0, 0, swingOffset);
        if (Key3Model != null && Key3Model.activeSelf) Key3Model.transform.localPosition = Key3ModelDefaultPos + new Vector3(0, 0, swingOffset);
        if (Key4Model != null && Key4Model.activeSelf) Key4Model.transform.localPosition = Key4ModelDefaultPos + new Vector3(0, 0, swingOffset);
        if (Key5Model != null && Key5Model.activeSelf) Key5Model.transform.localPosition = Key5ModelDefaultPos + new Vector3(0, 0, swingOffset); // ★追加

        if (swingTimer >= Mathf.PI)
        {
            isSwinging = false;
            if (Key1Model) { Key1Model.transform.localPosition = Key1ModelDefaultPos; Key1Model.SetActive(false); }
            if (Key2Model) { Key2Model.transform.localPosition = Key2ModelDefaultPos; Key2Model.SetActive(false); }
            if (Key3Model) { Key3Model.transform.localPosition = Key3ModelDefaultPos; Key3Model.SetActive(false); }
            if (Key4Model) { Key4Model.transform.localPosition = Key4ModelDefaultPos; Key4Model.SetActive(false); }
            if (Key5Model) { Key5Model.transform.localPosition = Key5ModelDefaultPos; Key5Model.SetActive(false); } // ★追加
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

    void UpdateCrowbarSwing()
    {
        if (!isCrowbarSwing || CrowbarModel == null) return;
        crowbarSwingTimer += Time.deltaTime * swingSpeed;

        if (crowbarSwingTimer < 0.25f)
        {
            float t = crowbarSwingTimer / 0.25f;
            t = Mathf.SmoothStep(0f, 1f, t);
            Vector3 windUpPos = crowbarModelDefaultPos + new Vector3(0.2f, 0.2f, -0.3f);
            CrowbarModel.transform.localPosition = Vector3.Lerp(crowbarModelDefaultPos, windUpPos, t);
            Quaternion windUpRot = Quaternion.Euler(10f, -80f, 100f);
            CrowbarModel.transform.localRotation = Quaternion.Lerp(crowbarDefaultRot, windUpRot, t);
            animator.SetTrigger("Attack");
        }
        else if (crowbarSwingTimer < 0.4f)
        {
            float t = (crowbarSwingTimer - 0.25f) / 0.15f;
            Vector3 windUpPos = crowbarModelDefaultPos + new Vector3(0.2f, 0.2f, -0.3f);
            Vector3 smashPos = crowbarModelDefaultPos + new Vector3(0f, -0.3f, 0.5f);
            CrowbarModel.transform.localPosition = Vector3.Lerp(windUpPos, smashPos, t);
            Quaternion windUpRot = Quaternion.Euler(10f, -80f, 100f);
            Quaternion smashRot = Quaternion.Euler(170f, -90f, 90f);
            CrowbarModel.transform.localRotation = Quaternion.Lerp(windUpRot, smashRot, t);
        }
        else if (crowbarSwingTimer < 1f)
        {
            float t = (crowbarSwingTimer - 0.4f) / 0.6f;
            t = Mathf.SmoothStep(0f, 1f, t);
            Vector3 smashPos = crowbarModelDefaultPos + new Vector3(0f, -0.3f, 0.5f);
            CrowbarModel.transform.localPosition = Vector3.Lerp(smashPos, crowbarModelDefaultPos, t);
            Quaternion smashRot = Quaternion.Euler(170f, -90f, 90f);
            CrowbarModel.transform.localRotation = Quaternion.Lerp(smashRot, crowbarDefaultRot, t);
        }
        else
        {
            CrowbarModel.transform.localPosition = crowbarModelDefaultPos;
            CrowbarModel.transform.localRotation = crowbarDefaultRot;
            isCrowbarSwing = false;
            crowbarSwingTimer = 0f;
        }
    }

    public async Task PlayKeySwing()
    {
        if (isSwinging) return;
        animator.SetTrigger("Opening");
        isSwinging = true;
        swingTimer = 0f;
        canLock = false;
        rb.velocity = Vector3.zero;
        isCameraSwing = false;
        cameraSwingTimer = 0f;
        await Task.Delay(2000);

        // ★ 使い終わったら非アクティブに
        if (Key1Model) Key1Model.SetActive(false);
        if (Key2Model) Key2Model.SetActive(false);
        if (Key3Model) Key3Model.SetActive(false);
        if (Key4Model) Key4Model.SetActive(false);
        if (Key5Model) Key5Model.SetActive(false); // ★追加
    }

    public void PlayItemSwing()
    {
        isItemSwing = true; itemSwingTimer = 0f; isCameraSwing = true; cameraSwingTimer = 0f;
        if (cam != null) cameraSwingStartRot = cam.transform.localRotation;
    }

    void ResetCrowbarCooldown()
    {
        canCrowbarSwing = true;
    }


    public void PlayCrowbarSwing()
    {
        if (!canCrowbarSwing) return;

        canCrowbarSwing = false;
        Invoke(nameof(ResetCrowbarCooldown), crowbarCooldown);

        if (crowbarSwingSE != null && attackAudioSource != null)
        {
            attackAudioSource.PlayOneShot(crowbarSwingSE);
        }
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
        cam.transform.localRotation = cameraRot * Quaternion.Euler(angle, 0, 0);
    }
}