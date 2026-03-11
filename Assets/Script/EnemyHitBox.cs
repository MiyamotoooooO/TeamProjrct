using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 触れた相手が「Player」タグを持っているか確認
        if (other.CompareTag("Player"))
        {
            // プレイヤーのPlayerHealthスクリプトを取得
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            // スクリプトがあって、まだ死んでいなければ Die() を呼ぶ！
            if (playerHealth != null && !playerHealth.isDead)
            {
                Debug.Log("敵の攻撃がプレイヤーに命中！");
                playerHealth.Die();
            }
        }
    }
}