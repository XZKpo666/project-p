using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class CharacterController2D : MonoBehaviour
{
    [SerializeField] private float m_JumpForce = 400f;                          // 玩家跳躍時增加的力量。(Amount of force added when the player jumps.
    [Range(0, 1)][SerializeField] private float m_CrouchSpeed = .36f;           // 應用於蹲伏運動的 maxSpeed 量。 1 = 100%(Amount of maxSpeed applied to crouching movement. 1 = 100%
    [Range(0, .3f)][SerializeField] private float m_MovementSmoothing = .05f;   // 平滑運動需要多少(How much to smooth out the movement
    [SerializeField] private bool m_AirControl = false;                         // 玩家在跳躍時是否可以轉向(Whether or not a player can steer while jumping;
    [SerializeField] private LayerMask m_WhatIsGround;                          // 確定角色基礎內容的面具(A mask determining what is ground to the character
    [SerializeField] private Transform m_GroundCheck;                           // 標記檢查玩家是否接地的位置。(A position marking where to check if the player is grounded.
    [SerializeField] private Transform m_CeilingCheck;                          // 標記檢查天花板的位置(A position marking where to check for ceilings
    [SerializeField] private Collider2D m_CrouchDisableCollider;                // 蹲下時將被禁用的對撞機(A collider that will be disabled when crouching

    const float k_GroundedRadius = .2f; // 判斷是否接地的重疊圓半徑(Radius of the overlap circle to determine if grounded
    private bool m_Grounded;            // 玩家是否被禁足。(Whether or not the player is grounded.
    const float k_CeilingRadius = .2f; // 重疊圓的半徑，以確定玩家是否可以站起來(Radius of the overlap circle to determine if the player can stand up
    private Rigidbody2D m_Rigidbody2D;
    private bool m_FacingRight = true;  // 用於確定玩家當前面對的方向。(For determining which way the player is currently facing.
    private Vector3 m_Velocity = Vector3.zero;

    [Header("Events")]
    [Space]

    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    public BoolEvent OnCrouchEvent;
    private bool m_wasCrouching = false;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();

        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();

        if (OnCrouchEvent == null)
            OnCrouchEvent = new BoolEvent();
    }

    private void FixedUpdate()
    {
        bool wasGrounded = m_Grounded;
        m_Grounded = false;

        // 如果對地面檢查位置的圓形投射擊中指定為地面的任何物體，則玩家將被停飛(The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // 這可以使用圖層來完成，但示例資產不會覆蓋您的項目設置。(This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                m_Grounded = true;
                if (!wasGrounded)
                    OnLandEvent.Invoke();
            }
        }
    }


    public void Move(float move, bool crouch, bool jump)
    {
        // 如果蹲下，檢查角色是否可以站起來(If crouching, check to see if the character can stand up
        if (!crouch)
        {
            // 如果角色有天花板阻止他們站起來，讓他們蹲下(If the character has a ceiling preventing them from standing up, keep them crouching
            if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                crouch = true;
            }
        }

        //僅在接地或打開 airControl 時控製播放器(only control the player if grounded or airControl is turned on
        if (m_Grounded || m_AirControl)
        {

            // 如果蹲著(If crouching
            if (crouch)
            {
                if (!m_wasCrouching)
                {
                    m_wasCrouching = true;
                    OnCrouchEvent.Invoke(true);
                }

                // 通過 crouchSpeed 倍增器降低速度(Reduce the speed by the crouchSpeed multiplier
                move *= m_CrouchSpeed;

                // 蹲下時禁用其中一個碰撞器(Disable one of the colliders when crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = false;
            }
            else
            {
                // 不蹲下時啟用對撞機(Enable the collider when not crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;

                if (m_wasCrouching)
                {
                    m_wasCrouching = false;
                    OnCrouchEvent.Invoke(false);
                }
            }

            // 通過找到目標速度移動角色(Move the character by finding the target velocity
            Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
            // 然後將其平滑並應用於角色(And then smoothing it out and applying it to the character
            m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

            // 如果輸入向右移動玩家而玩家面向左......(If the input is moving the player right and the player is facing left...
            if (move > 0 && !m_FacingRight)
            {
                // ...翻轉玩家(... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (move < 0 && m_FacingRight)
            {
                //...f翻轉玩家（... flip the player.
                Flip();
            }
        }
        // 如果玩家應該跳...（If the player should jump...
        if (m_Grounded && jump)
        {
            // 為播放器添加垂直力。（Add a vertical force to the player.
            m_Grounded = false;
            m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
        }

    }


    private void Flip()
    {
        // 切換玩家標記為面對的方式。（Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;

        // 將玩家的 x 本地比例乘以 -1。（Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
}
