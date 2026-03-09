using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animation : MonoBehaviour
{
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //animator.SetTrigger("Attack");
        }
        if (Input.GetMouseButtonDown(1))
        {
            //animator.SetTrigger("Opening");
        }
        //if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
        //    || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        //{
        //    animator.SetFloat("Walk", 1);
        //}
        bool isMove = Input.GetKey(KeyCode.W)
           || Input.GetKey(KeyCode.A)
           || Input.GetKey(KeyCode.S)
           || Input.GetKey(KeyCode.D);

        bool isRun = isMove && Input.GetKey(KeyCode.R);

        animator.SetFloat("Running", isRun ? 1 : 0);
        animator.SetFloat("Walk", (!isRun && isMove) ? 1 : 0);
    }
}
