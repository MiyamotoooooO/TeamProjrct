using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Pointに当たった" + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("プレイヤーに攻撃ヒット");

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Die();
            }
        }
    }

    // Start is called before the first frame update

}
