using UnityEngine;

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

    private void Update()
    {
        // 左クリックで判定開始
        if (Input.GetMouseButtonDown(0))
        {
            TryUseItem();
        }
    }

    void TryUseItem()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        // レイがドア（壁）に当たった場合のみ処理する
        if (Physics.Raycast(ray, out hit, useDistance, doorLayer))
        {
            // InventoryManagerのチェック
            if (player.inventoryManager == null)
            {
                //Debug.LogError("PlayerControllerにInventoryManagerが設定されていません！");
                return;
            }

            // インベントリの中に「指定した鍵の名前」があるか確認する
            string requiredKeyName = keyObject.name;

            if (player.inventoryManager.HasItem(requiredKeyName))
            {
                Debug.Log("鍵(" + requiredKeyName + ")を使用して壁を消しました：" + hit.collider.name);

                // 壁を消す
                Destroy(hit.collider.gameObject);
            }
            else
            {
                Debug.Log("鍵を持っていません。必要な鍵: " + requiredKeyName);
            }
        }
    }
}