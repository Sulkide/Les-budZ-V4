using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpiritController : MonoBehaviour
{
    [Header("Paramètres de déplacement")]
    public float acceleration = 1000f; // Accélération appliquée en fonction de l'input
    public float maxSpeed = 20f;      // Vitesse maximale
    public float drag = 5f;          // Coefficient de décélération pour l'effet de patinage

    [Header("Référence au script PlayerMovement")]
    public PlayerMovement playerMovement;  // À assigner dans l'inspecteur

    [Header("Paramètres d'affichage")]
    [Range(0f, 0.5f)]
    public float safeMargin = 0.01f; // Marge de sécurité (en coordonnées viewport) pour rester bien à l'intérieur de l'écran

    private PlayerInput playerControls;
    private Vector2 moveInput;
    private Vector2 velocity;       // Vitesse actuelle calculée manuellement
    private Rigidbody2D rb;
    private Camera mainCam;

    private void Awake()
    {
        // Récupération des composants nécessaires
        playerControls = GetComponentInParent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
    }

    public void Start()
    {
        
    }
    

    private void Update()
    {
        // Lecture de l'input (attention, cela se fait en Update pour être réactif)
        moveInput = playerControls.actions["Move"].ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        // Applique l'accélération en fonction de l'input
        velocity += moveInput * acceleration * Time.fixedDeltaTime;

        // Limite la vitesse à maxSpeed
        if (velocity.magnitude > maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
        }

        // Applique une décélération pour obtenir l'effet de patinage
        velocity = Vector2.Lerp(velocity, Vector2.zero, drag * Time.fixedDeltaTime);

        // Déplace l'objet en mode kinematic à l'aide de MovePosition
        Vector2 newPos = rb.position + velocity * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }

    private void LateUpdate()
    {
        // Vérifie si l'objet dépasse la zone visible de la caméra et le repositionne avec une marge de sécurité
        Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);
        bool reposition = false;

        // Contrôle sur l'axe horizontal
        if (viewportPos.x < safeMargin)
        {
            viewportPos.x = safeMargin;
            reposition = true;
        }
        else if (viewportPos.x > 1f - safeMargin)
        {
            viewportPos.x = 1f - safeMargin;
            reposition = true;
        }

        // Contrôle sur l'axe vertical
        if (viewportPos.y < safeMargin)
        {
            viewportPos.y = safeMargin;
            reposition = true;
        }
        else if (viewportPos.y > 1f - safeMargin)
        {
            viewportPos.y = 1f - safeMargin;
            reposition = true;
        }

        if (reposition)
        {
            Vector3 newWorldPos = mainCam.ViewportToWorldPoint(viewportPos);
            newWorldPos.z = transform.position.z; // Conserver la profondeur
            transform.position = newWorldPos;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (playerMovement != null && other.CompareTag("Target"))
        {
            playerMovement.Respawn(other.gameObject.transform.position);
        }
        else
        {
            Debug.LogWarning("Aucune référence à PlayerMovement n'est assignée dans l'inspecteur !");
        }
    }
}
