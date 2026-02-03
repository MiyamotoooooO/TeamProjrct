using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject attackPoint;
    public float attackDuration = 0.15f;

    bool isAttacking = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        isAttacking = true;

        attackPoint.SetActive(true);
        yield return new WaitForSeconds(attackDuration);
        attackPoint.SetActive(false);

        isAttacking = false;
    }
}
