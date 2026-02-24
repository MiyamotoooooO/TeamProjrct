using UnityEngine;

public class PlayerAttackPoint : MonoBehaviour
{
    public PlayerAttack playerAttack;
    private void OnTriggerEnter(Collider other)
    {

        if (!playerAttack.IsAttacking())
        {
            return;
        }
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
