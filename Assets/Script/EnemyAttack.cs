using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public GameObject AttackPoint;
    private void Start()
    {
        Debug.Log("EnemyAttack‹N“®Šm”F");
        AttackPoint.SetActive(false);
    }

    public void EnableAttackPoint()
    {
        Debug.Log("AttackPoint ON");
        AttackPoint.SetActive(true);
    }

    public void DisableAttackPoint()
    {
        Debug.Log("AttackPoint OFF");
        AttackPoint.SetActive(false);
    }

}
