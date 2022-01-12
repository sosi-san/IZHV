using System;
using System.Security.Claims;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// The main Player behaviour.
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>
    /// Velocity multiplier applied when jumping.
    /// </summary>
    public float jumpVelocity = 100.0f;
    

    /// <summary>
    /// Distance to which ground should be detected.
    /// </summary>
    public float groundCheckDistance = 0.01f;

    /// <summary>
    /// Layer mask used for the ground hit detection.
    /// </summary>
    public LayerMask groundLayerMask;

    /// <summary>
    /// Layer mask used for the obstacle hit detection.
    /// </summary>
    public LayerMask obstacleLayerMask;

    /// <summary>
    /// Speed of rotation animation in degrees.
    /// </summary>
    public float rotationSpeed = 30.0f;

    /// <summary>
    /// Which axes should be rotated?
    /// </summary>
    public bool3 rotateAxis = new bool3(true, false, false);
    
    /// <summary>
    /// Direction of rotation for each axis.
    /// </summary>
    public float3 axisDirection = new float3(1.0f, 1.0f, 1.0f);

    /// <summary>
    /// Sprite used in the neutral state.
    /// </summary>
    public Sprite spriteNeutral;
    
    /// <summary>
    /// Sprite used in the happy state.
    /// </summary>
    public Sprite spriteHappy;
    
    /// <summary>
    /// Sprite used in the sad state.
    /// </summary>
    public Sprite spriteSad;
    
    /// <summary>
    /// Sprite used in the pog state.
    /// </summary>
    public Sprite spritePog;
    
    /// <summary>
    /// Our RigidBody used for physics simulation.
    /// </summary>
    private Rigidbody2D mRB;
    
    /// <summary>
    /// Our BoxCollider used for collision detection.
    /// </summary>
    private BoxCollider2D mBC;
    
    /// <summary>
    /// Sprite renderer of the child sprite GameObject.
    /// </summary>
    private SpriteRenderer mSpriteRenderer;

    /// <summary>
    /// Transform of the child sprite GameObject.
    /// </summary>
    private Transform mSpriteTransform;

    /// <summary>
    /// Target angle of rotation in degrees.
    /// </summary>
    private Quaternion mTargetRotation;

    /// <summary>
    /// Remember when we switch gravity to prevent "hovering".
    /// </summary>
    private bool mSwitchedGravity = false;
    
    /// <summary>
    /// Current state of gravity - 1.0 for down, -1.0f for up.
    /// </summary>
    private float mCurrentGravity = 1.0f;

    /// <summary>
    /// Buffer in seconds for registering press of jump button
    /// </summary>
    private float jumpBufferTime = 0.1f;

    /// <summary>
    /// If value is > 0 then jump button was pressed in {jumpBufferTime}
    /// </summary>
    private float jumpBufferCounter;
    
    /// <summary>
    /// Buffer in seconds for registering press of vertical button
    /// </summary>
    private float verticalBufferTime = 0.1f;

    /// <summary>
    /// If value is > 0 then vertical button was pressed in {verticalBufferTime}
    /// </summary>
    private float verticalBufferCounter;
    
    /// <summary>
    ///  State of vertical axis
    /// </summary>
    private float verticalInput;
    
    /// <summary>
    ///  State of vertical axis
    /// </summary>
    private float gravityScale = 2.5f;
    /// <summary>
    ///  State of vertical axis
    /// </summary>
    private float fallMultiplayer = 3f;




    private bool JumpButtonPressed => Input.GetButton("Jump");
    /// <summary>
    /// Called before the first frame update.
    /// </summary>
    void Start()
    {
        mRB = GetComponent<Rigidbody2D>();
        mBC = GetComponent<BoxCollider2D>();
        mSpriteRenderer = gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
        mSpriteTransform = gameObject.transform.GetChild(0).GetComponent<Transform>();
        mTargetRotation = mSpriteTransform.rotation;
    }

    /// <summary>
    /// Update called once per frame.
    /// </summary>
    void Update()
    {
        // Process player input.
        verticalInput = Input.GetAxisRaw("Vertical");
        
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;
        
        if (verticalInput != 0f)
            verticalBufferCounter = verticalBufferTime;
        else
            verticalBufferCounter -= Time.deltaTime;
    }

    /// <summary>
    /// Update called for every update interval.
    /// </summary>
    private void FixedUpdate()
    {
        var onGround = IsOnGround();
        
        mSwitchedGravity &= !onGround;
        
        // Switch gravity with vertical movement.
        if (verticalBufferCounter > 0f && !mSwitchedGravity)
        {
            verticalBufferCounter = 0;
            
            mCurrentGravity = verticalInput > 0.0f ? 1.0f : -1.0f;
            Physics2D.gravity = mCurrentGravity * new Vector2(
                Math.Abs(Physics2D.gravity.x),
                Math.Abs(Physics2D.gravity.y)
            );
            mTargetRotation = Quaternion.Euler(new float3(
                rotateAxis.x && mCurrentGravity > 0.0f ? 180.0f : 0.0f, 
                rotateAxis.y && mCurrentGravity > 0.0f ? 180.0f : 0.0f, 
                rotateAxis.z && mCurrentGravity > 0.0f ? 180.0f : 0.0f
            ) * axisDirection);
            mSwitchedGravity = true;
            verticalBufferCounter = 0;
        }
        
        // Impart the initial impulse if we are jumping.
        if (jumpBufferCounter > 0f && onGround)
        {
            jumpBufferCounter = 0;
            var jumpVelocityNew = JumpButtonPressed ? 1.1f * jumpVelocity: 1f * jumpVelocity;
            mRB.velocity = -Physics2D.gravity * jumpVelocityNew;
        }

        if (Physics2D.gravity.y < 0f)
        {
            Debug.Log("Down to Up");
            if (mRB.velocity.y <= 0f)
            {
                mRB.gravityScale = fallMultiplayer;
            }
            else
            {
                mRB.gravityScale = gravityScale;
            }
        }
        if (Physics2D.gravity.y > 0f)
        {
            Debug.Log("Up to down");
            if (mRB.velocity.y >= 0f)
            {
                mRB.gravityScale = fallMultiplayer;
            }
            else
            {
                mRB.gravityScale = gravityScale;
            }
        }

        if (!onGround)
        { // While in mid-air, we can rotate.
            mSpriteTransform.rotation = Quaternion.RotateTowards(
                mSpriteTransform.rotation, mTargetRotation, 
                rotationSpeed * Time.fixedDeltaTime
            );
        }
        else
        { // Snap to target rotation once on solid ground.
            mSpriteTransform.rotation = mTargetRotation;
        }
    }
    /// <summary>
    /// Check if we are on the ground.
    /// </summary>
    /// <returns>Returns true if we are on solid ground.</returns>
    bool IsOnGround()
    {
        // Cast our current BoxCollider in the current gravity direction.
        var hitInfo = Physics2D.BoxCast(
            mBC.bounds.center, mBC.bounds.size, 
            0.0f, Physics2D.gravity.normalized, groundCheckDistance, 
            groundLayerMask
        );

        return hitInfo.collider != null;
    }
    
    /// <summary>
    /// Event triggered when we collide with something.
    /// </summary>
    /// <param name="other">The thing we collided with.</param>
    private void OnCollisionEnter2D(Collision2D other)
    {
        // Check the collided object against the layer mask.
        var hitObstacle = mBC.IsTouchingLayers(obstacleLayerMask);
        
        if (hitObstacle)
        { // If we collide with any obstacle -> end the game.
            // Update the sprite.
            mSpriteRenderer.sprite = spriteSad; 
            // Move to the uncollidable layer.
            gameObject.layer = LayerMask.NameToLayer("Uncollidable");
            // Fling the obstacle out of the screen.
            var otherRB = other.gameObject.GetComponent<Rigidbody2D>();
            other.gameObject.layer = LayerMask.NameToLayer("Uncollidable");
            otherRB.velocity = -Physics.gravity;
            // Loose the game.
            GameManager.Instance.LooseGame();
        }
    }
}
