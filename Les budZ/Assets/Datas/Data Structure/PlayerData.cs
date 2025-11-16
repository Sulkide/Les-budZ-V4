using UnityEngine;

[CreateAssetMenu(menuName = "Player Data")]
public class PlayerData : ScriptableObject
{
	[Space(5)] [Header("PlayerStats")] 
	public string playerName;
	public bool hasHealthBonus;
	public float recoverTime;
	public float cooldownCAC;
	public int projectileSpeed;
	public float cooldownProjectile;
	
	[Space(20)][Header("Gravity")]
	[HideInInspector] public float gravityStrength; 
	[HideInInspector] public float gravityScale; 
										 
	[Space(5)]
	public float fallGravityMult;
	public float maxFallSpeed; 
	[Space(5)]
	public float fastFallGravityMult; 
	public float maxFastFallSpeed; 
	
	[Space(20)]

	[Header("Run")]
	public float runMaxSpeed; 
	public float runAcceleration; 
	[HideInInspector] public float runAccelAmount; 
	public float runDecceleration; 
	[HideInInspector] public float runDeccelAmount; 
	[Space(5)]
	[Range(0f, 1)] public float accelInAir; 
	[Range(0f, 1)] public float deccelInAir;
	[Space(5)]
	public bool doConserveMomentum = true;


	[Space(20)]

	[Header("Jump")]
	public float jumpHeight; 
	public float jumpTimeToApex; 
	[HideInInspector] public float jumpForce; 

	[Header("Both Jumps")]
	public float jumpCutGravityMult; 
	[Range(0f, 1)] public float jumpHangGravityMult; 
	public float jumpHangTimeThreshold; 
	[Space(0.5f)]
	public float jumpHangAccelerationMult; 
	public float jumpHangMaxSpeedMult; 				

	[Header("Wall Jump")]
	public Vector2 wallJumpForce; 
	[Space(5)]
	[Range(0f, 1f)] public float wallJumpRunLerp; 
	[Range(0f, 1.5f)] public float wallJumpTime; 
	public bool doTurnOnWallJump; 

	[Space(20)]

	[Header("Slide")]
	public float slideSpeed;
	public float slideAccel;
	public float maxAngleWithFriction = 30f;
	public float groundSlideDeceleration = 12f;

    [Header("Assists")]
	[Range(0.01f, 0.5f)] public float coyoteTime; 
	[Range(0.01f, 0.5f)] public float jumpInputBufferTime; 

	[Space(20)]

	[Header("Dash")]
	public int dashAmount;
	public float dashSpeed;
	public float dashMaxSpeed = 30f;
	public float dashSleepTime; 
	[Space(5)]
	public float dashAttackTime;
	[Space(5)]
	public float dashEndTime; 
	public Vector2 dashEndSpeed; 
	[Range(0f, 1f)] public float dashEndRunLerp; 
	[Space(5)]
	public float dashRefillTime;
	[Space(5)]
	[Range(0.01f, 0.5f)] public float dashInputBufferTime;
	

	[Header("Dash Jump Spécial")]
	public float dashJumpMaxDistance = 0.8f;     
	public float dashJumpMaxSlopeAngle = 70f;    
	public float dashJumpForceMult = 1.0f;    
	public float dashJumpNormalInfluence = 0.4f;
	public float dashJumpMaxUpSpeed = 15f;
	public float dashJumpUpSpeed = 12f;
	
	[Header("Ground Pound")]
	public float groundPoundAngleTolerance = 10f; 
	public float groundPoundSpeed = 30f;           
	public float groundPoundFreezeTime = 0.5f; 
	
	[Header("Attack / Stay Air Attack Settings")]
	public float idleAttackTime = 1f;
	public float movingAttackTime = 0.5f;
	public float airAttackTime = 0.25f;
	
	public float stayAirAttackAccel = 20f;
	public float stayAirAttackOppositeDecel = 25f;
	public float stayAirAttackMaxSpeed = 15f;
	public float stayAirAttackBounceForce = 10f;
	public float stayAirAttackMinSpeed = 1f;

	public float minHeightBounce = 0.2f;
	public float maxHeightBounce = 0.8f;
	public float nextBounceDivision = 1.1f;
	public float bonusBounceMarge = 0.12f;
	public float bonusBounceMult = 1.3f;
	

	
    private void OnValidate()
    {
		
		gravityStrength = -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);
		
		
		gravityScale = gravityStrength / Physics2D.gravity.y;

		
		runAccelAmount = (50 * runAcceleration) / runMaxSpeed;
		runDeccelAmount = (50 * runDecceleration) / runMaxSpeed;

		
		jumpForce = Mathf.Abs(gravityStrength) * jumpTimeToApex;

		#region Variable Ranges
		runAcceleration = Mathf.Clamp(runAcceleration, 0.01f, runMaxSpeed);
		runDecceleration = Mathf.Clamp(runDecceleration, 0.01f, runMaxSpeed);
		#endregion
	}
}