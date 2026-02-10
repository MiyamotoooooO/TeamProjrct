using UnityEngine;

public class PlayerAttackPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("HIT:" + other.name);
        // ボスに当たったら
        BossAI boss = other.GetComponent<BossAI>();
        if (boss != null)
        {
            boss.TakeDamage();
            Debug.Log("ボスにヒット！");
        }
    }

}
