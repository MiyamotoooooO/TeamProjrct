using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject attackPoint;
    [Header("攻撃時間")]
    public float attackDuration = 0.5f;

    [Header("クールタイム")]
    public float attackCooldown = 5.0f;

    bool isAttacking = false;
    bool isCooldown = false;
    bool canAttackThisEncounter = false;

    void Update()
    {
        //遭遇中でなければ不可
        if (!canAttackThisEncounter)
        {
            return;
        }
        //クールタイム or 攻撃中は不可
        if (isAttacking || isCooldown)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        isAttacking = true;

        //攻撃判定
        attackPoint.SetActive(true);
        yield return new WaitForSeconds(attackDuration);
        attackPoint.SetActive(false);

        isAttacking = false;

        //クールタイム開始
        StartCoroutine(Cooldown());
    }
    IEnumerator Cooldown()
    {
        isCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isCooldown = false;
    }
    public void EnbleAttack()
    {
        canAttackThisEncounter = true;
    }
    public void DisableAttack()
    {
        canAttackThisEncounter = false;
        isCooldown = false;
        isAttacking = false;
    }
}

