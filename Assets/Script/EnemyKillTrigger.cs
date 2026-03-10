using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyKillTrigger : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private EnemyChaseKiller enemy;    // 敵の本体
    [SerializeField] private Transform cameraFacePoint; // 敵前のカメラ位置（Empty）
    [SerializeField] private float cameraSnapTime = 0.25f; // カメラ寄せの所要時間（Hijack非使用時の参考）

    private bool fired;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        if (!enemy) enemy = GetComponentInParent<EnemyChaseKiller>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var killer = GetComponentInParent<EnemyChaseKiller>();
        if (killer) killer.BeginKill();   // ← これだけ呼ぶ
    }
}