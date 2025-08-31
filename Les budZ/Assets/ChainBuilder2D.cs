using UnityEngine;

// Assure-toi que le script PlayerMovement est dans le même namespace ou accessible ici
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovement))]
public class ChainBuilder2D : MonoBehaviour
{
    [Header("Base Prefab")]
    public GameObject linkPrefab;

    [Header("Chaîne")]
    [Min(1)] public int linkCount = 5;
    public Direction buildDirection = Direction.Right;
    public float spacing = 0f;

    [Header("Suivi du premier maillon (Link_0)")]
    [Tooltip("Si défini, Link_0 suit cette cible en physique (MovePosition / MoveRotation).")]
    public Transform rootFollowTarget;
    public bool followRotation = false;

    [Header("Override Scale Link_0")]
    public bool overrideFirstLinkScale = false;
    public Vector3 firstLinkLocalScale = Vector3.one;
    public bool adjustFirstColliderToScale = false;

    [Header("Rigidbody Overrides")]
    public bool overrideMass = true;
    public float mass = 1f;
    public bool overrideGravityScale = true;
    public float gravityScale = 1f;

    [Header("Sprite Overrides")]
    public bool randomizeColor = false;
    public Color color = Color.white;
    public Gradient randomColorGradient;
    public bool overrideSorting = true;
    public int baseSortingOrder = 0;
    public int sortingOrderStep = 0;

    [Header("Ancrages / Joint")]
    public Vector2 localAnchorA = new Vector2(0.5f, 0f);
    public Vector2 localAnchorB = new Vector2(-0.5f, 0f);
    public bool autoComputeFromCollider = true;

    [Header("Limites globales des joints (de Link_2 à Link_n)")]
    public bool overrideJointLimits = true;
    public bool useJointLimits = true;
    public float jointMinAngle = -90f;
    public float jointMaxAngle = 90f;
    public bool invertLimitsWhenBuildingLeft = false;

    [Header("Joint spécifique : premier joint inter-maillons (Link_1 ↔ Link_0)")]
    [Tooltip("Override indépendant pour le premier joint (entre Link_1 et Link_0).")]
    public bool overrideFirstChainJointLimits = false;
    [Tooltip("Si vrai, verrouille totalement ce premier joint (min = max = 0).")]
    public bool firstChainJointLockRotation = false;
    [Tooltip("Angle min spécifique pour le premier joint (si non verrouillé).")]
    public float firstChainJointMinAngle = -30f;
    [Tooltip("Angle max spécifique pour le premier joint (si non verrouillé).")]
    public float firstChainJointMaxAngle = 30f;

    [Header("Force sur les derniers maillons")]
    [Tooltip("Multiplicateur appliqué sur l'intensité de la vélocité pour la force inverse.")]
    public float forceMultiplier = 1f;
    [Tooltip("Nombre de maillons finaux (à partir du dernier) sur lesquels appliquer la force. 0 = uniquement le dernier.")]
    [Min(0)] public int forceAffectCount = 0;

    [Header("Debug / Build")]
    public bool rebuildOnStart = true;
    public bool clearPrevious = true;

    public enum Direction { Left, Right }

    private GameObject[] _links;
    private Rigidbody2D _link0Rb;
    private HingeJoint2D _firstChainJoint;

    // Références au parent
    private PlayerMovement _playerMovement;
    private Rigidbody2D _parentRb;

    void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _parentRb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (rebuildOnStart)
            Build();
    }

    void OnEnable()
    {
        if (_links != null)
            foreach (var link in _links)
                if (link != null)
                    link.SetActive(true);
    }

    void OnDisable()
    {
        if (_links != null)
            foreach (var link in _links)
                if (link != null)
                    link.SetActive(false);
    }

    void FixedUpdate()
    {
        // Suivi du premier link
        if (rootFollowTarget != null && _link0Rb != null)
        {
            _link0Rb.MovePosition(rootFollowTarget.position);
            if (followRotation)
                _link0Rb.MoveRotation(rootFollowTarget.rotation.eulerAngles.z);
        }

        // Application de la force inverse sur plusieurs maillons finaux
        if (_playerMovement != null && _playerMovement.moveInput != Vector2.zero && _parentRb != null && _links != null)
        {
            Vector2 vel = _parentRb.linearVelocity;
            if (vel.sqrMagnitude > 0.0001f)
            {
                Vector2 dir = vel.normalized;
                float mag = vel.magnitude;

                // Appliquer de l'indice 0 (dernier) jusqu'à forceAffectCount
                for (int offset = 0; offset <= forceAffectCount; offset++)
                {
                    int idx = _links.Length - 1 - offset;
                    if (idx < 0) break;

                    var link = _links[idx];
                    if (link == null) continue;

                    var rb = link.GetComponent<Rigidbody2D>();
                    rb.gravityScale = 1f;
                    if (rb == null) continue;

                    rb.AddForce(-dir * mag/2 * forceMultiplier);
                }
            }
        }
    }

    [ContextMenu("Build Chain")]
    public void Build()
    {
        if (linkPrefab == null)
        {
            Debug.LogError("[ChainBuilder2D] Link Prefab non assigné.");
            return;
        }

        if (clearPrevious)
            Clear();

        if (linkCount <= 0)
        {
            Debug.LogWarning("[ChainBuilder2D] linkCount <= 0, rien à construire.");
            return;
        }

        _links = new GameObject[linkCount];
        _firstChainJoint = null;

        // Largeur de base
        float width = 1f;
        BoxCollider2D prefabBox = linkPrefab.GetComponent<BoxCollider2D>();
        if (prefabBox != null)
            width = prefabBox.size.x;

        int dirSign = (buildDirection == Direction.Right) ? 1 : -1;
        Vector3 startPos = transform.position;
        Transform commonParent = transform.parent;

        GameObject previous = null;

        for (int i = 0; i < linkCount; i++)
        {
            Vector3 pos = startPos + new Vector3(dirSign * (i * (width + spacing)), 0f, 0f);
            GameObject link = Instantiate(linkPrefab, pos, Quaternion.identity, commonParent);
            link.name = $"Link_{i}";
            _links[i] = link;

            // Override scale du premier maillon
            if (i == 0 && overrideFirstLinkScale)
            {
                link.transform.localScale = firstLinkLocalScale;
                if (adjustFirstColliderToScale)
                {
                    BoxCollider2D box = link.GetComponent<BoxCollider2D>();
                    if (box != null)
                        box.size = new Vector2(
                            box.size.x * Mathf.Abs(firstLinkLocalScale.x),
                            box.size.y * Mathf.Abs(firstLinkLocalScale.y)
                        );
                }
            }

            Rigidbody2D rb = link.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (overrideMass) rb.mass = mass;
                if (overrideGravityScale) rb.gravityScale = gravityScale;
            }
            if (i == 0) _link0Rb = rb;

            SpriteRenderer sr = link.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (randomizeColor)
                    sr.color = (randomColorGradient != null && linkCount > 1)
                        ? randomColorGradient.Evaluate((float)i / (linkCount - 1f))
                        : Random.ColorHSV();
                else
                    sr.color = color;

                if (overrideSorting)
                    sr.sortingOrder = baseSortingOrder + i * sortingOrderStep;
            }

            // Création des joints pour i>0
            if (i > 0)
            {
                HingeJoint2D joint = link.AddComponent<HingeJoint2D>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedBody = previous.GetComponent<Rigidbody2D>();

                Vector2 anchorOnCurrent = ComputeAnchor(link, localAnchorA, autoComputeFromCollider);
                Vector2 anchorOnPrev = ComputeAnchor(previous, localAnchorB, autoComputeFromCollider);
                joint.anchor = anchorOnCurrent;
                joint.connectedAnchor = anchorOnPrev;

                bool isFirstChainJoint = (i == 1);
                ApplyLimits(joint, isFirstChainJoint);

                if (isFirstChainJoint)
                    _firstChainJoint = joint;
            }

            previous = link;
        }
    }

    private Vector2 ComputeAnchor(GameObject go, Vector2 normalizedAnchor, bool auto)
    {
        if (!auto) return normalizedAnchor;
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) return normalizedAnchor;
        return new Vector2(
            box.offset.x + normalizedAnchor.x * box.size.x,
            box.offset.y + normalizedAnchor.y * box.size.y
        );
    }

    private void ApplyLimits(HingeJoint2D joint, bool isFirstChainJoint)
    {
        if (isFirstChainJoint && overrideFirstChainJointLimits)
        {
            JointAngleLimits2D limits = joint.limits;
            if (firstChainJointLockRotation) { limits.min = 0f; limits.max = 0f; }
            else { limits.min = firstChainJointMinAngle; limits.max = firstChainJointMaxAngle; }
            joint.limits = limits;
            joint.useLimits = true;
            return;
        }
        if (!overrideJointLimits || !useJointLimits) return;
        float min = jointMinAngle;
        float max = jointMaxAngle;
        if (buildDirection == Direction.Left && invertLimitsWhenBuildingLeft)
        {
            var oldMin = min;
            min = -max; max = -oldMin;
        }
        var gl = joint.limits;
        gl.min = min; gl.max = max;
        joint.limits = gl;
        joint.useLimits = true;
    }

    [ContextMenu("Clear Chain")]
    public void Clear()
    {
        if (_links != null)
            foreach (var lk in _links)
                if (lk != null)
#if UNITY_EDITOR
                    if (Application.isEditor) DestroyImmediate(lk); else Destroy(lk);
#else
                    Destroy(lk);
#endif
        _links = null;
        _link0Rb = null;
        _firstChainJoint = null;
    }

    public void RebuildRuntime(int newCount, bool toRight)
    {
        linkCount = newCount;
        buildDirection = toRight ? Direction.Right : Direction.Left;
        Build();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (overrideFirstChainJointLimits && linkCount < 2)
            Debug.LogWarning("[ChainBuilder2D] overrideFirstChainJointLimits actif mais linkCount < 2 (pas de joint).");
    }
#endif
}
