using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoint : MonoBehaviour
{
    [Header("ゾンビ本体のAnimatorを指定してください")]
    public Animator zombieAnimator;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Pointに当たった" + other.name);

        if (other.CompareTag("Player"))
        {
            if (zombieAnimator != null && IsAttacking())
            {
                Debug.Log("攻撃ヒット！プレイヤー死亡");

                PlayerHealth health = other.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.Die();
                }
            }
        }
    }

    bool IsAttacking()
    {
        // 現在再生中のアニメーション情報を取得 (0はBase Layer)
        AnimatorStateInfo stateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Z_Attack");
    }
}
