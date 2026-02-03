using UnityEngine;

public class PlayerAttackPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // ボスに当たったら
        BossAI boss = other.GetComponent<BossAI>();
        if (boss != null)
        {
            boss.TakeDamage();
            Debug.Log("ボスにヒット！");
        }
    }
}
