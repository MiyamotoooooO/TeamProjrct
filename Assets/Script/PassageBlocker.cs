using UnityEngine;

public class PassageBlocker : MonoBehaviour
{
    [Header("ブロック設定")]
    [Tooltip("ロックが解除されたら消すオブジェクト")]
    public GameObject blockerObject;

    [Header("メッセージ設定")]
    [Tooltip("通れない時に表示するメッセージUI")]
    public GameObject lockedMessageText;
    [Tooltip("メッセージを表示する時間")]
    public float messageDuration = 2.0f;

    // 内部変数
    private bool isLocked = true;

    void Start()
    {
        // blockerObjectが指定されてなければ、このスクリプトがついているオブジェクト自体を消す対象にする
        if (blockerObject == null)
        {
            blockerObject = this.gameObject;
        }

        if (lockedMessageText != null)
        {
            lockedMessageText.SetActive(false);
        }
    }

    // プレイヤーがぶつかった時の処理
    private void OnCollisionEnter(Collision collision)
    {
        // ロック中かつ、プレイヤーがぶつかったら
        if (isLocked && collision.gameObject.CompareTag("Player"))
        {
            ShowLockedMessage();
        }
    }

    // メッセージを一瞬表示する
    void ShowLockedMessage()
    {
        if (lockedMessageText != null)
        {
            lockedMessageText.SetActive(true);
            // 以前の予約をキャンセルして新しく非表示予約
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), messageDuration);
        }
    }

    void HideMessage()
    {
        if (lockedMessageText != null)
        {
            lockedMessageText.SetActive(false);
        }
    }

    // ★外部（SearchPointViewer）から呼ばれる解除キー
    public void UnlockPassage()
    {
        if (!isLocked) return; // すでに開いていれば何もしない

        isLocked = false;

        // 邪魔な壁を消す（あるいはColliderだけ消す）
        if (blockerObject != null)
        {
            blockerObject.SetActive(false);
        }

        Debug.Log("通路のロックが解除されました！");
    }
}