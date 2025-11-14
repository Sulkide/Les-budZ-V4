using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public float maxAngleWithFriction = 30f;
    public bool canWallJump = true;

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

    public bool isFacingRight { get; private set; }
    public bool isJumping { get; private set; }
    public bool isWallJumping { get; private set; }
    public bool isDashing { get; private set; }
    public bool isSliding { get; private set; }
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
        
        lastOnGroundTime     -= Time.deltaTime;
        lastOnWallTime       -= Time.deltaTime;
        lastOnWallLeftTime   -= Time.deltaTime;
        lastOnWallRightTime  -= Time.deltaTime;
        lastPressedJumpTime  -= Time.deltaTime;


        GroundCheck3D();
        WallCheck3D();
        
        HandleFacing();

        HandleJumpState();
        HandleJumpBuffer();
    }

    void FixedUpdate()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused) return;

        if (GameManager.instance != null)
        {
            GameManager.instance.FindPlayer(parent.name, gameObject.transform, this);
            GameManager.instance.CharacterCheck(parent.name, data.playerName);
        }

        bool pushingIntoWall =
            (lastOnWallLeftTime > 0f  && moveInput.x < -0.01f) ||
            (lastOnWallRightTime > 0f && moveInput.x >  0.01f);

        if (CanSlide() && pushingIntoWall)
        {
            isSliding = true;
            if (playerAnimator != null)
                playerAnimator.SetBool("isSliding", true);
            
            rb.constraints = defaultConstraints | RigidbodyConstraints.FreezePositionZ;
        }
        else
        {
            isSliding = false;
            if (playerAnimator != null)
                playerAnimator.SetBool("isSliding", false);
            
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
        Debug.Log("OnAttackReleased");
    }

    private void OnAttackPressed(InputAction.CallbackContext obj)
    {
        Debug.Log("OnAttackPressed");
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
        Debug.Log("OnDashPressed");
    }

    private void OnJumpReleased(InputAction.CallbackContext obj)
    {
        if (CanJumpCut())
            isJumpCut = true;
    }

    private void OnJumpPressed(InputAction.CallbackContext obj)
    {
        if (cannotMove || data == null) return;
        lastPressedJumpTime = data.jumpInputBufferTime;
    }

    #endregion

    #region GRAVITY / GENERAL

    public void SetGravityScale(float scale)
    {
        rb.useGravity = false;
        gravityScale = scale;
    }


    private void ApplyCustomGravity()
    {
        if (data == null || isSliding) return;

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
        if (groundCheckPoint == null) return;

        bool grounded = Physics.CheckBox(
            groundCheckPoint.position,
            groundCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        if (grounded)
        {
            lastOnGroundTime = data != null ? data.coyoteTime : 0.1f;
        }
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

    private void SlideCheck3D()
    {
        
        if (CanSlide() && ((lastOnWallLeftTime > 0 && moveInput.x < 0) ||
                           (lastOnWallRightTime > 0 && moveInput.x > 0)))
        {
            isSliding = true;
            if (playerAnimator != null)
                playerAnimator.SetBool("isSliding", true);
        }
        else
        {
            isSliding = false;
            if (playerAnimator != null)
                playerAnimator.SetBool("isSliding", false);
        }
        
        if (isSliding)
        {
            Slide3D();
        }

    }
    #endregion


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

  

    #region JUMP LOGIC

    private void HandleJumpState()
    {
        if (isJumping && rb.velocity.y < 0f)
        {
            isJumping = false;
            isJumpFalling = true;
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




    private bool CanJump()
    {
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
        // Pas de slide sur les murs "Slippery"
        if (IsWallSlippery())
            return false;

        // En l'air, collé à un mur, pas en saut, pas en dash
        // et on ne slide que si on ne monte plus
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

    }
    
    
    private void Jump()
    {
        if (cannotMove) return;

        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;

        float force = data.jumpForce;
        
        if (rb.linearVelocity.y < 0)
            force -= rb.linearVelocity.y;

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

        // Si on a encore une vitesse vers le haut, on la casse
        if (rb.velocity.y > 0f)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        }

        // On veut tendre vers data.slideSpeed (valeur NÉGATIVE, par ex -6)
        float targetY = data.slideSpeed;
        float speedDif = targetY - rb.velocity.y;          // combien il manque pour atteindre la vitesse de slide
        float movement = speedDif * data.slideAccel;       // accélération de slide

        // Clamp comme en 2D
        float maxForce = Mathf.Abs(speedDif) / Time.fixedDeltaTime;
        movement = Mathf.Clamp(movement, -maxForce, maxForce);

        // Force uniquement sur Y
        rb.AddForce(Vector3.up * movement, ForceMode.Force);

        // 🔒 On verrouille la vitesse en Z pendant la slide
        Vector3 vel = rb.velocity;
        vel.z = 0f;
        rb.velocity = vel;
    }


    #endregion
    
    #region COLLISION SLOPE ALIGN & FRICTION

    private void OnCollisionStay(Collision collision)
    {
        // On ne s'intéresse qu'au sol
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        if (collision.contactCount == 0) return;

        // On prend le contact avec la normale la plus "vers le haut"
        ContactPoint bestContact = collision.GetContact(0);
        for (int i = 1; i < collision.contactCount; i++)
        {
            var c = collision.GetContact(i);
            if (c.normal.y > bestContact.normal.y)
                bestContact = c;
        }

        // ---- ALIGNEMENT SUR LA PENTE (comme en 2D) ----
        // On ne regarde que X/Y, on ignore Z
        Vector2 n2D = new Vector2(bestContact.normal.x, bestContact.normal.y).normalized;
        if (n2D.sqrMagnitude < 0.0001f)
            return;

        float normalAngle  = Mathf.Atan2(n2D.y, n2D.x) * Mathf.Rad2Deg;
        float surfaceAngle = normalAngle - 90f;

        // On limite l'inclinaison du perso à [-45°, 45°] comme dans ton script 2D
        if (surfaceAngle >= -45f && surfaceAngle <= 45f)
        {
            Vector3 euler = transform.eulerAngles;
            euler.z = surfaceAngle;
            transform.rotation = Quaternion.Euler(euler);
        }

        // ---- GESTION FRICTION / ANTI-GLISSADE ----
        // Pas d'input (ni en X, ni en Z → moveInput.x / moveInput.y)
        bool noMoveInput = moveInput.sqrMagnitude < 0.001f;

        // On ne fige que les pentes "raisonnables"
        bool withinFrictionAngle =
            surfaceAngle >= -maxAngleWithFriction &&
            surfaceAngle <=  maxAngleWithFriction;

        // lastOnGroundTime > 0 => on est "considéré au sol" (coyote time)
        bool onGround = lastOnGroundTime > 0f;

        if (withinFrictionAngle && noMoveInput && onGround)
        {
            // Le joueur NE BOUGE PAS → on met de la friction pour qu'il ne glisse pas
            if (collider != null && frictionMaterial != null)
                collider.sharedMaterial = frictionMaterial;
        }
        else
        {
            // Soit la pente est trop raide, soit le joueur bouge, soit en l'air -> pas de friction
            if (collider != null && noFrictionMaterial != null)
                collider.sharedMaterial = noFrictionMaterial;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // On ne s'intéresse qu'au sol
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        // On redresse le perso quand il quitte ce sol
        Vector3 euler = transform.eulerAngles;
        euler.z = 0f;
        transform.rotation = Quaternion.Euler(euler);

        // Et on remet le matériau sans friction
        if (collider != null && noFrictionMaterial != null)
            collider.sharedMaterial = noFrictionMaterial;
    }

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
    }
    #endregion

}
