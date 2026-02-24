using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public GameObject[] attackPoints;

    private void Start()
    {
        Debug.Log("EnemyAttack‹N“®Šm”F");

        foreach (GameObject point in attackPoints)
        {
            if (point != null)
                point.SetActive(false);
        }
    }

    public void EnableAttackPoint()
    {
        Debug.Log("AttackPoint ON");

        foreach (GameObject point in attackPoints)
        {
            if (point != null)
                point.SetActive(true);
        }
    }

    public void DisableAttackPoint()
    {
        Debug.Log("AttackPoint OFF");

        foreach (GameObject point in attackPoints)
        {
            if (point != null)
                point.SetActive(false);
        }
    }
}
