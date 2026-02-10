using UnityEngine;

public class SpiderGimmick : MonoBehaviour
{
    [Header("ギミック設定")]
    [Tooltip("消滅させるクモ本体（親オブジェクト）")]
    public GameObject spiderBody; // ★ここが変更点

    [Tooltip("出現させる鍵オブジェクト")]
    public GameObject keyObject;

    [Tooltip("反応するタグ名")]
    public string targetTag = "MouseToy";

    [Header("（オプション）効果音")]
    public AudioClip gimmickSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            Debug.Log("ネズミがエリアに入った！");
            ActivateGimmick(other.gameObject);
        }
    }

    private void ActivateGimmick(GameObject mouseToy)
    {
        // 音を鳴らす
        if (gimmickSound != null && keyObject != null)
        {
            AudioSource.PlayClipAtPoint(gimmickSound, keyObject.transform.position);
        }

        // 鍵を出現させる
        if (keyObject != null)
        {
            keyObject.SetActive(true);
        }

        // ネズミを消す
        Destroy(mouseToy);

        // ★変更点：自分ではなく「クモ本体（親）」を消す
        if (spiderBody != null)
        {
            Destroy(spiderBody);
        }
        else
        {
            // 設定し忘れた時の保険（自分を消す）
            Destroy(this.gameObject);
        }
    }
}