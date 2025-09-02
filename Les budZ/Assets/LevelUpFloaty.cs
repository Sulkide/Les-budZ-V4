using UnityEngine;
using DG.Tweening;
#if TMP_PRESENT || TEXTMESHPRO || DOTWEEN_TMP
using TMPro;
#endif

[DisallowMultipleComponent]
public class LevelUpFloaty : MonoBehaviour
{
    [Header("Cible & Suivi")]
    [Tooltip("Objet à suivre (position).")]
    public Transform target;
    [Tooltip("Décalage depuis la cible (monde si non-parenté, local si parenté).")]
    public Vector3 offset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("Parentera le prefab à la cible (suit aussi la rotation/scale). Sinon, suivi de position en monde.")]
    public bool parentToTarget = true;
    [Tooltip("Si non parenté: suivre en LateUpdate la position monde de la cible.")]
    public bool followWhenNotParented = true;

    [Header("Intro (squishy)")]
    [Tooltip("Scale visée après l'intro.")]
    public Vector3 targetScale = Vector3.one;
    [Tooltip("Durée de l'animation d'entrée.")]
    [Min(0.01f)] public float introDuration = 0.35f;
    [Tooltip("Overshoot du OutBack pour l'effet squishy (1.0-2.5 ~).")]
    [Range(0.5f, 3f)] public float introOvershoot = 1.3f;

    [Header("Rotation aléatoire")]
    [Tooltip("Vitesse min de rotation (deg/s).")]
    [Min(0f)] public float spinSpeedMin = 90f;
    [Tooltip("Vitesse max de rotation (deg/s).")]
    [Min(0f)] public float spinSpeedMax = 260f;
    [Tooltip("Choisit aléatoirement sens horaire/anti-horaire.")]
    public bool randomizeDirection = true;

    [Header("Tempo")]
    [Tooltip("Temps d'affichage (après l'intro) avant l'outro).")]
    [Min(0f)] public float holdDuration = 0.8f;
    [Tooltip("Durée de l'outro (réduction scale -> 0).")]
    [Min(0.05f)] public float outroDuration = 0.3f;

    [Header("Texte +Level (optionnel)")]
    [Tooltip("Référence au TMP_Text (trouvé automatiquement si nul).")]
#if TMP_PRESENT || TEXTMESHPRO || DOTWEEN_TMP
    public TMP_Text levelText;
#else
    public UnityEngine.UI.Text levelText; // fallback si pas de TMP
#endif
    [Tooltip("Contenu du texte affiché de l'intro à l'outro.")]
    public string levelTextContent = "+Level";
    [Tooltip("Activer une petite mise à l'échelle du texte à l'intro.")]
    public bool textPopAtIntro = true;
    [Tooltip("Facteur de pop (scale local du texte).")]
    [Range(1f, 2.5f)] public float textPopFactor = 1.15f;
    [Tooltip("Durée du pop du texte (superposée à l'intro).")]
    [Min(0f)] public float textPopDuration = 0.15f;

    [Header("Démarrage")]
    [Tooltip("Lancer automatiquement à l'activation.")]
    public bool playOnEnable = true;

    // --- runtime ---
    private float _spinSpeed;
    private int _spinSign = 1;
    private bool _outroStarted = false;
    private bool _following;
    private Transform _tr;
    private Sequence _lifeSeq;

    void Awake()
    {
        _tr = transform;

        // Texte auto
        TryBindText();

        // Init scale
        _tr.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    void OnDisable()
    {
        KillTweens();
    }

    void OnDestroy()
    {
        KillTweens();
    }

    void LateUpdate()
    {
        // Suivi (si non parenté)
        if (!_outroStarted && !_following && parentToTarget && target != null)
        {
            // verrous init parentage une seule fois
            _following = true;
            _tr.SetParent(target, true);
            _tr.localPosition = offset;
        }
        else if (target != null && !parentToTarget && followWhenNotParented)
        {
            _tr.position = target.position + offset;
        }

        // Rotation constante
        if (_spinSpeed > 0f)
        {
            float delta = _spinSign * _spinSpeed * Time.deltaTime;
            _tr.Rotate(0f, 0f, delta, Space.Self);
        }
    }

    /// <summary>
    /// Lance l'animation complète (intro -> hold -> outro).
    /// </summary>
    public void Play()
    {
        KillTweens();

        // Position initiale alignée
        if (target != null)
        {
            if (parentToTarget)
            {
                _tr.SetParent(target, true);
                _tr.localPosition = offset;
                _following = true;
            }
            else
            {
                _tr.position = target.position + offset;
            }
        }

        // Random vitesse/sens
        _spinSpeed = (spinSpeedMax > spinSpeedMin)
            ? Random.Range(spinSpeedMin, spinSpeedMax)
            : spinSpeedMin;
        _spinSign = randomizeDirection ? (Random.value < 0.5f ? -1 : 1) : 1;

        // Intro squishy
        var intro = _tr.DOScale(targetScale, introDuration)
                       .SetEase(Ease.OutBack, overshoot: introOvershoot);

        // Pop du texte
        if (levelText != null && textPopAtIntro)
        {
            var txtTr = levelText.transform;
            var baseScale = txtTr.localScale;
            txtTr.localScale = baseScale;
            txtTr.DOKill();
            txtTr.DOPunchScale(baseScale * (textPopFactor - 1f), textPopDuration, 1, 0f);
        }

        // Séquence de vie
        _lifeSeq = DOTween.Sequence();
        _lifeSeq.Append(intro);
        if (holdDuration > 0f)
            _lifeSeq.AppendInterval(holdDuration);
        _lifeSeq.OnComplete(StartOutro);
    }

    private void StartOutro()
    {
        if (_outroStarted) return;
        _outroStarted = true;

        // On garde la rotation en Update pendant l'outro.
        var outro = _tr.DOScale(Vector3.zero, outroDuration)
                       .SetEase(Ease.InBack);
        outro.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    private void KillTweens()
    {
        _lifeSeq?.Kill();
        _lifeSeq = null;
        _tr.DOKill();
        if (levelText != null) levelText.transform.DOKill();
    }

    private void TryBindText()
    {
        if (levelText == null)
        {
#if TMP_PRESENT || TEXTMESHPRO || DOTWEEN_TMP
            levelText = GetComponentInChildren<TMP_Text>(true);
#else
            levelText = GetComponentInChildren<UnityEngine.UI.Text>(true);
#endif
        }
        if (levelText != null)
        {
            levelText.text = string.IsNullOrEmpty(levelTextContent) ? "+Level" : levelTextContent;
        }
    }

    // ---------- Méthode pratique de spawn ----------
    /// <summary>
    /// Instancie et configure le prefab LevelUpFloaty.
    /// </summary>
    public static LevelUpFloaty Spawn(LevelUpFloaty prefab, Transform followTarget,
                                      bool parentTo = true, Vector3? worldOffset = null)
    {
        if (prefab == null)
        {
            Debug.LogError("[LevelUpFloaty] Prefab nul.");
            return null;
        }
        var inst = Instantiate(prefab);
        inst.target = followTarget;
        inst.parentToTarget = parentTo;
        inst.offset = worldOffset ?? prefab.offset;
        inst.playOnEnable = true;
        return inst;
    }
}
