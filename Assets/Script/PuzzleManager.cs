using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [Header("正解の順番")]
    public int[] answer = { 2, 4, 1, 3 };
    private int progress = 0;

    [Header("正解時に入手するアイテム")]
    [Tooltip("クリアした時にインベントリに入れたいアイテム（鍵のプレハブ等）をアタッチしてください")]
    public GameObject keyItemObject;

    [Header("参照設定")]
    public PlayerController playerController;
    public InventoryManager inventoryManager;

    private bool isCleared = false; // クリア済みかどうかのフラグ

    private void Start()
    {
        // 参照が空なら自動で探す
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
    }

    public void InputButton(int id, PuzzleButton button)
    {
        // 既にクリア済みならボタンを押しても何もしない
        if (isCleared) return;

        // 正しいボタンか
        if (id == answer[progress])
        {
            progress++;

            // 全問正解した
            if (progress >= answer.Length)
            {
                Debug.Log("Puzzle Clear");
                isCleared = true; // 二重クリア防止

                // カエルの時のようにインベントリに直接追加するコルーチンを呼ぶ
                StartCoroutine(ClearSequence());
            }
        }
        else
        {
            Debug.Log("Miss リセット");
            button.GlowWrong();
            progress = 0;
        }
    }

    // アイテム入手と演出のシーケンス
    private IEnumerator ClearSequence()
    {
        if (keyItemObject != null && inventoryManager != null && playerController != null)
        {
            string cleanName = keyItemObject.name.Replace("(Clone)", "").Trim();

            // ★ プレハブを直接 PickUpItem に渡すとエラーになる場合があるため、
            // 見えない場所で一瞬だけ実体を作り、それを拾わせることで安全にインベントリに入れます。
            GameObject tempItem = Instantiate(keyItemObject, transform.position, Quaternion.identity);
            tempItem.name = cleanName;

            // インベントリにアイテムを入れる
            inventoryManager.PickUpItem(tempItem);
            playerController.UpdateItemModel();

            // 入手演出（クルクル回るUI）を表示する
            if (playerController.itemGetDisplay != null)
            {
                playerController.itemGetDisplay.ShowItemGet(cleanName);

                // 演出が終わるまで待つ
                yield return new WaitWhile(() => playerController.itemGetDisplay.isDisplaying);
            }
        }
    }
}