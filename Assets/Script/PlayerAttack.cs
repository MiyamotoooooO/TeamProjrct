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
    public float attackCooldown = 1.5f;

    bool isAttacking = false;
    bool isCooldown = false;
    bool canAttackThisEncounter = false;
    //[Header("攻撃音")]
    //public AudioClip attackSound;
    //private AudioSource audioSource;
    //追加
    public PlayerController playerController;

    private void Start()
    {
        //audioSource = GetComponent<AudioSource>();
    }
    void Update()
    {
        //遭遇中でなければ不可
        //if (!canAttackThisEncounter)
        //{
        //    return;
        //}
        //クールタイム or 攻撃中は不可
        if (isAttacking || isCooldown)
        {
            return;
        }
        //追加ーーーー
        //if(!playerController.IsHoldingCrowbar())
        //{
        //    return;
        //}
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        isAttacking = true;

        //// ★ここで音を鳴らす
        //if (audioSource != null && attackSound != null)
        //{
        //   // audioSource.Stop(); //連打対策
        //    audioSource.PlayOneShot(attackSound);
        //}
        //攻撃判定
        if (canAttackThisEncounter)
        {
            attackPoint.SetActive(true);
        }

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
    public bool IsAttacking()
    {
        return isAttacking;
    }

}

