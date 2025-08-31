using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerController : Entity
{
    
    private NewControls playerControls;

    [SerializeField] private Animator playerAnimator;

    public string[] allAnimations = new[] { "isRunning", "isJumping", "isFalling", "isWalking" };
    
    
    
    enum PlayerState
    {
        Idle,
        Walk,
        Run,
        Jump,
        Fall
    }

    private PlayerState currentState = PlayerState.Idle;

    private void ChangeState(PlayerState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case PlayerState.Idle : 
                ResetAllAnimatorExept("");
                break;
            case PlayerState.Walk :
                playerAnimator.SetBool("isWalking", true);
                ResetAllAnimatorExept("isWalking");
                break;
            case PlayerState.Run :
                playerAnimator.SetBool("isRunning", true);
                ResetAllAnimatorExept("isRunning");
                break;
            case PlayerState.Jump :
                playerAnimator.SetBool("isJumping", true);
                ResetAllAnimatorExept("isJumping");
                break;
            case PlayerState.Fall :
                playerAnimator.SetBool("isFalling", true);
                ResetAllAnimatorExept("isFalling");
                break;
        }
    }
    
    
    private void ResetAllAnimatorExept(string animationName)
    {
        foreach (var animName in allAnimations)
        {
            if (animName == animationName) continue;
            playerAnimator.SetBool(animName, false);
        }
    }

    [SerializeField] private GameObject projectilePrefab;

    private Vector2 shootAngle;
    
    public void Shoot(InputAction.CallbackContext input)
    {
        
        if (shootAngle != Vector2.zero)
        {
            Rigidbody2D projectileRb = Instantiate(projectilePrefab, transform.position, Quaternion.Euler(0, 0, Mathf.Atan2(shootAngle.y, shootAngle.x) * Mathf.Rad2Deg)).GetComponent<Rigidbody2D>();
            projectileRb.linearVelocity = shootAngle * currentEntityData.projectileSpeed;
        }
    }

    public void GetShootAngle(InputAction.CallbackContext directionVector)
    {
        
        shootAngle = directionVector.ReadValue<Vector2>().normalized;
    }

    protected override void Die()
    {
        
    }
}
