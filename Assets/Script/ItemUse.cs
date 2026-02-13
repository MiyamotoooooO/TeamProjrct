using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class ItemUse : MonoBehaviour
{
    public PlayerController player;
    public GameObject cam;

    [Header("使用距離")]
    public float useDistance = 3f;

    [Header("鍵アイテム（Door 用）")]
    public GameObject keyObject;

    [Header("Bloodlump 除去に必要なアイテム名")]
    public string detergentName = "Detergent";

    [Header("Bloodlump 除去後に出す Sphere")]
    public GameObject spawnSpherePrefab;

    public TMP_Text UseText;

    private void Update()
    {
        if (player.isInventoryOpen)
        {
            UseText.enabled = false;
            return;
        }

        ShowClickUI();

        if (Input.GetMouseButtonDown(0))
        {
            TryUseItem();
        }
        player.UpdateKeySwing();
    }

    async void TryUseItem()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        // ★ 何も当たらなかったら即終了
        if (!Physics.Raycast(ray, out hit, useDistance))
        {
            return;
        }

        // ★ Bloodlump 処理
        if (hit.collider.CompareTag("Bloodlump"))
        {
            if (player.inventoryManager.HasItem(detergentName))
            {
                player.PlayItemSwing();
                await Task.Delay(900);

                Destroy(hit.collider.gameObject);
                Instantiate(spawnSpherePrefab, hit.point, Quaternion.identity);

                player.inventoryManager.RemoveItem(detergentName);
                player.UpdateItemModel();
            }
            return;
        }

        // ★ PuzzleButton
        if (hit.collider.CompareTag("PuzzleButton"))
        {
            PuzzleButton btn = hit.collider.GetComponent<PuzzleButton>();
            if (btn != null) btn.PressButton();
            return;
        }

        // ★ 回転パズル
        if (hit.collider.CompareTag("RotateObject"))
        {
            RotateObject rot = hit.collider.GetComponent<RotateObject>();
            if (rot != null) rot.RotateLeft();
            return;
        }

        // ★ Door 以外なら鍵処理は絶対にしない
        var door = hit.collider.GetComponentInParent<DoubleDoorController>();
        if (door == null)
        {
            return;
        }

        // 鍵名
        string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();

        // ★ 鍵を持っていなければ振らない
        if (!player.inventoryManager.HasItem(requiredKeyName))
        {
            Debug.Log("鍵を持っていません：" + requiredKeyName);
            return;
        }

        // ★ 鍵のタグ確認
        string tag = player.inventoryManager.GetItemTag(requiredKeyName);
        if (tag != "Key")
        {
            return; // 鍵じゃないなら振らない
        }
        // ★ ここまで来て初めて鍵を振る（ドア確定）
        player.canControl = false; // 移動停止
        player.canLock = false; // 視点移動

        player.PlayKeySwing();
        //player.UpdateKeySwing();
        await Task.Delay(1000);


        player.inventoryManager.RemoveItem(requiredKeyName);


        door.ForceOpen();

        await Task.Delay(3000);
        player.canControl = true;
        player.canLock = true;
    }

    void ShowClickUI()
    {
        UseText.enabled = false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, useDistance)) return;

        // Bloodlump
        if (hit.collider.CompareTag("Bloodlump"))
        {
            if (player.inventoryManager.HasItem(detergentName))
            {
                UseText.enabled = true;
            }
            return;
        }

        // Door
        var door = hit.collider.GetComponentInParent<DoubleDoorController>();
        if (door != null)
        {
            string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();

            if (player.inventoryManager.HasItem(requiredKeyName))
            {
                UseText.enabled = true;
            }
        }


    }
}





/*using UnityEngine;
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

               string tag = player.inventoryManager.GetItemTag(requiredKeyName);

                if (tag == null)
                {
                    if (tag == ("Key"))
                    {
                        // 鍵を使う動作
                        player.PlayKeySwing();
                        // 停止
                        await Task.Delay(TimeSpan.FromSeconds(0.9));
                        // 使用したらインベントリから削除
                        //player.inventoryManager.RemoveItem(requiredKeyName);
                        // 壁を消す
                        Destroy(hit.collider.gameObject);
                    }
                    else if (tag == ("Item"))
                    {
                        player.PlayItemSwing();
                    }
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
}*/
