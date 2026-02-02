using UnityEngine;
using System.Collections.Generic;

public class PlayerItemConnection : MonoBehaviour
{
    [Header("メインカメラ")]
    public GameObject cam;

    [Header("InventoryManagerを参照")]
    public InventoryManager inventoryManager;

    [Header("鍵モデル")]
    public GameObject KeyModel;

    [Header("アイテムモデル")]
    public GameObject ItemModel;

    [Header("懐中電灯モデル")]
    public GameObject FlashlightModel;

    [Header("ライターモデル")]
    public GameObject LighterModel;

    [Header("揺れる速さ")]
    public float bobSpeed = 6f;

    [Header("揺れる幅")]
    public float bobAmount = 0.05f;

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

    // private
    private Vector3 KeyModelDefaultPos; //Keyの最初の位置を保存
    private Vector3 itemModelDefaultPos; // Itemの最初の位置を保存
    private Vector3 flashlightModelDefaultPos; // Flashlightの最初の位置を保存
    private Vector3 lighterModelDefaultPos; // Lighterの最初の位置を保存
    private Quaternion defaultRot; // アイテムの最初の角度を保存

    // アニメーション用変数
    private float bobTimer = 0f; // 揺れのリズム用タイマー
    private bool isSwinging = false; // 今Keyを使っているかのフラグ
    private float swingTimer = 0f; // 振り始めてから何秒経ったかを計る
    private bool isItemSwing = false; // 今Itemを振っているかのフラグ
    private float itemSwingTimer = 0f; // Itemを振るアニメーションのストップウォッチ

    private bool isCameraSwing = false; // 今視点を揺らす演出中かのフラグ
    private float cameraSwingTimer = -2f; // カメラの揺れのストップウォッチ
    private Quaternion cameraSwingStartRot; // 振り始めたカメラの角度をメモする場所

    private void Start()
    {
        // 依存関係の自動取得（もしInspectorで設定されていなければ）
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();

        // 初期位置の保存
        if (KeyModel != null) KeyModelDefaultPos = KeyModel.transform.localPosition;
        if (ItemModel != null)
        {
            itemModelDefaultPos = ItemModel.transform.localPosition;
            defaultRot = ItemModel.transform.localRotation;
        }
        if (FlashlightModel != null) flashlightModelDefaultPos = FlashlightModel.transform.localPosition;
        if (LighterModel != null) lighterModelDefaultPos = LighterModel.transform.localPosition;
    }

    private void Update()
    {
        // アニメーションの更新処理
        UpdateItemBob();
        UpdateKeySwing();
        UpdateItemSwing();
        UpdateCameraSwing();
    }

    // アイテムを捨てる
    public void DropCurrentItem()
    {
        if (inventoryManager == null || inventoryManager.currentItems.Count == 0) return;

        string itemName = inventoryManager.currentItems[0];
        Vector3 dropPos = transform.position + transform.forward * 1f;
        inventoryManager.DropItem(itemName, dropPos);

        UpdateItemModel();
    }

    // モデルの表示切り替え
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
                if (FlashlightModel != null) FlashlightModel.SetActive(true); // 変数名修正: FlashlightModelを使用
                break;
            case "Lighter":
                if (LighterModel != null) LighterModel.SetActive(true);
                break;
            case "Item": // Itemタグの場合もCrowbar(ItemModel)を表示する仕様の場合
                if (ItemModel != null) ItemModel.SetActive(true);
                break;
            default:
                // 名前で救済する処理などが必要ならここに記述
                if (tag == "Lighter" && LighterModel != null) LighterModel.SetActive(true);
                else Debug.LogWarning($"未対応のタグです: {tag}");
                break;
        }
    }

    // 移動時の揺れ
    void UpdateItemBob()
    {
        bool isMoving = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffsetY = Mathf.Sin(bobTimer) * bobAmount;
            float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmount;

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

            bobTimer = 0f;
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

            // 鍵を使用した（スイングが終わった）際のアイテム消費処理
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

    // 外部（PlayerController）から呼ばれる攻撃/使用アクション
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