using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    public bool cannotMove;

    public PlayerInput playerControls;

    [UnityEngine.Range(1f, 4f)] public int currentPlayerID;

    public string parentName;
    private Transform playerTransform;
    
    public int currentLife = 5;

    
    public bool isDead;

    [SerializeField] private Animator playerAnimator;
    public bool canWallJump = true;
    public bool canDash = true;
    public bool canShoot = true;
    public bool canGrap = true;
	public bool canBump = true;
    public GameObject[] characters;


    private float requiredTimeToFall = 1f;
    private float timerFall;
    private int currentIndex = -1;
    private bool jumpButtonIsPressed;
    private bool hasPlayedAnimationLanded = true;
    private Vector2 lastPosition;
    public GameObject[] characterReference;

    public Transform pivot;
    public bool flipAimArm;
    public float pivotCorrection;

    public Transform projectileStartPoint;
    public GameObject DashPointForPlayer;
    public GameObject cacPoint;
    public GameObject dashPoint;

    public GameObject armOriginal;
    public GameObject armAim;

    public bool HasCurrentlyHealthbonus;
    public GameObject fairyAnimation;
    public GameObject fairyDeathPrebfab;


    private float currentCACTime = 0;
    private float currentRecoverTime = 0;
    private float currentRecoverReset = 0;
    public float damageRecoveryDuration = 1f;
    private bool contactGround;
    public bool collisionGround;
    private int originalLayer;
    private bool isBumping;

    private Vector2 capsuleSize;
    private Vector2 capsuleOffset;
    
    private Vector3 originalScale;

    private Collider2D collider2D;

    [SerializeField] private GameObject projectilePrefab;

    private float currentCoolDown = 0;

    public PlayerData Data;

    private bool isGroundSliding;

    
    List<string> clipsRandomImpact = new List<string> { "impact1", "impact2", "impact3", "impact4" };
    List<string> clipsRandomDeath = new List<string> { "deathBell1" };
    List<string> clipsRandomSlap = new List<string> { "slap1" };
    List<string> clipsRandomjump = new List<string> { "jump1" };
    List<string> clipsRandomWalljump = new List<string> { "wall jump" };
    List<string> clipsRandomDash = new List<string> { "dash1" };
        
    #region COMPONENTS

    public Rigidbody2D RB { get; private set; }

    #endregion

    #region STATE PARAMETERS

    public bool isFacingRight { get; private set; }
    public bool isJumping { get; private set; }
    public bool isWallJumping { get; private set; }
    public bool isDashing { get; private set; }
    public bool isSliding { get; private set; }


    public float lastOnGroundTime { get; private set; }

    public bool fixedLastOnGroundTime { get; private set; }

    public float lastOnWallTime { get; private set; }
    public float lastOnWallRightTime { get; private set; }
    public float lastOnWallLeftTime { get; private set; }


    private bool isJumpCut;
    private bool isJumpFalling;


    private float wallJumpStartTime;
    private int lastWallJumpDir;


    private int dashesLeft;
    private bool dashRefilling;
    private Vector2 lastDashDir;
    private bool isDashAttacking;

    private float targetSpeed;

    #endregion

    #region INPUT PARAMETERS

    public Vector2 moveInput;

    public float lastPressedJumpTime { get; private set; }
    public float lastPressedDashTime { get; private set; }

    #endregion

    #region CHECK PARAMETERS

    [Header("Checks")] [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)] [SerializeField] public Transform frontWallCheckPoint;
    [SerializeField] public Transform backWallCheckPoint;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.5f, 1f);

    #endregion

    #region LAYERS & TAGS

    [Header("Layers & Tags")] [SerializeField]
    private LayerMask groundLayer;

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask enemyPorjectileLayer;

    #endregion

    [Header("Friction Materials")] public PhysicsMaterial2D frictionMaterial;
    public PhysicsMaterial2D noFrictionMaterial;
    public float maxAngleWithFriction = 30f;

    [Header("Ground Slide Settings")] public float groundSlideMaxSpeed = 8f;
    public float groundSlideDeceleration = 12f;


    // Ajoutez ces variables en début de classe (par exemple, après vos autres variables publiques)
    [Header("Grappling Hook Settings")]
    public Transform grappleOrigin; // Le point sur le joueur d'où partira le grappin

    public float grappleMaxDistance = 10f; // Distance maximale du grappin
    public LayerMask grappleLayer; // Layer(s) autorisé(s) pour l'attachement
    private LineRenderer grappleLine; // Pour dessiner la corde
    public bool isGrappling = false;
    private Vector2 grapplePoint; // Position où le grappin s'est attaché
    public float swingBoostMultiplier = 2f;
    private DistanceJoint2D grappleJoint;
    public float grapplePullForce = 200f;
    private float initialRopeLength = 0f;
    private float originalGrappleTargetGravityScale;
    private bool grappleGravityModified = false; // Ajustez cette valeur selon le comportement souhaité 


    [Header("Marker Settings")]
    public GameObject grappleAnchorMarkerPrefab; // Assignez dans l'inspecteur votre prefab de marqueur

    private GameObject activeGrappleAnchorMarker;
    private List<Collider2D> selfColliders;
    private Transform grappleTarget; // Référence à l'objet touché
    private Vector2 grappleTargetLocalPoint;

    [Header("BaseModelPrefab")] public GameObject baseModelPrefab;

    [Header("Moving Platforme")] private Transform currentMovingPlatform = null;
    private Vector2 lastPlatformPosition;

    [Header("Gravity")] public bool isGravityOff;
    private bool flipGravityState;
    public float gravityJumpForce = 45f;
    public Collider2D gravityCollider;
    private bool justGetOutOfPlanet;

// ─── En haut de la classe, ajoutez ces réglages ───
    [Header("Glide Flight Settings")] public bool canGlide = true;
    [Tooltip("Vitesse minimale en plané")] public float minGlideSpeed = 2f;
    [Tooltip("Vitesse maximale en plané")] public float maxGlideSpeed = 15f;

    [Tooltip("Accélération (en unité de vitesse/s) quand on plonge (moveInput.y < 0)")]
    public float diveAcceleration = 5f;

    [Tooltip("Décélération (en unité de vitesse/s) quand on remonte (moveInput.y > 0)")]
    public float climbDeceleration = 5f;

    [Tooltip("Vitesse à laquelle on pivote vers la direction voulue (1 = tournage en 1 s)")]
    public float steeringSpeed = 2f;

    [Tooltip("Multiplicateur de gravité en plané (0 = plus de gravité, 1 = plein effet)")]
    public float glideGravityMult = 0.5f;

    [Tooltip("Vitesse de chute constante (en unités/s) lorsque la vitesse de plané est inférieure à minGlideSpeed")]
    public float stallFallSpeed = 2f;

// flag pour savoir si on vient tout juste d’entrer en chute lente
    private bool isLowSpeedFalling = false;

// accélération calculée à l’entrée en chute lente pour atteindre max en 2s
    private float lowSpeedFallAccelDynamic;

    [Tooltip("Vitesse horizontale max (en unités/s) lors de la chute lente")]
    public float stallHorizontalSpeed = 1f;

    public float lowSpeedFallAcceleration = 10f;

// Temps passé en chute lente (stall)
    private float stallTimer = 1f;

    [Tooltip("Durée max (en s) de chute lente avant d'annuler le plané")]
    public float maxStallDuration = 1f;

    [Tooltip("Vitesse horizontale max (en unités/s) lors de la chute lente")]
// Flag pour bloquer temporairement le plané
    private bool glideDisabled = false;

    public float yHoldDecayDelay = 0.25f;

    [Tooltip("Vitesse (unités/s) à laquelle glideMove.y décroît vers -1")]
    public float yDecayRate = 2f;

// Variables internes
    private float yHoldTimer = 0f;
    private float lastHoldY = 0f;
    private bool yDecayEnabled = false;


// Garder la direction de vol courante
    private Vector2 glideDir = Vector2.down;

    // pour timer la chute lente
    private bool isStalling = false;

    public bool isGliding;

    private void Start()
    {
        
        
        capsuleSize = GetComponent<CapsuleCollider2D>().size;
        capsuleOffset = GetComponent<CapsuleCollider2D>().offset;
        gameObject.layer = LayerMask.NameToLayer("Default");
        Invoke("ChangeLayer", 1);
        originalLayer = gameObject.layer;
        SetGravityScale(Data.gravityScale);
        isFacingRight = true;
        canBump = true;
        RemovePlayerControl(false);
        cacPoint.SetActive(false);
        dashPoint.SetActive(false);
        grappleLayer = LayerMask.GetMask("Ground", "Player", "Enemy", "EnemyProjectile");
        collider2D = GetComponent<Collider2D>();
        if (gameObject.transform.parent != null)
        {
            parentName = gameObject.transform.parent.name;
        }

        GameManager.instance.addOrRemovePlayerBonus(parentName, Data.hasHealthBonus);

        if (parentName == "Player 1(Clone)")
        {
            HasCurrentlyHealthbonus = GameManager.instance.player1CurrentBonus;
            GameManager.instance.player1Bonus = GameManager.instance.player1CurrentBonus;
            currentLife = GameManager.instance.player1CurrentLifeSaved;

        }
        else if (parentName == "Player 2(Clone)")
        {
            HasCurrentlyHealthbonus = GameManager.instance.player2CurrentBonus;
            GameManager.instance.player2Bonus = GameManager.instance.player2CurrentBonus;
            currentLife = GameManager.instance.player2CurrentLifeSaved;
        }
        else if (parentName == "Player 3(Clone)")
        {
            HasCurrentlyHealthbonus = GameManager.instance.player3CurrentBonus;
            GameManager.instance.player3Bonus = GameManager.instance.player3CurrentBonus;
            currentLife = GameManager.instance.player3CurrentLifeSaved;
        }
        else if (parentName == "Player 4(Clone)")
        {
            HasCurrentlyHealthbonus = GameManager.instance.player4CurrentBonus;
            GameManager.instance.player4Bonus = GameManager.instance.player4CurrentBonus;
            currentLife = GameManager.instance.player4CurrentLifeSaved;
        }

        selfColliders = new List<Collider2D>(GetComponentsInChildren<Collider2D>());


        grappleLine = gameObject.AddComponent<LineRenderer>();
        grappleLine.startWidth = 0.1f;
        grappleLine.endWidth = 0.1f;
        grappleLine.material = new Material(Shader.Find("Sprites/Default"));
        grappleLine.startColor = Color.white;
        grappleLine.endColor = Color.white;
        grappleLine.positionCount = 0;
        
        
        
    }

    private void ChangeLayer()
    {
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    private void Awake()
    {
        cam = Camera.main;
        RB = GetComponent<Rigidbody2D>();
        playerControls = GetComponentInParent<PlayerInput>();
        originalScale = transform.localScale;
    }

    public bool areControllsRemoved = false;
    public bool useInputRegistered = false;

    private void Update()
    {
        if (GameManager.instance.isPaused) return;
        
        GameManager.instance.FindPlayer(parentName, gameObject.transform, this);
        GameManager.instance.CharacterCheck(parentName, Data.playerName);

        UIInput();

        if (!cam) return;

        // Convertir la position du joueur en coordonnées viewport
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);

        // L'objet est considéré visible s'il est dans le rectangle [0,1] x [0,1] et devant la caméra (viewportPos.z > 0)
        bool isVisible = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                         viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z > 0;

        // Si le joueur devient invisible et que le timer n'est pas déjà lancé
        if (!isVisible && !isOffScreen)
        {
            isOffScreen = true;
            offScreenCoroutine = StartCoroutine(OffScreenTimer());
        }
        // Si le joueur redevient visible avant l'expiration du timer
        else if (isVisible && isOffScreen)
        {
            isOffScreen = false;
            if (offScreenCoroutine != null)
            {
                StopCoroutine(offScreenCoroutine);
                offScreenCoroutine = null;
            }
        }

        FixedLastOnGroundTime();

        if (playerControls.actions["Use"].ReadValue<float>() > 0)
        {
            useInputRegistered = true;
        }
        else
        {
            useInputRegistered = false;
        }

        moveInput.x = playerControls.actions["Move"].ReadValue<Vector2>().x;
        moveInput.y = playerControls.actions["Move"].ReadValue<Vector2>().y;

        if (moveInput.y < -0.8f && collisionGround && !isDashing)
        {
            isGroundSliding = true;
        }
        else
        {
            isGroundSliding = false;
        }

        if (areControllsRemoved)
        {
            RB.linearVelocity = Vector2.zero;
            playerAnimator.SetBool("isWalking", false);
            playerAnimator.SetBool("isRunning", false);
            return;
        }


        if (HasCurrentlyHealthbonus && !isDead)
        {
            fairyAnimation.SetActive(true);
        }
        else
        {
            fairyAnimation.SetActive(false);
        }


        CheckCharacter();

        #region TIMERS

        lastOnGroundTime -= Time.deltaTime;
        lastOnWallTime -= Time.deltaTime;
        lastOnWallRightTime -= Time.deltaTime;
        lastOnWallLeftTime -= Time.deltaTime;

        lastPressedJumpTime -= Time.deltaTime;
        lastPressedDashTime -= Time.deltaTime;

        #endregion

        #region INPUT HANDLER

        if (moveInput.x != 0)
        {
            if (isGravityOff == false)
            {
                CheckDirectionToFace(moveInput.x > 0);

                if (justGetOutOfPlanet)
                {
                    if (moveInput.x > 0 && transform.localScale.x < 0 && lastOnGroundTime > 0)
                    {
                        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y,
                            transform.localScale.z);
                        justGetOutOfPlanet = false;
                    }
                    else if (moveInput.x < 0 && transform.localScale.x > 0 && lastOnGroundTime > 0)
                    {
                        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y,
                            transform.localScale.z);
                        justGetOutOfPlanet = false;
                    }
                }
            }
            else
            {
                justGetOutOfPlanet = true;

                float deadzone = 0.1f; // seuil à ajuster selon votre besoin
                Vector3 localMove = transform.InverseTransformDirection(new Vector3(moveInput.x, moveInput.y, 0));

                if (Mathf.Abs(localMove.x) > deadzone)
                {
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (localMove.x > 0 ? 1 : -1);
                    transform.localScale = scale;
                }

                float angle;
                Vector3 axis;
                transform.rotation.ToAngleAxis(out angle, out axis);

// Pour obtenir un angle signé, si l'axe Z est négatif, on inverse l'angle
                if (axis.z < 0)
                    angle = -angle;
            }
        }


        if (!isDashing && transform.childCount > 0)
        {
            transform.GetChild(0).rotation = transform.rotation;
        }

        if (playerAnimator.GetBool("isFalling"))
        {
            hasPlayedAnimationLanded = false;
        }

        if ((!isSliding || !isDashing) && collisionGround && fixedLastOnGroundTime && hasPlayedAnimationLanded == false && !waitForSquish)
        {
            StartCoroutine(PlaySquishCoroutine());
        }


        if (isGravityOff == false)
        {
            if (Mathf.Abs(moveInput.x) == 0 && (lastOnGroundTime > 0f))
            {
                playerAnimator.SetBool("isWalking", false);
                playerAnimator.SetBool("isRunning", false);
            }
            else if (Mathf.Abs(moveInput.x) < 0.5 && Mathf.Abs(moveInput.x) != 0 &&
                     playerAnimator.GetBool("isFalling") == false)
            {
                if (!isDashing)
                {
                    playerAnimator.SetBool("isWalking", true);
                    playerAnimator.SetBool("isRunning", false);
                }
            }
            else if (Mathf.Abs(moveInput.x) <= 1 && Mathf.Abs(moveInput.x) != 0 &&
                     playerAnimator.GetBool("isFalling") == false)
            {
                if (!isDashing)
                {
                    playerAnimator.SetBool("isRunning", true);
                    playerAnimator.SetBool("isWalking", false);
                }
            }
            else
            {
                playerAnimator.SetBool("isWalking", false);
                playerAnimator.SetBool("isRunning", false);
            }

            if (contactGround)
            {
                playerAnimator.SetBool("isFalling", false);
            }
        }


        if (RB.linearVelocity.y > 0 && lastOnGroundTime > 0.1)
        {
            if (currentMovingPlatform == null)
            {
                playerAnimator.SetBool("isFalling", false);
            }
        }
        else if ((RB.linearVelocity.y < 0))
        {
            if (isGravityOff)
            {
                if (isSliding || currentMovingPlatform != null)
                {
                    playerAnimator.SetBool("isFalling", false);
                }
                else
                {
                    if (!isDashing)
                    {
                        if (lastOnGroundTime > 0f)
                        {
                            playerAnimator.SetBool("isFalling", false);
                            playerAnimator.SetBool("isJumping", false);

                            if (Mathf.Abs(moveInput.x) == 0)
                            {
                                playerAnimator.SetBool("isWalking", false);
                                playerAnimator.SetBool("isRunning", false);
                            }
                            else if (Mathf.Abs(moveInput.x) < 0.5 && Mathf.Abs(moveInput.x) != (0))
                            {
                                if (!isDashing)
                                {
                                    playerAnimator.SetBool("isWalking", true);
                                    playerAnimator.SetBool("isRunning", false);
                                }
                            }
                            else if ((Mathf.Abs(moveInput.x) <= 1 && Mathf.Abs(moveInput.x) != 0))
                            {
                                if (!isDashing)
                                {
                                    playerAnimator.SetBool("isRunning", true);
                                    playerAnimator.SetBool("isWalking", false);
                                }
                            }
                        }
                        else
                        {
                            if (!isJumping)
                            {
                                playerAnimator.SetBool("isJumping", false);
                                playerAnimator.SetBool("isFalling", true);
                            }

                            if (isJumping)
                            {
                                playerAnimator.SetBool("isJumping", true);
                                playerAnimator.SetBool("isWalking", false);
                                playerAnimator.SetBool("isRunning", false);
                            }
                        }
                    }
                }
            }
            else
            {
                playerAnimator.SetBool("isJumping", false);

                if (isSliding || currentMovingPlatform != null)
                {
                    playerAnimator.SetBool("isFalling", false);
                }
                else
                {
                    if (!isDashing)
                    {
                        if (RB.linearVelocity.y < -0.3 && !contactGround)
                        {
                            playerAnimator.SetBool("isFalling", true);
                        }
                    }
                }
            }
        }

        if (currentRecoverReset > 0)
        {
            RemovePlayerControl(true);
            currentRecoverReset -= Time.deltaTime;
        }
        else
        {
            RemovePlayerControl(false);
        }

        if ((playerControls.actions["Jump"].ReadValue<float>() > 0 ||
             playerControls.actions["Jump 2"].ReadValue<float>() > 0) && !collisionGround &&
            playerAnimator.GetBool("isFalling") == false)
        {
            playerAnimator.SetBool("isJumping", true);
        }
        else
        {
            playerAnimator.SetBool("isJumping", false);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.J) ||
            playerControls.actions["Jump"].WasPressedThisFrame() ||
            playerControls.actions["Jump 2"].WasPressedThisFrame())
        {
            OnJumpInput();
        }

        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.C) || Input.GetKeyUp(KeyCode.J) ||
            playerControls.actions["Jump"].WasReleasedThisFrame() ||
            playerControls.actions["Jump 2"].WasPressedThisFrame())
        {
            OnJumpUpInput();
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K) ||
            playerControls.actions["Dash"].ReadValue<float>() > 0)
        {
            if (!cutGlide)
            {
                Push();
            }
        }
        else
        {
            playerAnimator.SetBool("isPushing", false);
            isGliding = false;
            cutGlide = false;
        }


        if (playerControls.actions["Dpad"].WasPressedThisFrame() &&
            (Mathf.Abs(moveInput.x) == 0 && lastOnGroundTime > 0))
        {
            ChangeCharacter(playerControls.actions["Dpad"].ReadValue<Vector2>());
        }

        #endregion

        targetSpeed = moveInput.x * Data.runMaxSpeed;

        #region COLLISION CHECKS

        if (!isDashing && !isJumping)
        {
            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
            {
                contactGround = true;
                if (lastOnGroundTime < -0.1f)
                {
                }

                lastOnGroundTime = Data.coyoteTime;
            }
            else
            {
                contactGround = false;
            }


            if (((Physics2D.OverlapBox(frontWallCheckPoint.position, wallCheckSize, 0, groundLayer) && isFacingRight)
                 || (Physics2D.OverlapBox(backWallCheckPoint.position, wallCheckSize, 0, groundLayer) &&
                     !isFacingRight)) && !isWallJumping)
            {
                lastOnWallRightTime = Data.coyoteTime;
            }


            if (((Physics2D.OverlapBox(frontWallCheckPoint.position, wallCheckSize, 0, groundLayer) && !isFacingRight)
                 || (Physics2D.OverlapBox(backWallCheckPoint.position, wallCheckSize, 0, groundLayer) &&
                     isFacingRight)) && !isWallJumping)
            {
                lastOnWallLeftTime = Data.coyoteTime;
            }


            lastOnWallTime = Mathf.Max(lastOnWallLeftTime, lastOnWallRightTime);
        }

        #endregion

        #region JUMP CHECKS

        if (isJumping && RB.linearVelocity.y < 0)
        {
            isJumping = false;

            isJumpFalling = true;
        }

        if (RB.linearVelocity.y > 0)
        {
        }
        else if (RB.linearVelocity.y < 0)
        {
        }

        if (isWallJumping && Time.time - wallJumpStartTime > Data.wallJumpTime)
        {
            isWallJumping = false;
        }

        if (lastOnGroundTime > 0 && !isJumping && !isWallJumping)
        {
            isJumpCut = false;

            isJumpFalling = false;
        }

        if (!isDashing)
        {
            if (CanJump() && lastPressedJumpTime > 0)
            {
                isJumping = true;
                isWallJumping = false;
                isJumpCut = false;
                isJumpFalling = false;

                if (isGravityOff == false)
                {
                    Jump();
                }
                else
                {
                    GravityJump();
                }
            }

            else if (CanWallJump() && lastPressedJumpTime > 0)
            {
                isWallJumping = true;
                isJumping = false;
                isJumpCut = false;
                isJumpFalling = false;

                wallJumpStartTime = Time.time;
                lastWallJumpDir = (lastOnWallRightTime > 0) ? -1 : 1;

                WallJump(lastWallJumpDir);
            }
        }

        #endregion

        #region DASH CHECKS

        if (CanDash() && lastPressedDashTime > 0)
        {
            Sleep(Data.dashSleepTime);


            if (moveInput != Vector2.zero)
                lastDashDir = moveInput;
            else
                lastDashDir = isFacingRight ? Vector2.right : Vector2.left;


            isDashing = true;
            isJumping = false;
            isWallJumping = false;
            isJumpCut = false;

            StartCoroutine(nameof(StartDash), lastDashDir);
        }

        #endregion

        #region SLIDE CHECKS

        if (CanSlide() && ((lastOnWallLeftTime > 0 && moveInput.x < 0) || (lastOnWallRightTime > 0 && moveInput.x > 0)))
        {
            isSliding = true;
            playerAnimator.SetBool("isSliding", true);
        }

        else
        {
            isSliding = false;
            playerAnimator.SetBool("isSliding", false);
        }

        #endregion

        #region GRAVITY

        if (isGravityOff == false || isGliding)
        {
            if (currentMovingPlatform == null && !isBumping)
            {
                if (!isDashAttacking)
                {
                    if (isSliding)
                    {
                        SetGravityScale(0);
                    }
                    else if (RB.linearVelocity.y < 0 && moveInput.y < 0)
                    {
                        SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);

                        RB.linearVelocity = new Vector2(RB.linearVelocity.x,
                            Mathf.Max(RB.linearVelocity.y, -Data.maxFastFallSpeed));
                    }
                    else if (isJumpCut)
                    {
                        SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
                        RB.linearVelocity = new Vector2(RB.linearVelocity.x,
                            Mathf.Max(RB.linearVelocity.y, -Data.maxFallSpeed));
                    }
                    else if ((isJumping || isWallJumping || isJumpFalling) &&
                             Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
                    {
                        SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
                    }
                    else if (RB.linearVelocity.y < 0 && lastOnGroundTime <= 0)
                    {
                        SetGravityScale(Data.gravityScale * Data.fallGravityMult);

                        RB.linearVelocity = new Vector2(RB.linearVelocity.x,
                            Mathf.Max(RB.linearVelocity.y, -Data.maxFallSpeed));
                    }
                    else
                    {
                        SetGravityScale(Data.gravityScale);
                    }
                }
                else
                {
                    SetGravityScale(0);
                }
            }
            else
            {
                SetGravityScale(Data.gravityScale);
            }
        }

        #endregion
    }

    private void FixedUpdate()
    {
        if (cannotMove || GameManager.instance.isPaused) return;

        if (isDashing)
        {
            playerAnimator.SetBool("isDashing", true);
            dashPoint.SetActive(true);
        }
        else
        {
            playerAnimator.SetBool("isDashing", false);
            dashPoint.SetActive(false);
        }


        if (areControllsRemoved) return;

        if (isSliding)
            Slide();

        if (currentMovingPlatform != null)
        {
            Vector2 platformDelta = (Vector2)currentMovingPlatform.position - lastPlatformPosition;

            // Si le joueur n'entre aucune commande, il suit la plateforme
            if (moveInput == Vector2.zero)
            {
                RB.position += platformDelta;
            }

            // Mise à jour de la dernière position de la plateforme
            lastPlatformPosition = currentMovingPlatform.position;
        }

        Aim();


        if (isGrappling)
        {
            // Si le grappin suit un objet mobile, mettre à jour le point d'ancrage
            if (grappleTarget != null)
            {
                grapplePoint = grappleTarget.TransformPoint(grappleTargetLocalPoint);
            }

            // Calcul de la direction et de l'extension de la corde
            Vector2 pullDirection = (grapplePoint - (Vector2)transform.position).normalized;
            float currentRopeLength = Vector2.Distance(transform.position, grapplePoint);
            float ropeExtension = (currentRopeLength - initialRopeLength) / 2f;

            if (ropeExtension > 0)
            {
                // Vérification si le joueur est en l'air pour appliquer un multiplicateur
                bool inAir = !Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer);
                float multiplier = inAir ? swingBoostMultiplier : 1f;
                Vector2 force = pullDirection * grapplePullForce * ropeExtension * multiplier;

                // Appliquer la force sur le joueur
                RB.AddForce(force, ForceMode2D.Force);

                // Si l'objet touché possède un Rigidbody2D, appliquer la force opposée
                Rigidbody2D targetRB = grappleTarget != null ? grappleTarget.GetComponent<Rigidbody2D>() : null;
                if (targetRB != null)
                {
                    targetRB.AddForce(-force, ForceMode2D.Force);
                }
            }

            // Empêche le joueur de s'éloigner trop (restriction sur l'extension de la corde)
            Vector2 anchorToPlayer = (Vector2)transform.position - grapplePoint;
            float distance = anchorToPlayer.magnitude;
            if (distance > initialRopeLength)
            {
                float awayVelocity = Vector2.Dot(RB.linearVelocity, anchorToPlayer.normalized);
                if (awayVelocity > 0)
                {
                    RB.linearVelocity -= anchorToPlayer.normalized * awayVelocity;
                }
            }
        }

        if (isGrappling && grappleTarget != null)
        {
            // Recalcule le point d'ancrage en fonction du déplacement de l'objet touché
            grapplePoint = grappleTarget.TransformPoint(grappleTargetLocalPoint);
        }

        if (isGrappling && grappleTarget != null)
        {
            if (grappleTarget.GetComponent<PlayerMovement>() == null)
            {
                Rigidbody2D targetRB = grappleTarget.GetComponent<Rigidbody2D>();
                if (targetRB != null && grappleGravityModified)
                {
                    // Si l'objet est en chute (vélocité en y < 0), on applique la gravity scale du joueur
                    if (targetRB.linearVelocity.y < 0)
                    {
                        if (isGravityOff == false)
                        {
                            targetRB.gravityScale = RB.gravityScale;
                        }
                    }
                    else
                    {
                        if (isGravityOff == false)
                        {
                            targetRB.gravityScale = originalGrappleTargetGravityScale;
                        }
                        // Sinon, on rétablit la gravity scale originale
                    }
                }
            }
        }

        if (isGrappling && grappleTarget != null)
        {
        }

        if (isGrappling && grappleTarget == null)
        {
            CancelGrapple();
        }


        if (currentRecoverTime > 0)
        {
            currentRecoverTime -= Time.deltaTime;
        }

        if (currentCACTime > 0)
        {
            currentCACTime -= Time.deltaTime;
        }

        if (isGroundSliding)
        {
            GroundSlide();
        }
        else if (resetCapsuleBool)
        {
            ResetCapsule();
        }
        else if (!isDashing)
        {
            playerAnimator.SetBool("isSlidingDown", false);
            if (isWallJumping)
                Run(Data.wallJumpRunLerp);
            else
                Run(1);
        }
        else if (isDashAttacking)
        {
            playerAnimator.SetBool("isSlidingDown", false);
            Run(Data.dashEndRunLerp);
        }
        


        if (isGravityOff)
        {
            gravityCollider.enabled = true;
            RunVertical(1);
            gravitySwitch = true;
        }
        else
        {
            gravityCollider.enabled = false;
            if (gravitySwitch)
            {
                Vector3 scale = transform.localScale;
                scale.x = 1;
                transform.localScale = scale;

                gravitySwitch = false;
            }
        }
    }

    private void UIInput()
    {
        if (playerControls.actions["Pause"].WasPressedThisFrame())
        {
            GameManager.instance.TogglePause();
        }
    }

    private float negativeTimer;

    public void FixedLastOnGroundTime()
    {
        if (lastOnGroundTime < 0f)
        {
            // Accumule le temps négatif
            negativeTimer += Time.deltaTime;

            // Dès qu'on dépasse 0.5 s, on active B
            if (negativeTimer >= 0.5f)
                fixedLastOnGroundTime = true;
        }
        else
        {
            // Si A redevient ≥ 0, on réinitialise tout
            negativeTimer = 0f;
            fixedLastOnGroundTime = false;
        }
    }


    public void PlaySquashBounce()
    {
        float force = 1;


        // Crée une nouvelle séquence DOTween
        Sequence seq = DOTween.Sequence();


        // 2) Squash & Stretch : scale X grand, scale Y petit
        seq.Append(transform.GetChild(0).GetChild(0).transform.DOScale(new Vector3(1.7f, 0.5f, 1f), 0.1f)
            .SetEase(Ease.OutQuad));

        // 3) Retour à l’échelle normale avec un peu d’élasticité
        seq.Append(transform.GetChild(0).GetChild(0).transform.DOScale(Vector3.one, 0.4f)
            .SetEase(Ease.OutElastic, 2f));

        hasPlayedAnimationLanded = true;
    }


    private bool gravitySwitch = false;

    #region INPUT CALLBACKS

    public void OnJumpInput()
    {
        lastPressedJumpTime = Data.jumpInputBufferTime;
    }

    public void OnJumpUpInput()
    {
        if (CanJumpCut() || CanWallJumpCut())
            isJumpCut = true;
    }

    public void OnDashInput()
    {
        lastPressedDashTime = Data.dashInputBufferTime;
    }

    #endregion

    #region GENERAL METHODS

    public void SetGravityScale(float scale)
    {
        RB.gravityScale = scale;
    }

    public void SetNoGravity()
    {
        if (flipGravityState == false)
        {
            flipGravityState = true;
            isGravityOff = true;
            SetGravityScale(0);
            RB.freezeRotation = false;
        }
    }

    public void ResetGravity()
    {
        if (flipGravityState)
        {
            Debug.Log("ResetGravity");

            flipGravityState = false;
            isGravityOff = false;
            SetGravityScale(Data.gravityScale);
            gameObject.transform.rotation =
                Quaternion.Euler(gameObject.transform.rotation.x, gameObject.transform.rotation.y, 0);
            RB.freezeRotation = true;
        }
    }

    private void Sleep(float duration)
    {
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }

    #endregion


    #region RUN METHODS

    private void Run(float lerpAmount)
    {
        if (isGliding || cannotMove)
            return;

        targetSpeed = Mathf.Lerp(RB.linearVelocity.x, targetSpeed, lerpAmount);


        #region Calculate AccelRate

        float accelRate;

        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f)
                ? Data.runAccelAmount * Data.accelInAir
                : Data.runDeccelAmount * Data.deccelInAir;

        #endregion

        #region Add Bonus Jump Apex Acceleration

        if ((isJumping || isWallJumping || isJumpFalling) &&
            Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
        {
            accelRate *= Data.jumpHangAccelerationMult;
            targetSpeed *= Data.jumpHangMaxSpeedMult;
        }

        #endregion

        #region Conserve Momentum

        if (Data.doConserveMomentum && Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f &&
            lastOnGroundTime < 0)
        {
            accelRate = 0;
        }

        #endregion

        float speedDif = targetSpeed - RB.linearVelocity.x;


        float movement = speedDif * accelRate;

        if (isGravityOff == false)
        {
            RB.AddForce(movement * Vector2.right, ForceMode2D.Force);
        }
        else
        {
            RB.AddForce(movement * Vector2.right, ForceMode2D.Force);
        }
    }

    private void RunVertical(float lerpAmount)
    {
        // Calcul de la vitesse verticale cible en fonction de l'input
        float targetVerticalSpeed = moveInput.y * Data.runMaxSpeed;

        targetVerticalSpeed = Mathf.Lerp(RB.linearVelocity.y, targetVerticalSpeed, lerpAmount);


        #region Calculate AccelRate

        float accelRate;

        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetVerticalSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetVerticalSpeed) > 0.01f)
                ? Data.runAccelAmount * Data.accelInAir
                : Data.runDeccelAmount * Data.deccelInAir;

        #endregion

        #region Add Bonus Jump Apex Acceleration

        if ((isJumping || isWallJumping || isJumpFalling) &&
            Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
        {
            accelRate *= Data.jumpHangAccelerationMult;
            targetVerticalSpeed *= Data.jumpHangMaxSpeedMult;
        }

        #endregion

        #region Conserve Momentum

        if (Data.doConserveMomentum && Mathf.Abs(RB.linearVelocity.y) > Mathf.Abs(targetVerticalSpeed) &&
            Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetVerticalSpeed) &&
            Mathf.Abs(targetVerticalSpeed) > 0.01f && lastOnGroundTime < 0)
        {
            accelRate = 0;
        }

        #endregion

        float speedDif = targetVerticalSpeed - RB.linearVelocity.y;


        float movement = speedDif * accelRate;

        if (isGravityOff == false)
        {
            RB.AddForce(movement * Vector2.up, ForceMode2D.Force);
        }
        else
        {
            RB.AddForce(movement * Vector2.up, ForceMode2D.Force);
        }
    }

    private void Turn()
    {
        if (!isDashing)
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;

            isFacingRight = !isFacingRight;
        }
    }

    #endregion

    #region JUMP METHODS

    public void Jump()
    {
        if (cannotMove) return;
        
        SoundManager.Instance.PlayRandomSFX(clipsRandomjump, 1.1f, 1.5f);
        
        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;

        #region Perform Jump

        float force = Data.jumpForce;
        if (RB.linearVelocity.y < 0)
            force -= RB.linearVelocity.y;

        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        #endregion
    }
    
    public void Bump(float force, Vector2 Direction)
    {
        if (cannotMove) return;

        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;

        #region Perform Jump
        
        if (RB.linearVelocity.y < 0)
            force -= RB.linearVelocity.y;

        RB.AddForce(Direction * force, ForceMode2D.Impulse);

        
        #endregion
    }


    public void GravityJump()
    {
        // Réinitialise le temps de saut
        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;

        // Calcule la force à appliquer
        float force = gravityJumpForce;

        // Récupère la vitesse actuelle le long de l'axe Y local (transform.up)
        float localUpVelocity = Vector2.Dot(RB.linearVelocity, transform.up);
        if (localUpVelocity < 0)
            force -= localUpVelocity; // Permet d’annuler une vitesse descendante si nécessaire

        // Applique l’impulsion le long de l'axe local "haut"
        RB.AddForce(transform.up * force, ForceMode2D.Impulse);
    }

    private void WallJump(int dir)
    {
        if (canWallJump)
        {
            lastPressedJumpTime = 0;
            lastOnGroundTime = 0;
            lastOnWallRightTime = 0;
            lastOnWallLeftTime = 0;

            SoundManager.Instance.PlayRandomSFX(clipsRandomWalljump, 0.9f , 1.1f);
            
            #region Perform Wall Jump

            Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
            force.x *= dir;

            if (Mathf.Sign(RB.linearVelocity.x) != Mathf.Sign(force.x))
                force.x -= RB.linearVelocity.x;

            if (RB.linearVelocity.y < 0)
                force.y -= RB.linearVelocity.y;

            RB.AddForce(force, ForceMode2D.Impulse);
        }

        #endregion
    }

    private bool IsWallSlippery()
    {
        // Vérifier le front wall
        Collider2D[] frontHits = Physics2D.OverlapBoxAll(frontWallCheckPoint.position, wallCheckSize, 0, groundLayer);
        foreach (var hit in frontHits)
        {
            if (hit.CompareTag("Slippery"))
                return true;
        }

        // Vérifier le back wall
        Collider2D[] backHits = Physics2D.OverlapBoxAll(backWallCheckPoint.position, wallCheckSize, 0, groundLayer);
        foreach (var hit in backHits)
        {
            if (hit.CompareTag("Slippery"))
                return true;
        }

        Collider2D[] GroundHits = Physics2D.OverlapBoxAll(groundCheckPoint.position, wallCheckSize, 0, groundLayer);
        foreach (var hit in GroundHits)
        {
            if (hit.CompareTag("Slippery"))
                return true;
        }

        return false;
    }

    #endregion

    [SerializeField] private float crushingSpeedThreshold = -4f;

    private bool resetCapsuleBool;
    private void GroundSlide()
    {
        GetComponent<CapsuleCollider2D>().size = new Vector2(capsuleSize.x, capsuleSize.y/4);
        GetComponent<CapsuleCollider2D>().offset = new Vector2(capsuleOffset.x, -0.66f);
        playerAnimator.SetBool("isSlidingDown", true);
        // On garde la même vitesse verticale
        float vx = RB.linearVelocity.x;
        // On décélère progressivement vers 0
        vx = Mathf.MoveTowards(vx, 0, groundSlideDeceleration * Time.fixedDeltaTime);
        RB.linearVelocity = new Vector2(vx, RB.linearVelocity.y);
        resetCapsuleBool = true;
    }

    private void ResetCapsule()
    {
        Debug.Log("Reset Capsule");
        GetComponent<CapsuleCollider2D>().size = capsuleSize;
        GetComponent<CapsuleCollider2D>().offset = capsuleOffset;
        resetCapsuleBool = false;
    }

    private void CheckCrushing(Collision2D collision)
    {
        // Vérifier que l'objet en collision appartient bien au layer "Ground"
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        // Optionnel : vérifier que le joueur est bien sur le sol (si nécessaire pour votre logique)
        //if (!Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
        //{
        //	Debug.Log("Pas de détection du sol lors du check d'écrasement.");
        //	return;
        //}

        // Parcourt tous les points de contact de la collision
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Si la normale indique que le contact vient du dessus (normal orientée vers le bas)
            if (contact.normal.y < -0.5f)
            {
                Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (otherRb != null)
                {
                    Debug.Log($"Contact par le haut détecté, vitesse de l'objet : {otherRb.linearVelocity.y}");
                    if (otherRb.linearVelocity.y < crushingSpeedThreshold)
                    {
                        Debug.Log($"Écrasement détecté, déclenchement de Death(): {otherRb.linearVelocity.y}");
                        StartCoroutine(Death());
                        return;
                    }
                }
            }
        }
    }


    private void Aim()
    {
        float angle = Mathf.Atan2(playerControls.actions["Aim"].ReadValue<Vector2>().y,
            playerControls.actions["Aim"].ReadValue<Vector2>().x) * Mathf.Rad2Deg;


        // --- lecture des inputs visée et grappin ---
        if (canShoot)
        {
            if (currentCoolDown > 0)
            {
                currentCoolDown -= Time.deltaTime;
            }

            if (!isFacingRight)
            {
                if (flipAimArm)
                {
                    angle += 180f;
                }

                projectileStartPoint.transform.rotation = Quaternion.Euler(projectileStartPoint.transform.rotation.x,
                    projectileStartPoint.transform.rotation.y, projectileStartPoint.transform.rotation.z + 180f);
            }

            pivot.rotation = Quaternion.Euler(0f, 0f, angle + pivotCorrection);
        }

        //Debug.Log(currentCoolDown);


        if (playerControls.actions["Aim"].ReadValue<Vector2>().normalized == Vector2.zero && canDash)
        {
            armOriginal.SetActive(true);
            armAim.SetActive(false);
            Dash();
        }
        else if (canShoot)
        {
            armOriginal.SetActive(false);
            armAim.SetActive(true);

            if (playerControls.actions["Shoot"].ReadValue<float>() > 0)
            {
                if (currentCoolDown > 0)
                {
                }
                else
                {
                    if (!isFacingRight)
                    {
                        if (flipAimArm)
                        {
                            angle -= 180f;
                        }

                        Rigidbody2D projectileRb = Instantiate(projectilePrefab,
                                projectileStartPoint.transform.position, Quaternion.Euler(0, 0, angle))
                            .GetComponent<Rigidbody2D>();
                        projectileRb.linearVelocity = playerControls.actions["Aim"].ReadValue<Vector2>().normalized *
                                                      Data.projectileSpeed;
                    }
                    else
                    {
                        Rigidbody2D projectileRb = Instantiate(projectilePrefab,
                                projectileStartPoint.transform.position, Quaternion.Euler(0, 0, angle))
                            .GetComponent<Rigidbody2D>();
                        projectileRb.linearVelocity = playerControls.actions["Aim"].ReadValue<Vector2>().normalized *
                                                      Data.projectileSpeed;
                    }


                    currentCoolDown = Data.cooldownProjectile;
                }
            }
        }

        if (!canGrap)
        {
            return;
        }


        // --- lecture des inputs visée et grappin ---
        Vector2 aimDirection = playerControls.actions["Aim"].ReadValue<Vector2>().normalized;
        bool grapInputActive = playerControls.actions["Grap"].ReadValue<float>() != 0 ||
                               playerControls.actions["Grap2"].ReadValue<float>() != 0;

        // --- si stick au repos + bouton Grap enfoncé, on fixe 45° ---
        if (aimDirection == Vector2.zero && grapInputActive)
        {
            float defaultAngle = isFacingRight ? 45f : 135f;
            float rad = defaultAngle * Mathf.Deg2Rad;
            aimDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        // Si le personnage slide sur un mur, on limite l'utilisation du grappin à l'avant uniquement.
        if (isSliding)
        {
            // Détermine le vecteur de face
            Vector2 facingVector = isFacingRight ? Vector2.right : Vector2.left;
            // Calcul du produit scalaire entre l'input de visée et le vecteur de face
            float dot = Vector2.Dot(aimDirection, facingVector);


            // Si le joueur vise vers l'avant (dot >= 0) et que Grap est actif, on bloque l'activation du grappin
            if (dot >= 0 && grapInputActive)
            {
                if (isGrappling)
                {
                    CancelGrapple();
                }

                return; // On sort pour ne pas activer le grappin
            }
        }

        // détruit le marker.

        if (aimDirection != Vector2.zero)
        {
            //marqueur
            RaycastHit2D[] markerHits =
                Physics2D.RaycastAll(grappleOrigin.position, aimDirection, grappleMaxDistance, grappleLayer);
            RaycastHit2D validMarkerHit = new RaycastHit2D();
            float minDistanceMarker = Mathf.Infinity;
            foreach (var hit in markerHits)
            {
                // On ignore le collider du joueur, les objets marqués "CantGrap"
                // et ceux avec lesquels le joueur est en contact.
                if (hit.collider != null && hit.collider != collider2D && !hit.collider.CompareTag("CantGrap"))
                {
                    if (collider2D.IsTouching(hit.collider))
                        continue;
                    if (hit.distance < minDistanceMarker)
                    {
                        minDistanceMarker = hit.distance;
                        validMarkerHit = hit;
                    }
                }
            }

            if (validMarkerHit.collider != null)
            {
                if (activeGrappleAnchorMarker == null)
                {
                    activeGrappleAnchorMarker = Instantiate(grappleAnchorMarkerPrefab, grappleOrigin.position,
                        Quaternion.identity, grappleOrigin);
                }

                activeGrappleAnchorMarker.transform.position = validMarkerHit.point;
            }
            else if (activeGrappleAnchorMarker != null)
            {
                Destroy(activeGrappleAnchorMarker);
                activeGrappleAnchorMarker = null;
            }
        }
        else
        {
            if (activeGrappleAnchorMarker != null)
            {
                Destroy(activeGrappleAnchorMarker);
                activeGrappleAnchorMarker = null;
            }

            // Ne cancel le grappin que s'il n'est pas déjà actif
            if (!isGrappling)
            {
                armOriginal.SetActive(true);
                armAim.SetActive(false);
                return;
            }
            // Sinon, si isGrappling est déjà true, on ne fait rien (le grappin reste actif)
        }

        if (!grapInputActive)
        {
            // Si le bouton n'est pas enfoncé, détruire le marker et annuler le grappin s'il est actif
            if (isGrappling)
            {
                CancelGrapple();
            }
        }
        else // grapInputActive == true
        {
            // Utilisation de RaycastAll pour récupérer tous les hits et filtrer notre propre collider
            // Placement du marqueur
            RaycastHit2D[] markerHits =
                Physics2D.RaycastAll(grappleOrigin.position, aimDirection, grappleMaxDistance, grappleLayer);
            RaycastHit2D validMarkerHit = new RaycastHit2D();
            float minDistanceMarker = Mathf.Infinity;

            foreach (var hit in markerHits)
            {
                if (hit.collider == null || hit.collider == collider2D)
                    continue;

                // Bloquer si l'objet est non grappable ou en collision avec le joueur.
                if (hit.collider.CompareTag("CantGrap") || collider2D.IsTouching(hit.collider))
                    break;

                if (hit.distance < minDistanceMarker)
                {
                    minDistanceMarker = hit.distance;
                    validMarkerHit = hit;
                }
            }

            if (validMarkerHit.collider != null)
            {
                if (activeGrappleAnchorMarker == null)
                    activeGrappleAnchorMarker = Instantiate(grappleAnchorMarkerPrefab, grappleOrigin.position,
                        Quaternion.identity, grappleOrigin);
                activeGrappleAnchorMarker.transform.position = validMarkerHit.point;
            }
            else if (activeGrappleAnchorMarker != null)
            {
                Destroy(activeGrappleAnchorMarker);
                activeGrappleAnchorMarker = null;
            }


            if (!isGrappling)
            {
                // On utilise RaycastAll pour filtrer le collider de l'objet
                RaycastHit2D[] hits = Physics2D.RaycastAll(grappleOrigin.position, aimDirection, grappleMaxDistance,
                    grappleLayer);
                RaycastHit2D validHit = new RaycastHit2D();
                float minDistance = Mathf.Infinity;
                foreach (var hit in hits)
                {
                    if (hit.collider == null || hit.collider == collider2D)
                        continue;

                    // Ignorer les objets non grappables ou en collision avec le joueur.
                    if (hit.collider.CompareTag("CantGrap") || collider2D.IsTouching(hit.collider))
                        break;

                    if (hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                        validHit = hit;
                    }
                }

                if (validHit.collider != null)
                {
                    isGrappling = true;
                    grappleTarget = validHit.collider.transform;
                    grappleTargetLocalPoint = grappleTarget.InverseTransformPoint(validHit.point);
                    grapplePoint = validHit.point;
                    initialRopeLength = Vector2.Distance(grappleOrigin.position, grapplePoint);

                    // 2. Stocker la gravity scale originale si l'objet possède un Rigidbody2D
                    Rigidbody2D targetRB = grappleTarget.GetComponent<Rigidbody2D>();
                    if (targetRB != null)
                    {
                        originalGrappleTargetGravityScale = targetRB.gravityScale;
                        grappleGravityModified = true;
                    }
                }
            }

            if (isGrappling && grappleLine != null)
            {
                grappleLine.positionCount = 2;
                grappleLine.SetPosition(0, grappleOrigin.position);
                grappleLine.SetPosition(1, grapplePoint);
            }
        }
    }

    public void CancelGrapple()
    {
        if (isGrappling)
        {
            isGrappling = false;
            if (grappleLine != null)
            {
                grappleLine.positionCount = 0;
            }

            if (grappleJoint != null)
            {
                Destroy(grappleJoint);
                grappleJoint = null;
            }

            // Réinitialiser la gravity scale de l'objet grappé
            if (grappleTarget != null)
            {
                Rigidbody2D targetRB = grappleTarget.GetComponent<Rigidbody2D>();
                if (targetRB != null && grappleGravityModified)
                {
                    targetRB.gravityScale = originalGrappleTargetGravityScale;
                }
            }

            grappleGravityModified = false;
        }
    }

    private void Dash()
    {
        if (playerControls.actions["Shoot"].ReadValue<float>() > 0)
        {
            playerAnimator.SetBool("isCAC", true);
            if (moveInput != Vector2.zero)
            {
                playerAnimator.SetBool("isCAC", false);
                OnDashInput();
            }
        }
        else
        {
            playerAnimator.SetBool("isCAC", false);
        }
    }

    public void Push()
    {
        // à chaque nouveau dash on autorise de nouveau le glide
        glideDisabled = false;
        isStalling = false;

        playerAnimator.SetBool("isPushing", true);
        if (canGlide)
        {
            Glide();
        }

        if (moveInput != Vector2.zero)
            playerAnimator.SetBool("isPushing", false);
    }


    private Vector2 glideMove;

    private void Glide()
    {
        // 1) recupère l'input
        glideMove.x = moveInput.x;

        if (!yDecayEnabled)
        {
            glideMove.y = moveInput.y;
        }


        // 2) détection de maintien de la même valeur y
        if (moveInput.y >= -0.5f && moveInput.y <= 1f)
        {
            // si l’axe y n’a presque pas bougé
            if (Mathf.Abs(moveInput.y - lastHoldY) < 0.01f)
            {
                yHoldTimer += Time.deltaTime;
                if (yHoldTimer >= yHoldDecayDelay)
                    yDecayEnabled = true;
            }
            else
            {
                yHoldTimer = 0f;
                yDecayEnabled = false;
                lastHoldY = moveInput.y;
            }
        }
        else
        {
            // hors de l’intervalle → reset
            yHoldTimer = 0f;
            yDecayEnabled = false;
            lastHoldY = moveInput.y;
        }

        // 3) si la décroissance est activée, on fait tendre y vers -1
        if (yDecayEnabled)
        {
            Debug.Log("bouger");
            glideMove.y = Mathf.MoveTowards(glideMove.y, -1f, yDecayRate * Time.deltaTime);
        }


        // --- suite de Glide() inchangée ---
        if (glideDisabled) return;
        if (!collisionGround)
        {
            float dt = Time.deltaTime;
            float speed = RB.linearVelocity.magnitude;


            if (glideMove.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(glideMove.y, glideMove.x) * Mathf.Rad2Deg;
                float angle2 = Mathf.Atan2(glideMove.y, -glideMove.x) * Mathf.Rad2Deg;
                if (isFacingRight)
                    transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, angle);
                else
                    transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, angle2);
            }
            else
            {
                transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, -90f);
            }

            if (!isGliding)
            {
                isGliding = true;
                isStalling = false;
                SetGravityScale(Data.gravityScale * glideGravityMult);

                Vector2 init = isFacingRight ? new Vector2(1, -0.5F) : new Vector2(-1, -0.5F);
                glideDir = init.normalized;

                /*
                // départ à 45° vers le bas
                if (speed <= minGlideSpeed*4)
                {
                    Vector2 init = isFacingRight ? new Vector2(1, -1) : new Vector2(-1, -1);
                    glideDir = init.normalized;
                    Debug.Log("test1");
                }

                if (speed > minGlideSpeed*4)
                {
                    Vector2 init = moveInput.normalized;
                    glideDir = init.normalized;
                    Debug.Log("Stall duration exceeded");
                }
                */
            }

            // ——— Gestion du stall (vitesse trop faible OU déjà en stall) ———
            bool justEnteredStall = !isStalling && speed <= minGlideSpeed;
            if (justEnteredStall || isStalling)
            {
                isStalling = true;
                stallTimer += dt;

                if (stallTimer > maxStallDuration)
                {
                    CancelGlide();
                    return;
                }

                float vx = glideMove.x * stallHorizontalSpeed;
                float vy = -stallFallSpeed;
                RB.linearVelocity = new Vector2(vx, vy);
                return;
            }
            else
            {
            }

            isStalling = false;
            SetGravityScale(Data.gravityScale * glideGravityMult);

            if (glideMove.y < 0f)
            {
                speed += -glideMove.y * diveAcceleration * dt;
                stallTimer = 1;
            }
            else
            {
                float decelFactor = Mathf.Max(glideMove.y, 1f);
                speed -= decelFactor * climbDeceleration * dt;
            }

            speed = Mathf.Clamp(speed, minGlideSpeed, maxGlideSpeed);

            Vector2 targetDir = (glideMove.sqrMagnitude > 0.01f) ? glideMove.normalized : glideDir;

            glideDir = Vector2.Lerp(glideDir, targetDir, steeringSpeed * dt).normalized;

            RB.linearVelocity = glideDir * speed;
        }
        else if (isGliding)
        {
            isGliding = false;
            isLowSpeedFalling = false;
            transform.GetChild(0).localRotation = Quaternion.identity;
            SetGravityScale(Data.gravityScale);
        }
    }


    private void CancelGlide()
    {
        cutGlide = true;
        stallTimer = 1;
        glideDisabled = true;
        isStalling = false;
        isGliding = false;
        SetGravityScale(Data.gravityScale);
        isLowSpeedFalling = false;
        transform.GetChild(0).localRotation = Quaternion.identity;
    }

    private bool cutGlide;


    [SerializeField] private RagdollController ragdollController;

    [SerializeField] private GameObject spiritPrefab;

    [SerializeField] private Transform respawnPoint;


    public void Levelup()
    {
        
    }
    
    private IEnumerator Death()
    {
        isDead = true;

        SoundManager.Instance.PlayRandomSFX(clipsRandomDeath, 0.9f, 1.4f);
        gameObject.tag = "Untagged";

        spiritPrefab.transform.position = gameObject.transform.position;

        if (spiritPrefab.transform.parent != null && spiritPrefab.transform.parent.parent != null)
        {
            spiritPrefab.transform.SetParent(spiritPrefab.transform.parent.parent,
                true); // true pour conserver sa position mondiale
        }

        areControllsRemoved = true;

        Vector2 currentVelocity = RB.linearVelocity / 2;
        collider2D.enabled = false;
        RB.simulated = false;
        ragdollController.EnableRagdoll(true);
        ragdollController.SetRagdollVelocity(currentVelocity);
        if (isFacingRight)
        {
            ragdollController.SpinOnZ(torqueAmount: 15f, duration: 1f);
        }
        else
        {
            ragdollController.SpinOnZ(torqueAmount: -15f, duration: 1f);
        }

        GameManager.instance.PlayerDeadCheck(parentName, isDead);

        yield return new WaitForSeconds(2f);


        bool allPlayersDead = true;


        if (GameManager.instance.isPlayer1present && !GameManager.instance.isPlayer1Dead)
        {
            allPlayersDead = false;
        }

        if (GameManager.instance.isPlayer2present && !GameManager.instance.isPlayer2Dead)
        {
            allPlayersDead = false;
        }

        if (GameManager.instance.isPlayer3present && !GameManager.instance.isPlayer3Dead)
        {
            allPlayersDead = false;
        }

        if (GameManager.instance.isPlayer4present && !GameManager.instance.isPlayer4Dead)
        {
            allPlayersDead = false;
        }


        if (allPlayersDead)
        {
            GameManager.instance.ReloadScene();
        }
        else
        {
            spiritPrefab.SetActive(true);
        }
    }

    public void Respawn(Vector3 position)
    {
        gameObject.tag = "Target";


        collider2D.enabled = true;
        RB.simulated = true;
        RB.bodyType = RigidbodyType2D.Dynamic;
        ragdollController.EnableRagdoll(false);

        RefreshModel();

        Transform parent = spiritPrefab.transform.parent;
        if (parent != null && parent.childCount > 0)
        {
            int lastChildIndex = parent.childCount - 1;
            Transform lastChild = parent.GetChild(lastChildIndex - 1);
            spiritPrefab.transform.SetParent(lastChild, true);
        }

        spiritPrefab.SetActive(false);
        gameObject.transform.position = position;
        areControllsRemoved = false;
        isDead = false;
        GameManager.instance.PlayerDeadCheck(parentName, isDead);
    }

    public void RefreshModel()
    {
        if (baseModelPrefab == null)
        {
            return;
        }

        if (transform.childCount > 0)
        {
            Transform currentModel = transform.GetChild(0).GetChild(0).transform;


            GameObject tempDefault = Instantiate(baseModelPrefab);
            tempDefault.SetActive(false);


            CopyTransformRecursive(tempDefault.transform, currentModel);

            Destroy(tempDefault);
        }
    }

    private Coroutine offScreenCoroutine;
    private bool isOffScreen = false;
    private bool isOnMovingPlatform;

    public bool deactivateOnOffScreen;
    private Camera cam;

    private IEnumerator OffScreenTimer()
    {
        if (isDead == false)
        {
            yield return new WaitForSeconds(3f);

            // Vérifier une dernière fois que le joueur est toujours hors de la vue
            Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
            bool isVisible = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                             viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z > 0;
            if (!isVisible)
            {
                if (!deactivateOnOffScreen)
                {
                    StartCoroutine(Death());
                }


                // Si toujours hors de vue, lance la coroutine Death()
            }

            isOffScreen = false;
            offScreenCoroutine = null;
        }
    }


    private void CopyTransformRecursive(Transform source, Transform destination)
    {
        destination.localPosition = source.localPosition;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;

        // Copier les valeurs pour chaque enfant
        for (int i = 0; i < destination.childCount; i++)
        {
            if (i < source.childCount)
            {
                CopyTransformRecursive(source.GetChild(i), destination.GetChild(i));
            }
        }
    }

    public Tween Rotate360Z(float duration, Ease ease = Ease.Linear)
    {
        int rand = UnityEngine.Random.Range(-1, 1);

        return transform.GetChild(0).GetChild(0).transform
            .DOLocalRotate(
                new Vector3(0f, 0f, 360f * rand),
                duration,
                RotateMode.FastBeyond360
            )
            .SetEase(ease)
            .OnComplete(() => transform.GetChild(0).GetChild(0).transform.localEulerAngles = Vector3.zero);
    }

    public void KnockBack(Vector2 direction, bool triggerDamage, float knockBackForce, bool doAnimation, int damage)
    {
        if (doAnimation)
        {
            PlaySquish();
        }

        Vector2 knockBackDir = direction.normalized;

        float upwardMultiplier = 1f;
        Vector2 finalDirection = ((knockBackDir * upwardMultiplier + Vector2.up / 4)).normalized;


        RB.linearVelocity = Vector2.zero;


        RB.AddForce(finalDirection * (knockBackForce), ForceMode2D.Impulse);

        if (triggerDamage)
        {
            Damage(damage);
        }
    }

    public void PlaySquish()
    {
        float duration = 0.5f;
        float squishX = 0.9f;
        float stretchY = 1.1f;


        // Create a new DOTween sequence
        Sequence seq = DOTween.Sequence();

        // At the start of the sequence, set X to true
        seq.AppendCallback(() => playerAnimator.SetBool("isDamageUp", true));

        // Squish and stretch over the first half of the duration
        seq.Append(transform.GetChild(0).GetChild(0).transform.DOScale(
            new Vector3(originalScale.x * squishX, originalScale.y * stretchY, originalScale.z),
            duration / 2f
        ).SetEase(Ease.OutElastic));

        // Return to original scale over the second half of the duration
        seq.Append(transform.GetChild(0).GetChild(0).transform.DOScale(
            originalScale,
            duration / 2f
        ).SetEase(Ease.OutElastic));

        // At the end of the sequence, set X back to false
        seq.AppendCallback(() => playerAnimator.SetBool("isDamageUp", false));

        // Play the sequence
        seq.Play();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Si l'objet entrant appartient à la layer "PlayerXPlayer"
        if (collision.gameObject.layer == LayerMask.NameToLayer("PlayerXPlayer"))
        {
        }

        // Votre logique existante pour la layer "HitPoint"
        if (collision.gameObject.layer == LayerMask.NameToLayer("HitPoint"))
        {
            if (isGravityOff == false)
            {
                Jump();
            }
            else
            {
                GravityJump();
            }
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 6)
        {
            collisionGround = true;
        }


        if (isGrappling)
        {
            /*
            // Vérifier si le GameObject touché est le même que celui du grappin ou est un enfant de ce dernier
            if (collision.gameObject == grappleTarget.gameObject || collision.gameObject.transform.IsChildOf(grappleTarget))
            {
                if (collision.transform.position.y < transform.position.y)
                {
                    CancelGrapple();
                }
            }*/
        }

        if (collision.gameObject.CompareTag("MovingPlatform"))
        {
            currentMovingPlatform = collision.transform;
            lastPlatformPosition = currentMovingPlatform.position;
        }

        CheckCrushing(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isGravityOff)
        {
            CheckCrushing(collision);

            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (collision.gameObject.layer == 6)
                {
                    collisionGround = true;
                }

                float normalAngle = Mathf.Atan2(contact.normal.y, contact.normal.x) * Mathf.Rad2Deg;
                float surfaceAngle = normalAngle - 90f;
                if (surfaceAngle >= -45 && surfaceAngle <= 45)
                {
                    transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, surfaceAngle);
                }

                if (surfaceAngle >= -maxAngleWithFriction && surfaceAngle <= maxAngleWithFriction && moveInput.x == 0 &&
                    lastOnGroundTime > 0)
                {
                    collider2D.sharedMaterial = frictionMaterial;
                }
                else
                {
                    collider2D.sharedMaterial = noFrictionMaterial;
                }
            }
        }
    }


    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!isGravityOff)
        {
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }

        if (collision.gameObject.layer == 6)
        {
            collisionGround = false;
        }

        if (collision.gameObject.CompareTag("MovingPlatform"))
        {
            currentMovingPlatform = null;
        }
    }


    public void Damage(int damage)
    {
        SoundManager.Instance.PlayRandomSFX(clipsRandomSlap, 0.9f, 1.1f);
        CancelGrapple();
        StartCoroutine(DamageCoroutine(damage));
    }

    private bool waitForRecovery;
    private IEnumerator DamageCoroutine(int damage)
    {
        //cannotMove = true;

        if (waitForRecovery)
        {
            yield break;
        }
        
        
        if (HasCurrentlyHealthbonus)
        {
            Instantiate(fairyDeathPrebfab, fairyAnimation.transform.position, Quaternion.Euler(0, 0, 0));
            GameManager.instance.addOrRemovePlayerBonus(parentName, false);
            HasCurrentlyHealthbonus = false;
            areControllsRemoved = false;
            StartCoroutine(WaitForRecovery());
            yield break;
        }
        
        
        currentLife -= damage;
        
        if (currentLife > 0)
        {
            areControllsRemoved = false;
            StartCoroutine(WaitForRecovery());
        }
        else
        {
            StartCoroutine(Death());
        }

        
    }

    IEnumerator WaitForRecovery()
    {
        waitForRecovery = true;
        yield return new WaitForSecondsRealtime(damageRecoveryDuration);
        waitForRecovery = false;
    }

    private void RemovePlayerControl(bool value)
    {
        if (value == true)
        {
            //playerControls.Disable();
        }
        else
        {
            //playerControls.Enable();
        }
    }

    private bool waitForSquish = false;
    private IEnumerator PlaySquishCoroutine()
    {
        waitForSquish = true;
        PlaySquashBounce();
        SoundManager.Instance.PlayRandomSFX(clipsRandomImpact, 0.9f, 1.1f);
        yield return new WaitForSeconds(0.1f);
        waitForSquish = false;
    }

    public void ChangeCharacter(Vector2 direction)
    {
        int indexToActivate = -1;

        if (direction == Vector2.up) // Player0
        {
            if (GameManager.instance.isDarckoxPresent)
            {
                Debug.Log("Darckox");
                return;
            }

            indexToActivate = 0;
        }

        if (direction == Vector2.down) // Player1 
        {
            if (GameManager.instance.isSlowPresent)
            {
                Debug.Log("Slow");
                return;
            }

            indexToActivate = 1;
        }

        if (direction == Vector2.right) // Player2
        {
            if (GameManager.instance.isSulkidePresent)
            {
                Debug.Log("Sulkide");

                return;
            }

            indexToActivate = 2;
        }

        if (direction == Vector2.left) // Player3
        {
            if (GameManager.instance.isSulanaPresent)
            {
                Debug.Log("Sulana");
                return;
            }

            indexToActivate = 3;
        }

        if (indexToActivate < 0 || indexToActivate >= characters.Length)
            return;


        Vector3 lastActivePosition = transform.position;
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].activeSelf)
            {
                lastActivePosition = characters[i].transform.position;
                break;
            }
        }


        for (int i = 0; i < characters.Length; i++)
        {
            bool shouldActivate = (i == indexToActivate);
            characters[i].SetActive(shouldActivate);
            if (shouldActivate)
            {
                characters[i].transform.position = lastActivePosition;
            }
        }
    }


    private void CheckCharacter()
    {
        if (characters[0].activeInHierarchy)
        {
            GameManager.instance.isDarckoxPresent = true;
        }
        else
        {
            //GameManager.instance.isDarckoxPresent = false;
        }

        if (characters[1].activeInHierarchy)
        {
            GameManager.instance.isSlowPresent = true;
        }
        else
        {
            //GameManager.instance.isSlowPresent = false;
        }

        if (characters[2].activeInHierarchy)
        {
            GameManager.instance.isSulkidePresent = true;
        }
        else
        {
            //GameManager.instance.isSulkidePresent = false;
        }

        if (characters[3].activeInHierarchy)
        {
            GameManager.instance.isSulanaPresent = true;
        }
        else
        {
            //GameManager.instance.isSulanaPresent = false;
        }
    }


    
    #region DASH METHODS

    
    [SerializeField] private float compressFactor = 0.6f;    // facteur de compression en X
    [SerializeField] private float stretchFactor  = 1.9f;
    
    private IEnumerator StartDash(Vector2 dir)
    {
        if (canDash)
        {
        
            SoundManager.Instance.PlayRandomSFX(clipsRandomDash, 0.9f, 1.2f);
            
            Transform dashChild = null;
            Vector3 originalScale = Vector3.one;
            if (transform.childCount > 0)
            {
                dashChild = transform.GetChild(0);
                originalScale = dashChild.localScale;
            }

            // Si la direction n'est pas nulle et que le premier enfant existe
            if (dir != Vector2.zero && dashChild != null)
            {
                // Calculer l'angle en degrés
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                dashChild.rotation = Quaternion.Euler(0f, 0f, angle);

                // Si le joystick est dirigé vers la gauche, flipper l'objet en X
                if (dir.x < 0)
                {
                    Vector3 flippedScale = originalScale;
                    flippedScale.x = -Mathf.Abs(originalScale.x);
                    flippedScale.y = -Mathf.Abs(originalScale.y);
                    dashChild.rotation = Quaternion.Euler(0, 0, angle);
                    dashChild.localScale = flippedScale;
                }
                else
                {
                    Vector3 notFlippedScale = originalScale;
                    notFlippedScale.x = Mathf.Abs(originalScale.x);
                    dashChild.localScale = notFlippedScale;
                }
            }
            
            
            // Lancement du tween de stretch
            float dashDuration = Data.dashAttackTime;
            Vector3 startScale  = dashChild.localScale;  // prend en compte le flip
            Vector3 targetScale = new Vector3(
                startScale.x * stretchFactor,   // étirement en X
                startScale.y * compressFactor,  // compression en Y
                startScale.z
            );

            Tween stretchTween = dashChild
                .DOScale(targetScale, dashDuration)
                .SetEase(Ease.Linear);

            // Désactivation de la gravité et début du dash


            lastOnGroundTime = 0;
            lastPressedDashTime = 0;
            float startTime = Time.time;

            dashesLeft--;
            isDashAttacking = true;


            SetGravityScale(0);


            while (Time.time - startTime <= Data.dashAttackTime &&
                   (!playerControls.actions["Jump"].WasPressedThisFrame() &&
                    !playerControls.actions["Jump 2"].WasPressedThisFrame()))
            {
                RB.linearVelocity = dir.normalized * Data.dashSpeed;
                yield return null;
            }

            if (stretchTween.IsActive() && stretchTween.IsPlaying())
                stretchTween.Kill();
            dashChild.localScale = originalScale;

            startTime = Time.time;
            isDashAttacking = false;

            if (!isGravityOff)
            {
                if (RB.linearVelocity.y > 0)
                {
                    RB.linearVelocity = new Vector2(RB.linearVelocity.x, RB.linearVelocity.y * 0.42f);
                }

                SetGravityScale(Data.gravityScale);
            }

            /*
            RB.linearVelocity = Data.dashEndSpeed * dir.normalized;

            while (Time.time - startTime <= Data.dashEndTime)
            {
                yield return null;
            }
            */
            // Réinitialiser la rotation et l'échelle du premier enfant à la fin du dash
            if (dashChild != null)
            {
                dashChild.rotation = Quaternion.Euler(0f, 0f, 0f);
                dashChild.localScale = originalScale;
            }

            transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 0f);

            isDashing = false;
        }
    }


    public IEnumerator RefillDash(int amount)
    {
        dashRefilling = true;
        yield return new WaitForSeconds(Data.dashRefillTime);
        dashRefilling = false;
        dashesLeft = Mathf.Min(Data.dashAmount, dashesLeft + 1);
    }

    #endregion

    #region OTHER MOVEMENT METHODS

    private void Slide()
    {
        if (RB.linearVelocity.y > 0)
        {
            RB.AddForce(-RB.linearVelocity.y * Vector2.up, ForceMode2D.Impulse);
        }

        float speedDif = Data.slideSpeed - RB.linearVelocity.y;
        float movement = speedDif * Data.slideAccel;

        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime),
            Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

        RB.AddForce(movement * Vector2.up);
    }

    #endregion


    #region CHECK METHODS

    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != isFacingRight)
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


    private bool CanJump()
    {
        return lastOnGroundTime > 0 && !isJumping;
    }

    private bool CanWallJump()
    {
        // Empêcher le wall jump si le mur est slippery.
        if (IsWallSlippery())
            return false;

        return (lastPressedJumpTime > 0 && lastOnWallTime > 0 && lastOnGroundTime <= 0 &&
                (!isWallJumping || (lastOnWallRightTime > 0 && lastWallJumpDir == 1) ||
                 (lastOnWallLeftTime > 0 && lastWallJumpDir == -1)));
    }

    private bool CanJumpCut()
    {
        return isJumping && RB.linearVelocity.y > 0;
    }

    private bool CanWallJumpCut()
    {
        return isWallJumping && RB.linearVelocity.y > 0;
    }

    private bool CanDash()
    {
        if (!isDashing && dashesLeft < Data.dashAmount && (lastOnGroundTime > 0 || isJumping) && !dashRefilling )
        {
            StartCoroutine(nameof(RefillDash), 1);
        }

        return dashesLeft > 0;
    }

    public bool CanSlide()
    {
        // Si la surface du mur est slippery, on ne peut pas glisser.
        if (IsWallSlippery())
            return false;

        return (lastOnWallTime > 0 && !isJumping && !isWallJumping && !isDashing && lastOnGroundTime <= 0);
    }

    #endregion


    private void OnDrawGizmos()
    {
        // Permet de dessiner dans la scene view même quand l’objet n’est pas sélectionné
        // Si vous préférez ne dessiner les Gizmos que quand vous sélectionnez l’Objet,
        // alors utilisez plutôt OnDrawGizmosSelected().

        // 1. Vérifier que les points ne sont pas nuls (pour éviter des erreurs si vous 
        //    oubliez d’assigner groundCheckPoint ou frontWallCheckPoint dans l’inspecteur)
        if (groundCheckPoint != null)
        {
            // Couleur pour la zone de détection du sol
            Gizmos.color = Color.yellow;

            // DrawWireCube dessine un cube filaire (wireframe). Comme c’est un OverlapBox,
            // on lui donne la position et la taille. 
            // Note : Pour 2D, on peut imaginer ça comme un rectangle, mais Unity le dessinera en 3D.
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }

        if (frontWallCheckPoint != null)
        {
            // Couleur pour la zone de détection du mur à l’avant
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(frontWallCheckPoint.position, wallCheckSize);
        }

        if (backWallCheckPoint != null)
        {
            // Couleur pour la zone de détection du mur à l’arrière
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(backWallCheckPoint.position, wallCheckSize);
        }
    }
}