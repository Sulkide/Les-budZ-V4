using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GrassBlade : MonoBehaviour
{
    // — Vent global —
    public static float windDirection = 1f;
    public static float windIntensity = 0.5f;

    // — Oscillation au vent —
    public float maxWindLeanAngle = 15f;
    public float swayAmplitude    = 5f;
    public float swaySpeed        = 2f;

    // — Pliage sous le joueur —
    public bool  useSideBend        = false;
    public float flattenScaleY      = 0.2f;
    public float recoverSpeed       = 1f;
    public float maxPlayerLeanAngle = 30f;
    public float bendSpeed          = 100f;

    [Tooltip("Multiplicateur appliqué à la zone de détection APRÈS un contact")]
    public float detectionZoneMultiplier = 2f;

    private bool   isSteppedOn = false;
    private int    playerSide  = 0;
    private float  baseScaleY;
    private float  swayPhase;

    // Le BoxCollider et ses valeurs d'origine
    private BoxCollider2D boxCollider;
    private Vector2       baseColliderSize;
    private Vector2       baseColliderOffset;

    void Awake()
    {
        boxCollider         = GetComponent<BoxCollider2D>();
        // On stocke la taille et l'offset du collider définis sur le prefab
        baseColliderSize    = boxCollider.size;
        baseColliderOffset  = boxCollider.offset;
        if (BladeCullingManager.Instance != null)
            BladeCullingManager.Instance.RegisterBlade(gameObject);
    }

    void Start()
    {
        baseScaleY = transform.localScale.y;
        swayPhase  = Random.Range(0f, 2f * Mathf.PI);
        
    }

    void OnEnable()
    {
        // Dès qu’un brin devient actif, on l’enregistre
        if (BladeCullingManager.Instance != null)
            BladeCullingManager.Instance.RegisterBlade(gameObject);
    }

    void OnDisable()
    {
        // Dès qu’un brin est désactivé/détruit, on le désenregistre
        if (BladeCullingManager.Instance != null)
            BladeCullingManager.Instance.UnregisterBlade(gameObject);
    }
    
    void OnDestroy()
    {
        // on se désinscrit seulement quand l'objet est vraiment détruit
        if (BladeCullingManager.Instance != null)
            BladeCullingManager.Instance.UnregisterBlade(gameObject);
    }
    
    void Update()
    {
        // 1) On calcule l'angle cible (vent ou pli latéral / aplatissement)
        float targetAngle;
        if (!isSteppedOn)
        {
            float windLean = windDirection * windIntensity * maxWindLeanAngle;
            float windSway = Mathf.Sin(Time.time * swaySpeed + swayPhase)
                            * swayAmplitude * windIntensity;
            targetAngle = windLean + windSway;
        }
        else
        {
            targetAngle = useSideBend
                ? playerSide * maxPlayerLeanAngle
                : 0f;
        }

        // 2) Application progressive de la rotation
        float currentAngle = transform.rotation.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
        float newAngle = Mathf.MoveTowards(currentAngle, targetAngle, bendSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);

        // 3) Gestion de l'échelle pour l'aplatissement
        if (!isSteppedOn && transform.localScale.y < baseScaleY)
        {
            float newY = Mathf.Min(baseScaleY, transform.localScale.y + recoverSpeed * Time.deltaTime);
            transform.localScale = new Vector3(transform.localScale.x, newY, transform.localScale.z);
        }
        else if (isSteppedOn && !useSideBend)
        {
            float newY = Mathf.Max(flattenScaleY, transform.localScale.y - recoverSpeed * Time.deltaTime);
            transform.localScale = new Vector3(transform.localScale.x, newY, transform.localScale.z);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Target") && !isSteppedOn)
        {
            isSteppedOn = true;

            // CALCUL D'UNE TAILLE CARRÉE pour le collider : on prend la plus grande
            // dimension d'origine, puis on l'agrandit.
            float maxSide = Mathf.Max(baseColliderSize.x, baseColliderSize.y);
            float side    = maxSide * detectionZoneMultiplier;
            boxCollider.size   = new Vector2(side, side);

            // On garde l'offset d'origine (centrage)
            boxCollider.offset = baseColliderOffset;

            // Déterminer le côté du joueur
            float dx    = other.transform.position.x - transform.position.x;
            playerSide  = (dx >= 0f) ? 1 : -1;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Target") && isSteppedOn)
        {
            isSteppedOn = false;
            // On rétablit la taille et l'offset initiaux du collider
            boxCollider.size   = baseColliderSize;
            boxCollider.offset = baseColliderOffset;
            playerSide         = 0;
        }
    }

    // Visualisation du collider dans l'éditeur
    void OnDrawGizmosSelected()
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        Vector3 worldSize = new Vector3(
            boxCollider.size.x * transform.localScale.x,
            boxCollider.size.y * transform.localScale.y,
            0f
        );
        Vector3 worldPos = transform.position + (Vector3)boxCollider.offset;

        Gizmos.color = isSteppedOn ? Color.yellow : Color.green;
        Gizmos.DrawWireCube(worldPos, worldSize);
    }
}
