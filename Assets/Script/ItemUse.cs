using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System;

public class ItemUse : MonoBehaviour
{
    [Header("PlayerControllerを参照")]
    public PlayerController player;

    [Header("メインカメラを参照")]
    public GameObject cam;

    [Header("アイテム使用距離")]
    public float useDistance = 3f;

    [Header("ドアレイヤー")]
    public LayerMask doorLayer;

    [Header("必要な鍵の設定")]
    [Tooltip("ここに正解となる鍵のプレハブ（またはオブジェクト）をセットしてください")]
    public GameObject keyObject;

    public TMP_Text ClickText;

    private void Update()
    {
        // インベントリ中クリック、UI表示無効化
        if (player.isInventoryOpen)
        {
            ClickText.enabled = false;
            return;
        }

        ShowClickUI();

        // 左クリックで判定開始
        if (Input.GetMouseButtonDown(0))
        {
            TryUseItem();
        }
    }

    async Task TryUseItem()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        // レイがドア（壁）に当たった場合のみ処理する
        if (Physics.Raycast(ray, out hit, useDistance, doorLayer))
        {
            // InventoryManagerのチェック
            if (player.inventoryManager == null)
            {
                Debug.LogError("PlayerControllerにInventoryManagerが設定されていません！");
                return;
            }

            // インベントリの中に「指定した鍵の名前」があるか確認する
            string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();

            if (player.inventoryManager.HasItem(requiredKeyName))
            {
                Debug.Log("鍵(" + requiredKeyName + ")を使用して壁を消しました：" + hit.collider.name);

                int layer = player.inventoryManager.GetItemLayer(requiredKeyName);

                if (layer == LayerMask.NameToLayer("Key"))
                {
                    // 鍵を使う動作
                    player.PlayKeySwing();
                    // 停止
                    await Task.Delay(TimeSpan.FromSeconds(0.9));
                    // 使用したらインベントリから削除
                    player.inventoryManager.RemoveItem(requiredKeyName);
                    // 壁を消す
                    Destroy(hit.collider.gameObject);
                }
                else if (layer == LayerMask.NameToLayer("Item"))
                {
                    player.PlayItemSwing();
                }

                // 手持ちのモデルを更新
            }
            else
            {
                Debug.Log("籠を持っていません。必要な鍵：" + requiredKeyName);
            }
        }
    }

    void ShowClickUI()
    {
        ClickText.enabled = false; // 毎フレーム非表示にしておく

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, useDistance, doorLayer))
        {
            string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();

            // 鍵を持っているかチェック
            if (player.inventoryManager.HasItem(requiredKeyName))
            {
                ClickText.enabled = true;
                ClickText.text = "Click : Open";
            }
        }
    }
}
