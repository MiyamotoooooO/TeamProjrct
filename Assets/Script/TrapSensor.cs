using UnityEngine;

public class TrapSensor : MonoBehaviour
{
    [Tooltip("ここに TrapEventSystem をアタッチしたオブジェクトを入れる")]
    public TrapEventSystem eventSystem; // ★ここが変わりました

    // 一度だけ発動するためのフラグ
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return; // 2回目は何もしない

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            // イベントを開始！
            if (eventSystem != null) eventSystem.StartTrapEvent();
        }
    }
}