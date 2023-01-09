using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public CharacterController2D controller;
    public Animator animator;

    public float runSpeed = 50f;
    float horizontalMove = 0f;
    bool jump = false;
    bool crouch = false;    
   

    void Update()
    {        
        horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

        animator.SetFloat("Speed", Mathf.Abs(horizontalMove));

        if (Input.GetButtonDown("Jump"))
        {
            jump = true;
            animator.SetBool("IsJumping", true);            
        }

        if (Input.GetButtonDown("Crouch"))
        {
            crouch = true;
        }
        else if (Input.GetButtonUp("Crouch"))
        {
            crouch = false;
        }

    }

    public void OnLanding()
    {
        //跳躍動畫結束
        animator.SetBool("IsJumping", false);        
    }

    public void OnCrouching(bool IsCrouching) 
    {
        //蹲下動畫結束
        animator.SetBool("IsCrouching", IsCrouching);
    }

    void FixedUpdate()
    {
        //移動角色
        controller.Move(horizontalMove * Time.fixedDeltaTime, crouch, jump);
        jump = false;        
    }

}
