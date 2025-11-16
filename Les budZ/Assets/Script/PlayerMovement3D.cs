using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement3D : MonoBehaviour
{
    [Header("Options General")]
    public int currentLife = 5;
    public float gravityScale;
    public float damageCooldown = 2.5f;
    public RigidbodyConstraints defaultConstraints;
    public Vector2 moveInput;
    public Vector2 aimInput;
    public Vector2 dpadInput;
    public Vector3 capsuleSize;
    public Vector3 capsuleCenter;
    public Vector3 originalScale;
    public bool deactivateOnOffScreen;
    public bool alignToGroundSlope = true;
    public bool use3DMovement = true;
    public bool rotateChildOnDash = true;
    public float maxAngleWithFriction = 30f;
    public bool canWallJump = true;
    public bool canDash = true;
    public bool canAttack = true;

    [Space(5)]
    [Header("References")]
    public PlayerData data;
    public PlayerInput playerControls;
    public GameObject baseModelPrefab;
    public Animator playerAnimator;
    public Collider collider;
    public Rigidbody rb;
    public GameObject parent;
    public PhysicsMaterial frictionMaterial;
    public PhysicsMaterial noFrictionMaterial;
    public Camera cam;

    [Space(2)]
    public GameObject armOriginal;
    public GameObject armAim;
    public Transform armPivot;
    public bool flipAimArm;
    public float pivotCorrection = 180f;

    [Space(5)]
    [Header("Checks")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector3 groundCheckSize = new Vector3(0.49f, 0.03f, 0f);
    [SerializeField] public Transform frontWallCheckPoint;
    [SerializeField] public Transform backWallCheckPoint;
    [SerializeField] private Vector3 wallCheckSize = new Vector3(0.5f, 1f, 0f);
    [SerializeField] private Transform airAttackGroundCheckPoint;
    [SerializeField] private float airAttackGroundRadius = 0.5f;
    [SerializeField] private float airAttackGroundHeight = 1.0f;

    [Space(5)]
    [Header("Layers & Tags")]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    public LayerMask enemyProjectileLayer;

    [Space(5)]
    [Header("Parametres des états")]
    public bool cannotMove;
    public bool isDead;
    public bool areControllsRemoved;
    public bool wasGroundedLastFrame = false;
    public bool dashRefilling { get; private set; }
    public bool isFacingRight { get; private set; }
    public bool isJumping { get; private set; }
    public bool isFalling { get; private set; }
    public bool isWallJumping { get; private set; }
    public bool isDashing { get; private set; }
    public bool isSliding { get; private set; }
    public bool isGroundPounding { get; private set; }
    public bool isStayAirAttacking { get; private set; }
    public float attackStateEndTime { get; private set; }
    public bool isAirAttcking { get; private set; }
    public bool isMovingAttcking { get; private set; }
    public bool isIdleAttcking { get; private set; }
    public bool isJumpCut { get; private set; }
    public bool isJumpFalling { get; private set; }
    public bool isGroundSliding { get; private set; }
    public bool isDashRefilling { get; private set; }
    public bool isDashAttacking { get; private set; }
    public bool fixedLastOnGroundTime { get; private set; }
    public bool isGrappling { get; private set; }
    public bool isGroundedNow { get; private set; }
    public int lastWallJumpDir { get; private set; }
    public int dashesLeft { get; private set; }
    public float targetSpeed { get; private set; }
    public float wallJumpStartTime { get; private set; }
    public float lastOnGroundTime { get; private set; }
    public float lastOnWallTime { get; private set; }
    public float lastOnWallRightTime { get; private set; }
    public float lastOnWallLeftTime { get; private set; }
    public float lastPressedJumpTime { get; private set; }
    public float lastPressedDashTime { get; private set; }
    public Vector3 lastDashDir { get; private set; }

    public Vector3 stayAirAttackVelocity { get; private set; }
    public float stayAirCurrentHeight { get; private set; }
    public float lastJumpMaxY { get; private set; }
    public float lastGroundY { get; private set; }
    public bool trackJumpHeight { get; private set; }
    public float lastJumpButtonTime { get; private set; }

    [Header("Nom des Actions")]
    public string actionMapName = "Gameplay";
    public string actionMoveName = "Move";
    public string actionDpadName = "Dpad";
    public string actionAimName = "Aim";
    public string actionJumpName = "Jump";
    public string actionDashName = "Dash";
    public string actionUseName = "Use";
    public string actionAttackName = "Attack";
    public string actionGrapName = "Grap";
    public string actionStartName = "Start";
    public string actionPauseName = "Pause";
    public string actionSelectRName = "SelectR";
    public string actionSelectLName = "SelectL";
    public string actionFlipDimensionName = "FlipDimension";

    private InputAction moveAction;
    private InputAction dpadAction;
    private InputAction aimAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction useAction;
    private InputAction attackAction;
    private InputAction grapAction;
    private InputAction startAction;
    private InputAction pauseAction;
    private InputAction selectRAction;
    private InputAction selectLAction;
    private InputAction flipAction;

    [Header("liste des SXF")]
    public List<string> clipsRandomImpact = new List<string> { "impact1", "impact2", "impact3", "impact4" };
    public List<string> clipsRandomDeath = new List<string> { "deathBell1" };
    public List<string> clipsRandomSlap = new List<string> { "slap1" };
    public List<string> clipsRandomjump = new List<string> { "jump1" };
    public List<string> clipsRandomWalljump = new List<string> { "wall jump" };
    public List<string> clipsRandomDash = new List<string> { "dash1" };

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
        playerControls = GetComponentInParent<PlayerInput>();

        defaultConstraints = rb.constraints;
    }

    void Start()
    {
        if (rb != null) rb.useGravity = false; // on gère la gravité à la main
        gameObject.layer = LayerMask.NameToLayer("Player");
        capsuleSize = collider.bounds.size;
        capsuleCenter = collider.bounds.center;

        // Gravité "par défaut" venant des PlayerData
        if (data != null)
            SetGravityScale(data.gravityScale);

        isFacingRight = true;
        cam = Camera.main;
        originalScale = transform.localScale;

        if (gameObject.transform.parent != null)
        {
            parent = gameObject.transform.parent.gameObject;
        }

        if (data != null)
        {
            dashesLeft = data.dashAmount;
        }
        else
        {
            dashesLeft = 1;
        }
    }

    void OnEnable()
    {
        #region ENABLED INPUT ACTIONS

        var actions = playerControls.actions;
        if (!string.IsNullOrEmpty(actionMapName))
            actions.FindActionMap(actionMapName, throwIfNotFound: true);

        moveAction = actions[actionMoveName];
        dpadAction = actions[actionDpadName];
        aimAction = actions[actionAimName];

        jumpAction = actions[actionJumpName];
        dashAction = actions[actionDashName];
        useAction = actions[actionUseName];
        attackAction = actions[actionAttackName];
        grapAction = actions[actionGrapName];
        startAction = actions[actionStartName];
        pauseAction = actions[actionPauseName];
        selectRAction = actions[actionSelectRName];
        selectLAction = actions[actionSelectLName];
        flipAction = actions[actionFlipDimensionName];

        jumpAction.performed += OnJumpPressed;
        jumpAction.canceled += OnJumpReleased;

        dashAction.performed += OnDashPressed;
        dashAction.canceled += OnDashReleased;

        useAction.performed += OnUsePressed;
        useAction.canceled += OnUseReleased;

        attackAction.performed += OnAttackPressed;
        attackAction.canceled += OnAttackReleased;

        grapAction.performed += OnGrapPressed;
        grapAction.canceled += OnGrapReleased;

        startAction.performed += OnStartPressed;
        pauseAction.performed += OnPausePressed;

        selectRAction.performed += OnSelectRPressed;
        selectLAction.performed += OnSelectRPressed;

        flipAction.performed += OnFlipPressed;

        moveAction.Enable();
        dpadAction.Enable();
        aimAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
        useAction.Enable();
        attackAction.Enable();
        grapAction.Enable();
        startAction.Enable();
        pauseAction.Enable();
        selectRAction.Enable();
        selectLAction.Enable();
        flipAction.Enable();

        #endregion
    }

    private void OnDisable()
    {
        #region DISABLE INPUT ACTIONS

        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPressed;
            jumpAction.canceled -= OnJumpReleased;
        }

        if (dashAction != null)
        {
            dashAction.performed -= OnDashPressed;
            dashAction.canceled -= OnDashReleased;
        }

        if (useAction != null)
        {
            useAction.performed -= OnUsePressed;
            useAction.canceled -= OnUseReleased;
        }

        if (attackAction != null)
        {
            attackAction.performed -= OnAttackPressed;
            attackAction.canceled -= OnAttackReleased;
        }

        if (grapAction != null)
        {
            grapAction.performed -= OnGrapPressed;
            grapAction.canceled -= OnGrapReleased;
        }

        if (startAction != null)
        {
            startAction.performed -= OnStartPressed;
        }

        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePressed;
        }

        if (selectRAction != null)
        {
            selectRAction.performed -= OnSelectRPressed;
        }

        if (selectLAction != null)
        {
            selectLAction.performed -= OnSelectRPressed;
        }

        if (flipAction != null)
        {
            flipAction.performed -= OnFlipPressed;
        }

        #endregion
    }

    private void Update()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused) return;

        moveInput = moveAction.ReadValue<Vector2>();
        aimInput = aimAction.ReadValue<Vector2>();
        dpadInput = dpadAction.ReadValue<Vector2>();
        targetSpeed = moveInput.x * (data != null ? data.runMaxSpeed : 0f);

        lastOnGroundTime -= Time.deltaTime;
        lastOnWallTime -= Time.deltaTime;
        lastOnWallLeftTime -= Time.deltaTime;
        lastOnWallRightTime -= Time.deltaTime;
        lastPressedJumpTime -= Time.deltaTime;

        // Suivi de la hauteur du saut courant pour le premier rebond du StayAirAttack
        if (trackJumpHeight && !wasGroundedLastFrame)
        {
            lastJumpMaxY = Mathf.Max(lastJumpMaxY, transform.position.y);
        }

        GroundCheck3D();
        WallCheck3D();

        HandleFacing();

        HandleJumpState();
        HandleJumpBuffer();
        HandleDashState();
        HandleAttackState();
    }

    void FixedUpdate()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused) return;

        if (GameManager.instance != null)
        {
            GameManager.instance.FindPlayer(parent.name, transform, this);
            GameManager.instance.CharacterCheck(parent.name, data.playerName);
        }


        if (isDashing || isDashAttacking)
            return;
        
        if (isStayAirAttacking)
        {
            SwitchAnimation("isStayAttack");
            HandleStayAirAttackMovement();
            ApplyCustomGravity();
            return;
        }
        
        bool pushingIntoWall =
            (lastOnWallLeftTime > 0f  && moveInput.x < -0.01f) ||
            (lastOnWallRightTime > 0f && moveInput.x >  0.01f);

        if (CanSlide() && pushingIntoWall)
        {
            isSliding = true;
            SwitchAnimation("isSliding");
            rb.constraints = defaultConstraints | RigidbodyConstraints.FreezePositionZ;
        }
        else
        {
            isSliding = false;
            rb.constraints = defaultConstraints;
        }

        if (isSliding)
        {
            Slide3D();
            return;
        }
        
        ApplyCustomGravity();

        if (cannotMove || isDead) return;

        if (GameManager.instance.is3d)
            HandleMovement3D();
        else
            HandleMovement2D();
    }

    #region INPUT ACTION BUTTONS

    private void OnFlipPressed(InputAction.CallbackContext obj)
    {
        GameManager.instance.ChangeDimension();
    }

    private void OnSelectRPressed(InputAction.CallbackContext obj)
    {
        Debug.Log("OnSelectRPressed");
    }

    private void OnPausePressed(InputAction.CallbackContext obj)
    {
        Debug.Log("OnPausePressed");
    }

    private void OnStartPressed(InputAction.CallbackContext obj)
    {
        Debug.Log("OnStartPressed");
    }

    private void OnGrapReleased(InputAction.CallbackContext obj)
    {
        Debug.Log("OnGrapReleased");
    }

    private void OnGrapPressed(InputAction.CallbackContext obj)
    {
        Debug.Log("OnGrapPressed");
    }

    private void OnAttackReleased(InputAction.CallbackContext obj)
    {
        if (isStayAirAttacking)
        {
            isStayAirAttacking = false;
            SwitchAnimation("");
        }
    }

    private void OnAttackPressed(InputAction.CallbackContext obj)
    {
        if (cannotMove || data == null || !canAttack) return;
        if (isDashing || isDashAttacking) return;
        if (isGroundPounding) return;

        
        bool grounded = lastOnGroundTime > 0f && Mathf.Abs(rb.linearVelocity.y) < 0.01f;
        
        if (!wasGroundedLastFrame)
        {
            isIdleAttcking = false;
            isMovingAttcking = false;
            attackStateEndTime = 0f;

            if (!isAirAttcking && !isStayAirAttacking)
            {
                StartAirAttack();
            }

            return;
        }


        if (Time.time < attackStateEndTime && (isIdleAttcking || isMovingAttcking))
            return;

        bool moving2D = Mathf.Abs(moveInput.x) > 0.01f;
        bool moving3D = moveInput.sqrMagnitude > 0.01f;

        bool moving = GameManager.instance != null && GameManager.instance.is3d ? moving3D : moving2D;

        if (moving)
            StartMovingAttack();
        else
            StartIdleAttack();
    }

    private void OnUseReleased(InputAction.CallbackContext obj)
    {
        Debug.Log("OnUseReleased");
    }

    private void OnUsePressed(InputAction.CallbackContext obj)
    {
        Debug.Log("OnUsePressed");
    }

    private void OnDashPressed(InputAction.CallbackContext obj)
    {
        if (cannotMove || data == null || !canDash) return;
        lastPressedDashTime = data.dashInputBufferTime;
    }

    private void OnDashReleased(InputAction.CallbackContext obj)
    {
        if (cannotMove || data == null || !canDash) return;
        lastPressedDashTime = 0;
    }

    private void OnJumpReleased(InputAction.CallbackContext obj)
    {
        if (CanJumpCut())
            isJumpCut = true;
    }

    private void OnJumpPressed(InputAction.CallbackContext obj)
    {
        lastJumpButtonTime = Time.time;
        
        if (isStayAirAttacking)
            return;

        if (cannotMove || data == null) return;
        lastPressedJumpTime = data.jumpInputBufferTime;
    }

    #endregion

    #region GRAVITY

    public void SetGravityScale(float scale)
    {
        rb.useGravity = false;
        gravityScale = scale;
    }

    private void ApplyCustomGravity()
    {
        if (data == null || isSliding || isDashing || isDashAttacking || isGroundPounding) 
            return;

        float baseGravity = data.gravityScale;

        if (isJumpCut && rb.linearVelocity.y > 0f)
        {
            SetGravityScale(baseGravity * data.jumpCutGravityMult);
        }
        else if ((isJumping || isJumpFalling) &&
                 Mathf.Abs(rb.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            SetGravityScale(baseGravity * data.jumpHangGravityMult);
        }
        else if (rb.linearVelocity.y < 0 && lastOnGroundTime <= 0)
        {
            SetGravityScale(baseGravity * data.fallGravityMult);

            Vector3 vel = rb.linearVelocity;
            vel.y = Mathf.Max(vel.y, -data.maxFallSpeed);
            rb.linearVelocity = vel;
        }
        else
        {
            SetGravityScale(baseGravity);
        }

        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
    }

    #endregion

    #region CHECKS

    private void GroundCheck3D()
    {
        bool grounded = false;
        
        if (isStayAirAttacking && airAttackGroundCheckPoint != null)
        {
            float r = airAttackGroundRadius;
            float h = Mathf.Max(airAttackGroundHeight, r * 2f); 
            
            Vector3 center = airAttackGroundCheckPoint.position;
            float half = (h * 0.5f) - r;
            Vector3 top = center + Vector3.up * half;
            Vector3 bottom = center - Vector3.up * half;

            grounded = Physics.CheckCapsule(top, bottom, r, groundLayer);
        }
        else
        {
            if (groundCheckPoint != null)
            {
                grounded = Physics.CheckBox(
                    groundCheckPoint.position,
                    groundCheckSize * 0.5f,
                    Quaternion.identity,
                    groundLayer
                );
            }
        }

        if (grounded)
            lastOnGroundTime = data != null ? data.coyoteTime : 0.1f;
        
        if (grounded && !wasGroundedLastFrame)
        {
            TouchGround();
        }

        if (!grounded && wasGroundedLastFrame)
        {
            LeaveGround();
        }

        wasGroundedLastFrame = grounded;
    }


    private void WallCheck3D()
    {
        if (frontWallCheckPoint == null || backWallCheckPoint == null) return;

        bool frontHit = Physics.CheckBox(
            frontWallCheckPoint.position,
            wallCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        bool backHit = Physics.CheckBox(
            backWallCheckPoint.position,
            wallCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        if (((frontHit && isFacingRight) || (backHit && !isFacingRight)) && !isWallJumping)
        {
            lastOnWallRightTime = data != null ? data.coyoteTime : 0.1f;
        }

        if (((frontHit && !isFacingRight) || (backHit && isFacingRight)) && !isWallJumping)
        {
            lastOnWallLeftTime = data != null ? data.coyoteTime : 0.1f;
        }

        lastOnWallTime = Mathf.Max(lastOnWallLeftTime, lastOnWallRightTime);
    }

    private void HandleFacing()
    {
        if (moveInput.x > 0.01f)
            CheckDirectionToFace(true);
        else if (moveInput.x < -0.01f)
            CheckDirectionToFace(false);
    }

    private void CheckDirectionToFace(bool moveRight)
    {
        if (moveRight != isFacingRight)
        {
            if (!isDashing)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;

                isFacingRight = !isFacingRight;
            }
        }
    }

    private void HandleJumpState()
    {
        if (isJumping && rb.linearVelocity.y < 0f)
        {
            isJumping = false;
            isJumpFalling = true;

            if (!isAirAttcking && !isStayAirAttacking)
            {
                SwitchAnimation("isFalling");
            }
        }

        if (isWallJumping && data != null && Time.time - wallJumpStartTime > data.wallJumpTime)
        {
            isWallJumping = false;
        }

        if (lastOnGroundTime > 0f)
        {
            isJumping = false;
            isWallJumping = false;
            isJumpCut = false;
            isJumpFalling = false;
        }
    }


    private void HandleJumpBuffer()
    {
        if (data == null) return;

        if (CanJump() && lastPressedJumpTime > 0f)
        {
            isJumping = true;
            isWallJumping = false;
            isJumpCut = false;
            isJumpFalling = false;

            Jump();

            lastPressedJumpTime = 0f;
            return;
        }

        if (CanWallJump() && lastPressedJumpTime > 0f)
        {
            isWallJumping = true;
            isJumping = false;
            isJumpCut = false;
            isJumpFalling = false;

            wallJumpStartTime = Time.time;
            lastWallJumpDir = (lastOnWallRightTime > 0f) ? -1 : 1;

            WallJump(lastWallJumpDir);
            lastPressedJumpTime = 0f;
        }
    }

    void HandleDashState()
    {
        if (isGroundPounding) 
            return;

        if (!CanDash() || lastPressedDashTime <= 0f)
            return;
        if (moveInput == Vector2.zero)
        {
            SwitchAnimation("isCAC");
            return;
        }

        if (isStayAirAttacking)
            isStayAirAttacking = false;

        Vector3 inputDir;

        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        }
        else
        {
            inputDir = new Vector3(moveInput.x, moveInput.y, 0f);
        }

        if (inputDir.sqrMagnitude < 0.0001f)
            return;
        
        bool inAir = !wasGroundedLastFrame;


        bool is2DMode = GameManager.instance == null || !GameManager.instance.is3d;

        if (is2DMode && inAir && moveInput.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;

            if (Mathf.Abs(Mathf.DeltaAngle(angle, -90f)) <= data.groundPoundAngleTolerance)
            {
                lastPressedDashTime = 0f;
                StartCoroutine(StartGroundPound());
                return;
            }
        }

        
        lastDashDir = inputDir.normalized;

        isDashing = true;
        isJumping = false;
        isWallJumping = false;
        isJumpCut = false;

        StartCoroutine(StartDash(lastDashDir));

        lastPressedDashTime = 0f;
    }
    
    private IEnumerator GroundPoundLanding()
    {
        cannotMove = true;
        SwitchAnimation("isLanded");
        

        yield return new WaitForSeconds(data.groundPoundFreezeTime);

        cannotMove = false;

    }


    

    private void HandleAttackState()
    {
        if ((isIdleAttcking || isMovingAttcking) && Time.time >= attackStateEndTime)
        {
            isIdleAttcking = false;
            isMovingAttcking = false;
            SwitchAnimation("");
        }

        if (isAirAttcking && Time.time >= attackStateEndTime)
        {
            isAirAttcking = false;

            if (attackAction != null && attackAction.IsPressed() && canAttack)
            {
                StartStayAirAttack();
            }
            else
            {
                SwitchAnimation("");
            }
        }
    }

    private void HandleStayAirAttackMovement()
    {
        if (data == null) return;

        Vector3 inputDir;
        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        }
        else
        {
            inputDir = new Vector3(moveInput.x, 0f, 0f);
        }

        Vector3 horizontalVel = new Vector3(stayAirAttackVelocity.x, 0f, stayAirAttackVelocity.z);

        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Vector3 inputNorm = inputDir.normalized;

            if (horizontalVel.sqrMagnitude < data.stayAirAttackMinSpeed * data.stayAirAttackMinSpeed)
            {
                horizontalVel += inputNorm * data.stayAirAttackAccel * Time.fixedDeltaTime;
            }
            else
            {
                Vector3 velNorm = horizontalVel.normalized;
                float dot = Vector3.Dot(inputNorm, velNorm);

                if (dot >= 0f)
                {
                    horizontalVel += inputNorm * data.stayAirAttackAccel * Time.fixedDeltaTime;
                }
                else
                {
                    horizontalVel += inputNorm * data.stayAirAttackOppositeDecel * Time.fixedDeltaTime;
                }
            }
        }
        else
        {
            float speed = horizontalVel.magnitude;
            float newSpeed = Mathf.MoveTowards(speed, 0f, data.stayAirAttackOppositeDecel * 0.5f * Time.fixedDeltaTime);
            horizontalVel = (speed > 0f) ? horizontalVel.normalized * newSpeed : Vector3.zero;
        }

        if (horizontalVel.magnitude > data.stayAirAttackMaxSpeed)
            horizontalVel = horizontalVel.normalized * data.stayAirAttackMaxSpeed;

        stayAirAttackVelocity = new Vector3(horizontalVel.x, stayAirAttackVelocity.y, horizontalVel.z);

        Vector3 rbVel = rb.linearVelocity;
        rbVel.x = horizontalVel.x;
        rbVel.z = horizontalVel.z;
        rb.linearVelocity = rbVel;

        if (attackAction != null && !attackAction.IsPressed())
        {
            isStayAirAttacking = false;
            SwitchAnimation("");
        }
    }

    public bool CanDash()
    {
        if (!canDash || data == null) return false;

        if (!isDashing && dashesLeft < data.dashAmount &&
            (lastOnGroundTime > 0f || lastOnWallTime > 0f) && !dashRefilling)
        {
            StartCoroutine(RefillDash(1));
        }

        return dashesLeft > 0;
    }

    public IEnumerator RefillDash(int amount)
    {
        dashRefilling = true;
        isDashRefilling = true;

        yield return new WaitForSeconds(data.dashRefillTime);

        dashRefilling = false;
        isDashRefilling = false;

        dashesLeft = Mathf.Min(data.dashAmount, dashesLeft + amount);
    }


    

    private bool CanJump()
    {
        if (isStayAirAttacking)
            return false;

        return lastOnGroundTime > 0f;
    }

    private bool CanJumpCut()
    {
        return isJumping && rb.linearVelocity.y > 0f;
    }

    private bool CanWallJump()
    {
        if (!canWallJump) return false;
        if (IsWallSlippery()) return false;
        if (isStayAirAttacking) return false;

        return (lastPressedJumpTime > 0 &&
                lastOnWallTime > 0 &&
                lastOnGroundTime <= 0 &&
                (!isWallJumping ||
                 (lastOnWallRightTime > 0 && lastWallJumpDir == 1) ||
                 (lastOnWallLeftTime > 0 && lastWallJumpDir == -1)));
    }

    private bool IsWallSlippery()
    {
        if (frontWallCheckPoint == null || backWallCheckPoint == null) return false;

        Collider[] frontHits = Physics.OverlapBox(
            frontWallCheckPoint.position,
            wallCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        foreach (var hit in frontHits)
        {
            if (hit.CompareTag("Slippery"))
                return true;
        }

        Collider[] backHits = Physics.OverlapBox(
            backWallCheckPoint.position,
            wallCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        foreach (var hit in backHits)
        {
            if (hit.CompareTag("Slippery"))
                return true;
        }

        return false;
    }

    public bool CanSlide()
    {
        if (IsWallSlippery())
            return false;
        
        if (isStayAirAttacking)
            return false;

        return lastOnWallTime > 0f
               && lastOnGroundTime <= 0f
               && !isJumping
               && !isWallJumping
               && !isDashing
               && rb.linearVelocity.y <= 0.01f;
    }


    #endregion

    #region MOVEMENT

    private void HandleMovement2D()
    {
        if (data == null) return;

        float currentVelX = rb.linearVelocity.x;
        float desiredSpeed = targetSpeed;
        float accelRate;
        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(desiredSpeed) > 0.01f) ? data.runAccelAmount : data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(desiredSpeed) > 0.01f)
                ? data.runAccelAmount * data.accelInAir
                : data.runDeccelAmount * data.deccelInAir;

        if ((isJumping || isWallJumping || isJumpFalling) &&
            Mathf.Abs(rb.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            accelRate *= data.jumpHangAccelerationMult;
            desiredSpeed *= data.jumpHangMaxSpeedMult;
        }

        if (data.doConserveMomentum &&
            Mathf.Abs(currentVelX) > Mathf.Abs(desiredSpeed) &&
            Mathf.Sign(currentVelX) == Mathf.Sign(desiredSpeed) &&
            Mathf.Abs(desiredSpeed) > 0.01f &&
            lastOnGroundTime < 0)
        {
            accelRate = 0;
        }

        float speedDif = desiredSpeed - currentVelX;
        float movement = speedDif * accelRate;

        rb.AddForce(Vector3.right * movement, ForceMode.Force);

        //animation

        if (isJumping || isJumpFalling || isIdleAttcking || isMovingAttcking || isAirAttcking || isStayAirAttacking) return;

        if (Mathf.Abs(moveInput.x) == 0)
        {
            SwitchAnimation("");
        }
        else if (Mathf.Abs(moveInput.x) >= 0.5f)
        {
            SwitchAnimation("isRunning");
        }
        else
        {
            SwitchAnimation("isWalking");
        }
    }

    private void HandleMovement3D()
    {
        if (data == null) return;

        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float inputMag = Mathf.Clamp01(inputDir.magnitude);
        float targetSpeedMagnitude = data.runMaxSpeed * inputMag;
        Vector3 desiredVel = (inputMag > 0.01f) ? inputDir.normalized * targetSpeedMagnitude : Vector3.zero;

        float accelRate;
        if (lastOnGroundTime > 0)
            accelRate = (targetSpeedMagnitude > 0.01f) ? data.runAccelAmount : data.runDeccelAmount;
        else
            accelRate = (targetSpeedMagnitude > 0.01f)
                ? data.runAccelAmount * data.accelInAir
                : data.runDeccelAmount * data.deccelInAir;

        if ((isJumping || isWallJumping || isJumpFalling) &&
            Mathf.Abs(rb.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            accelRate *= data.jumpHangAccelerationMult;
            desiredVel *= data.jumpHangMaxSpeedMult;
        }

        Vector3 speedDif = desiredVel - horizontalVel;
        Vector3 movement = speedDif * accelRate;

        rb.AddForce(movement, ForceMode.Force);

        //animation

        if (isJumping || isJumpFalling || isIdleAttcking || isMovingAttcking || isAirAttcking || isStayAirAttacking) return;

        if (Mathf.Abs(moveInput.x) == 0 && Mathf.Abs(moveInput.y) == 0)
        {
            SwitchAnimation("");
        }
        else if (Mathf.Abs(moveInput.x) >= 0.5f || Mathf.Abs(moveInput.y) >= 0.5f)
        {
            SwitchAnimation("isRunning");
        }
        else
        {
            SwitchAnimation("isWalking");
        }
    }

    private void Jump()
    {
        if (cannotMove) return;

        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;

        float force = data.jumpForce;

        if (rb.linearVelocity.y < 0)
        {
            force -= rb.linearVelocity.y;
        }

        SwitchAnimation("isJumping");

        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }

    private void WallJump(int dir)
    {
        if (!canWallJump || data == null) return;

        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;
        lastOnWallRightTime = 0;
        lastOnWallLeftTime = 0;

        // SoundManager.Instance.PlayRandomSFX(clipsRandomWalljump, 0.9f, 1.1f);

        Vector3 force = new Vector3(data.wallJumpForce.x * dir, data.wallJumpForce.y, 0f);

        if (Mathf.Sign(rb.linearVelocity.x) != Mathf.Sign(force.x))
            force.x -= rb.linearVelocity.x;

        if (rb.linearVelocity.y < 0)
            force.y -= rb.linearVelocity.y;

        rb.AddForce(force, ForceMode.Impulse);
    }

    private void Slide3D()
    {
        if (data == null) return;

        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }

        float targetY = data.slideSpeed;
        float speedDif = targetY - rb.linearVelocity.y;
        float movement = speedDif * data.slideAccel;

        float maxForce = Mathf.Abs(speedDif) / Time.fixedDeltaTime;
        movement = Mathf.Clamp(movement, -maxForce, maxForce);

        rb.AddForce(Vector3.up * movement, ForceMode.Force);

        Vector3 vel = rb.linearVelocity;
        vel.z = 0f;
        rb.linearVelocity = vel;
    }

    IEnumerator StartDash(Vector3 dir)
    {
        if (!canDash || data == null)
        {
            isDashing = false;
            yield break;
        }

        SwitchAnimation("isDashing");

        float dashCompressFactor = 0.6f;
        float dashStretchFactor = 1.9f;

        Transform dashChild = null;
        Vector3 childOriginalScale = Vector3.one;
        Quaternion childOriginalRotation = Quaternion.identity;

        if (transform.childCount > 0)
        {
            dashChild = transform.GetChild(0);
            childOriginalScale = dashChild.localScale;
            childOriginalRotation = dashChild.localRotation;
        }


        Tween stretchTween = null;
        if (dashChild != null)
        {
            float dashDuration = data.dashAttackTime;
            Vector3 startScale = dashChild.localScale;
            Vector3 targetScale = new Vector3(
                startScale.x * dashStretchFactor,
                startScale.y * dashCompressFactor,
                startScale.z
            );
            
            Vector2 animInput = moveInput;
            
            if (!isFacingRight)
            {
                animInput.x *= -1f;
            }

            if (animInput.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(animInput.y, animInput.x) * Mathf.Rad2Deg;

                Vector3 baseEuler = childOriginalRotation.eulerAngles;
                dashChild.localRotation = Quaternion.Euler(baseEuler.x, baseEuler.y, angle);
            }
            else
            {
                dashChild.localRotation = childOriginalRotation;
            }

            stretchTween = dashChild
                .DOScale(targetScale, dashDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    dashChild.DOScale(startScale, dashDuration * 0.5f)
                             .SetEase(Ease.OutQuad);
                });
        }

        lastOnGroundTime = 0f;
        lastPressedDashTime = 0f;
        lastPressedJumpTime = 0f;

        float startTime = Time.time;

        dashesLeft--;
        isDashAttacking = true;

        SetGravityScale(0f);
        Vector3 dashDir;
        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            dashDir = new Vector3(dir.x, 0f, dir.z).normalized;
        }
        else
        {
            dashDir = new Vector3(dir.x, dir.y, 0f).normalized;
        }

        float currentAlongDash = Vector3.Dot(rb.linearVelocity, dashDir);
        
        float targetDashSpeed = currentAlongDash + data.dashSpeed;
        
        targetDashSpeed = Mathf.Clamp(targetDashSpeed, data.dashSpeed, data.dashMaxSpeed);


     
        
        while (Time.time - startTime <= data.dashAttackTime)
        {
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                if (TryDashJump(dashDir, dashChild, childOriginalScale, childOriginalRotation, stretchTween))
                    yield break;
            }

            Vector3 vel = rb.linearVelocity;
            
            float currentAlongNow = Vector3.Dot(vel, dashDir);
            
            Vector3 velPerp = vel - dashDir * currentAlongNow;
            
            Vector3 dashVel = dashDir * targetDashSpeed;

            rb.linearVelocity = velPerp + dashVel;

            yield return null;
        }

        
        if (stretchTween != null && stretchTween.IsActive())
            stretchTween.Kill();
        if (dashChild != null)
        {
            dashChild.localScale = childOriginalScale;
            dashChild.localRotation = childOriginalRotation;
        }

        isDashAttacking = false;
        
        if (rb.linearVelocity.y > 0f)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y *= 0.42f;
            rb.linearVelocity = vel;
        }

        SetGravityScale(data.gravityScale);

        isDashing = false;
    }

    private IEnumerator StartGroundPound()
    {
        if (GameManager.instance != null && GameManager.instance.is3d)
            yield break;

        if (data == null)
            yield break;

        isGroundPounding = true;
        isDashAttacking = false;
        isDashing = false;
        isJumping = false;
        isWallJumping = false;
        isJumpCut = false;
        isJumpFalling = false;
        isStayAirAttacking = false;
        
        Transform child = null;
        if (transform.childCount > 0)
        {
            child = transform.GetChild(0);
            Vector3 euler = child.localRotation.eulerAngles;
            child.localRotation = Quaternion.Euler(euler.x, euler.y, 0f);
        }

        SwitchAnimation("isGroundPound");
        
        SetGravityScale(0f);
        while (!wasGroundedLastFrame)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = 0f;
            vel.z = 0f;

            vel.y = -data.groundPoundSpeed; 
            rb.linearVelocity = vel;

            yield return null;
        }
        
        isGroundPounding = false;
        
        rb.linearVelocity = Vector3.zero;
        
        SetGravityScale(data.gravityScale);
        
        StartCoroutine(GroundPoundLanding());
    }



    private void StartIdleAttack()
    {
        if (data == null) return;

        isIdleAttcking = true;
        isMovingAttcking = false;
        isAirAttcking = false;
        isStayAirAttacking = false;

        attackStateEndTime = Time.time + data.idleAttackTime;

        SwitchAnimation("isIdleAttack");
    }
    
    private bool TryDashJump(Vector3 dashDir, Transform dashChild, Vector3 childOriginalScale, Quaternion childOriginalRotation, Tween stretchTween)
    {
        if (data == null || cannotMove)
            return false;

        Debug.Log("TryDashJump pendant le dash");
        
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Vector3 effectiveNormal = Vector3.up;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, data.dashJumpMaxDistance, groundLayer))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            if (slopeAngle <= data.dashJumpMaxSlopeAngle)
            {
                effectiveNormal = hit.normal;
            }
        }
        
        if (stretchTween != null && stretchTween.IsActive())
            stretchTween.Kill();

        if (dashChild != null)
        {
            dashChild.localScale = childOriginalScale;
            dashChild.localRotation = childOriginalRotation;
        }
        
        isDashAttacking = false;
        isDashing = false;

        SetGravityScale(data.gravityScale);

        isJumping = true;
        isWallJumping = false;
        isJumpCut = false;
        isJumpFalling = false;

        lastPressedJumpTime = 0f;
        lastOnGroundTime = 0f;


        Vector3 vel = rb.linearVelocity;
        
        Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);
        
        Vector3 upDir = Vector3.Lerp(Vector3.up, effectiveNormal, 0.4f).normalized;
        
        float upSpeed = data.dashJumpUpSpeed;

        float currentAlongUp = Vector3.Dot(vel, upDir);
        Vector3 velPerpToUp = vel - upDir * currentAlongUp;

        Vector3 finalVel = velPerpToUp + upDir * upSpeed;
        
        if (finalVel.y > data.dashJumpMaxUpSpeed)
        {
            finalVel.y = data.dashJumpMaxUpSpeed;
        }

        rb.linearVelocity = finalVel;
        
        SwitchAnimation("isJumping");

        return true;
    }




    private void StartMovingAttack()
    {
        if (data == null) return;

        isIdleAttcking = false;
        isMovingAttcking = true;
        isAirAttcking = false;
        isStayAirAttacking = false;

        attackStateEndTime = Time.time + data.movingAttackTime;

        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            SwitchAnimation("is3DAttack");
        }
        else
        {
            SwitchAnimation("is2DAttack");
        }
        
    }

    private void StartAirAttack()
    {
        if (data == null) return;

        isIdleAttcking = false;
        isMovingAttcking = false;
        isAirAttcking = true;
        isStayAirAttacking = false;

        attackStateEndTime = Time.time + data.airAttackTime;

        SwitchAnimation("isAirAttack");
    }

    private void StartStayAirAttack()
    {
        if (data == null) return;

        isIdleAttcking = false;
        isMovingAttcking = false;
        isAirAttcking = false;
        isStayAirAttacking = true;
    
        float rawHeight = Mathf.Max(0f, lastJumpMaxY - lastGroundY);
        if (rawHeight <= 0.01f)
            rawHeight = data.minHeightBounce;
    
        rawHeight = Mathf.Clamp(rawHeight, data.minHeightBounce, data.maxHeightBounce);
        
        stayAirCurrentHeight = rawHeight;

        Vector3 vel = rb.linearVelocity;
        stayAirAttackVelocity = new Vector3(vel.x, 0f, vel.z);

        if (vel.y > 0f)
        {
            vel.y = 0f;
            rb.linearVelocity = vel;
        }

        SwitchAnimation("isStayAttack");
    }


    #endregion

    #region COLLISION SLOPE ALIGN & FRICTION

    private void OnCollisionEnter(Collision collision)
    {
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        if (collision.contactCount == 0) return;
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        if (collision.contactCount == 0) return;

        ContactPoint bestContact = collision.GetContact(0);
        for (int i = 1; i < collision.contactCount; i++)
        {
            var c = collision.GetContact(i);
            if (c.normal.y > bestContact.normal.y)
                bestContact = c;
        }

        Vector2 n2D = new Vector2(bestContact.normal.x, bestContact.normal.y).normalized;
        if (n2D.sqrMagnitude < 0.0001f)
            return;

        float normalAngle = Mathf.Atan2(n2D.y, n2D.x) * Mathf.Rad2Deg;
        float surfaceAngle = normalAngle - 90f;

        if (surfaceAngle >= -45f && surfaceAngle <= 45f)
        {
            Vector3 euler = transform.eulerAngles;
            euler.z = surfaceAngle;
            transform.rotation = Quaternion.Euler(euler);
        }

        bool noMoveInput = moveInput.sqrMagnitude < 0.001f;

        bool withinFrictionAngle =
            surfaceAngle >= -maxAngleWithFriction &&
            surfaceAngle <= maxAngleWithFriction;

        bool onGround = lastOnGroundTime > 0f;

        if (withinFrictionAngle && noMoveInput && onGround)
        {
            if (collider != null && frictionMaterial != null)
                collider.sharedMaterial = frictionMaterial;
        }
        else
        {
            if (collider != null && noFrictionMaterial != null)
                collider.sharedMaterial = noFrictionMaterial;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        Vector3 euler = transform.eulerAngles;
        euler.z = 0f;
        transform.rotation = Quaternion.Euler(euler);

        if (collider != null && noFrictionMaterial != null)
            collider.sharedMaterial = noFrictionMaterial;
    }

    #endregion

    #region MISC

    public void SwitchAnimation(string animationName)
    {
        playerAnimator.SetBool("isWalking", false);
        playerAnimator.SetBool("isRunning", false);
        playerAnimator.SetBool("isJumping", false);
        playerAnimator.SetBool("isFalling", false);
        playerAnimator.SetBool("isSliding", false);
        playerAnimator.SetBool("isDamage", false);
        playerAnimator.SetBool("isDashing", false);
        playerAnimator.SetBool("isCAC", false);
        playerAnimator.SetBool("isPuching", false);
        playerAnimator.SetBool("isSlidingDown", false);
        playerAnimator.SetBool("isAirAttack", false);
        playerAnimator.SetBool("is2DAttack", false);
        playerAnimator.SetBool("is3DAttack", false);
        playerAnimator.SetBool("isIdleAttack", false);
        playerAnimator.SetBool("isStayAttack", false);
        playerAnimator.SetBool("isGroundPound", false);
        playerAnimator.SetBool("isLanded", false);

        if (string.IsNullOrEmpty(animationName)) return;
        playerAnimator.SetBool(animationName, true);

    }

    #region GROUND CALLBACKS

    private void TouchGround()
    {
        if (isStayAirAttacking && data != null)
        {
            float previousHeight;

            if (stayAirCurrentHeight <= 0f)
            {
                float rawHeight = Mathf.Max(0f, lastJumpMaxY - lastGroundY);
                if (rawHeight <= 0.01f)
                    rawHeight = data.minHeightBounce;

                rawHeight = Mathf.Clamp(rawHeight, data.minHeightBounce, data.maxHeightBounce);
                previousHeight = rawHeight;
            }
            else
            {
                previousHeight = stayAirCurrentHeight;
            }

            float nextHeight = previousHeight / data.nextBounceDivision;
            
            nextHeight = Mathf.Clamp(nextHeight, data.minHeightBounce, data.maxHeightBounce);

            if (Time.time - lastJumpButtonTime <= data.bonusBounceMarge)
            {
                nextHeight *= data.bonusBounceMult;
                nextHeight = Mathf.Clamp(nextHeight, data.minHeightBounce, data.maxHeightBounce);
            }
            
            stayAirCurrentHeight = nextHeight;
            
            float effectiveGravity = Mathf.Abs(data.gravityStrength);
            if (effectiveGravity < 0.0001f)
            {

                effectiveGravity = Mathf.Abs(Physics.gravity.y);
            }

            float bounceVelocity = Mathf.Sqrt(2f * effectiveGravity * nextHeight);

            Vector3 vel = rb.linearVelocity;
            vel.y = bounceVelocity;
            rb.linearVelocity = vel;
            
            Debug.Log($"[StayAir] prev={previousHeight:F3} next={nextHeight:F3} g={effectiveGravity:F3} v={bounceVelocity:F3}");
        }
        
        trackJumpHeight = false;

        isSliding = false;
        isJumping = false;
        isWallJumping = false;
        isJumpCut = false;
        isJumpFalling = false;

        TweenBounce();
    }


    private void LeaveGround()
    {
        isIdleAttcking = false;
        isMovingAttcking = false;
        attackStateEndTime = 0f;
        
        lastGroundY = transform.position.y;
        lastJumpMaxY = transform.position.y;
        trackJumpHeight = true;

        TweenStretch(new Vector3(0.9f, 1.1f, 1f), 0.2f);
    }

    #endregion

    #endregion

    #region GIZMOS
    private void OnDrawGizmos()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }

        if (frontWallCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(frontWallCheckPoint.position, wallCheckSize);
        }

        if (backWallCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(backWallCheckPoint.position, wallCheckSize);
        }
        
        if (airAttackGroundCheckPoint != null)
        {
            Gizmos.color = Color.blue;

            float r = airAttackGroundRadius;
            float h = Mathf.Max(airAttackGroundHeight, r * 2f);

            Vector3 center = airAttackGroundCheckPoint.position;
            float half = (h * 0.5f);

            Vector3 top = center + Vector3.up * half;
            Vector3 bottom = center - Vector3.up * half;
            
            DrawCircle(top, r);
            DrawCircle(bottom, r);
            
            Gizmos.DrawLine(top + Vector3.forward * r, bottom + Vector3.forward * r);
            Gizmos.DrawLine(top - Vector3.forward * r, bottom - Vector3.forward * r);
            Gizmos.DrawLine(top + Vector3.right * r, bottom + Vector3.right * r);
            Gizmos.DrawLine(top - Vector3.right * r, bottom - Vector3.right * r);
        }
        
        
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0f), 0f, Mathf.Sin(0f)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint =
                center +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    #endregion

    #region DOTWEEN

    private Transform GetModelRoot()
    {
        if (transform.childCount > 0)
        {
            Transform firstChild = transform.GetChild(0);
            if (firstChild.childCount > 0)
                return firstChild.GetChild(0);

            return firstChild;
        }

        return transform;
    }

    public Tween TweenSquish(float duration = 0.5f, float squishX = 0.9f, float stretchY = 1.1f)
    {
        Transform model = GetModelRoot();
        if (model == null) return null;

        Sequence seq = DOTween.Sequence();

        seq.Append(model.DOScale(
                new Vector3(
                    originalScale.x * squishX,
                    originalScale.y * stretchY,
                    originalScale.z),
                duration * 0.5f
            )
            .SetEase(Ease.OutElastic));

        seq.Append(model.DOScale(
                originalScale,
                duration * 0.5f
            )
            .SetEase(Ease.OutElastic));

        return seq.Play();
    }

    public Tween TweenBounce(float squishDuration = 0.1f, float recoverDuration = 0.4f)
    {
        Transform model = GetModelRoot();
        if (model == null) return null;

        Sequence seq = DOTween.Sequence();

        seq.Append(model.DOScale(
                new Vector3(1.7f, 0.5f, 1f),
                squishDuration
            )
            .SetEase(Ease.OutQuad));

        seq.Append(model.DOScale(
                originalScale,
                recoverDuration
            )
            .SetEase(Ease.OutElastic, 2f));

        return seq.Play();
    }

    public Tween TweenStretch(Vector3 stretchFactors, float duration = 0.2f, Ease ease = Ease.Linear)
    {
        Transform model = GetModelRoot();
        if (model == null) return null;

        Vector3 startScale = model.localScale;
        Vector3 targetScale = new Vector3(
            startScale.x * stretchFactors.x,
            startScale.y * stretchFactors.y,
            startScale.z * stretchFactors.z);

        Sequence seq = DOTween.Sequence();

        seq.Append(model.DOScale(targetScale, duration * 0.5f).SetEase(ease));

        seq.Append(model.DOScale(startScale, duration * 0.5f).SetEase(ease));

        return seq.Play();
    }

    public Tween TweenRotate360Y(float duration = 0.5f, Ease ease = Ease.Linear)
    {
        Transform model = GetModelRoot();
        if (model == null) return null;

        int dir = UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;

        return model
            .DOLocalRotate(
                new Vector3(0f, 360f * dir, 0f),
                duration,
                RotateMode.FastBeyond360
            )
            .SetEase(ease)
            .OnComplete(() => model.localEulerAngles = Vector3.zero);
    }

    #endregion
}
