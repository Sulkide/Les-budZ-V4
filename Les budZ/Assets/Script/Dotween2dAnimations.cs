using UnityEngine;
using DG.Tweening;

public class DOTweenAutoAnimator2D : MonoBehaviour
{
    [Header("Delay au Démarrage")]
    [Tooltip("Temps (en secondes) avant de lancer les animations au démarrage")]
    public float startDelay = 0f;

    [Header("Délai Aléatoire")]
    [Tooltip("Cochez pour utiliser un délai aléatoire entre min et max au démarrage")]
    public bool useRandomStartDelay = false;
    [Tooltip("Valeur minimale du délai de démarrage aléatoire")]
    public float randomDelayMin = 1f;
    [Tooltip("Valeur maximale du délai de démarrage aléatoire")]
    public float randomDelayMax = 9f;

    [Header("Délais de boucle")]
    [Tooltip("Temps (en secondes) avant de relancer chaque boucle si non aléatoire")]
    public float loopDelay = 0f;

    [Header("Délai de boucle aléatoire")]
    [Tooltip("Cochez pour utiliser un délai de boucle aléatoire entre min et max à chaque itération")]
    public bool useRandomLoopDelay = false;
    [Tooltip("Délai de boucle min (s)")]
    public float randomLoopDelayMin = 0.5f;
    [Tooltip("Délai de boucle max (s)")]
    public float randomLoopDelayMax = 2f;

    [Header("Flottement Vertical")]
    public bool floatUpDown;
    public float floatStrength = 0.5f;
    public float floatSpeed = 1f;

    [Header("Effet de Vent")]
    public bool windEffect;
    public float windAngleStrength = 15f;
    public float windSpeed = 1f;
    public bool windToRight = true;

    [Header("Squishy Bounce")]
    public bool squishyBounce;
    public float squishStrength = 0.2f;
    public float squishSpeed = 2f;

    [Header("Rotation Continue")]
    public bool rotateEffect;
    public float rotateSpeed = 1f;
    public bool rotateClockwise = true;

    [Header("Déplacement Circulaire Continu")]
    public bool circleMove;
    public float circleRadius = 0.5f;
    public float circleSpeed = 1f;
    public bool circleClockwise = true;

    [Header("Vibration de Position")]
    public bool positionVibration;
    public float positionVibrationStrength = 0.1f;
    public float positionVibrationSpeed = 20f;

    [Header("Tremblement de Rotation")]
    public bool rotationShake;
    public float rotationShakeStrength = 5f;
    public float rotationShakeSpeed = 20f;

    [Header("Tremblement de Scale")]
    public bool scaleShake;
    public float scaleShakeStrength = 0.1f;
    public float scaleShakeSpeed = 20f;

    [Header("Effet Cascade")]
    [Tooltip("Active l'effet de cascade")]
    public bool cascade = false;
    [Tooltip("Position Y finale relative à atteindre")]
    public float cascadeEndY = -2f;
    [Tooltip("Force d'étirement (scale Y supplémentaire)")]
    public float cascadeStretchForce = 0.5f;
    [Tooltip("Durée de l'action descendante et fade out (en s)")]
    public float cascadeActionTime = 1f;
    [Tooltip("Temps de pause une fois invisible (en s)")]
    public float cascadePauseTime = 0.5f;
    [Tooltip("Durée de la réapparition (fade in) (en s)")]
    public float cascadeReappearTime = 0.7f;
    
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private Quaternion initialRotation;
    private Transform windContainer;
    private float initialWindZ;
    private SpriteRenderer sr;

    void Awake()
    {
        // Enregistre l'état initial avant tout décalage ou parenting
        initialPosition = transform.localPosition;
        initialScale    = transform.localScale;
        initialRotation = transform.localRotation;
        sr = GetComponent<SpriteRenderer>();

        // Prépare le container pour l'effet vent avant Start
        if (windEffect)
        {
            SetupWindContainer();
            initialWindZ = windContainer.localEulerAngles.z;
            // (re)calculez bien initialPosition si besoin
            initialPosition = transform.localPosition;
        }
    }

    void Start()
    {
        // Délai de démarrage (fixe ou aléatoire)
        if (useRandomStartDelay)
            startDelay = Random.Range(randomDelayMin, randomDelayMax);

        if (startDelay > 0f)
            DOVirtual.DelayedCall(startDelay, InitAnimations);
        else
            InitAnimations();
    }

    private void InitAnimations()
    {
        if (windEffect)        LoopWind();
        if (floatUpDown)       LoopFloat();
        if (squishyBounce)     LoopSquishy();
        if (rotateEffect)      LoopRotate();
        if (circleMove)        LoopCircle();
        if (positionVibration) LoopPositionVibration();
        if (rotationShake)     LoopRotationShake();
        if (scaleShake)        LoopScaleShake();
        if (cascade) LoopCascade();
    }
    
    
    private void LoopCascade()
    {
        // on recrée à chaque itération pour reset les callbacks
        Sequence seq = DOTween.Sequence();

        // 1) descente + étirement + fade out
        seq.Append(transform
            .DOLocalMoveY(initialPosition.y + cascadeEndY, cascadeActionTime)
            .SetEase(Ease.InQuad)
        );
        seq.Join(transform
            .DOScaleY(initialScale.y + cascadeStretchForce, cascadeActionTime)
            .SetEase(Ease.InQuad)
        );
        seq.Join(sr
            .DOFade(0f, cascadeActionTime)
        );

        // 2) pause quand invisible
        seq.AppendInterval(cascadePauseTime);

        // 3) téléport + reset scale + alpha à 0 instantané
        seq.AppendCallback(() =>
        {
            transform.localPosition = initialPosition;
            transform.localScale    = initialScale;
            var c = sr.color; c.a = 0f; sr.color = c;
        });

        // 4) fade in
        seq.Append(sr.DOFade(1f, cascadeReappearTime));

        // 5) relance de la boucle (avec loopDelay si besoin)
        seq.OnComplete(() =>
        {
            float delay = GetLoopDelay();
            if (delay > 0f)
                DOVirtual.DelayedCall(delay, LoopCascade);
            else
                LoopCascade();
        });
    }

    private float GetLoopDelay()
    {
        return useRandomLoopDelay
            ? Random.Range(randomLoopDelayMin, randomLoopDelayMax)
            : loopDelay;
    }

    private void LoopFloat()
    {
        transform
            .DOLocalMoveY(initialPosition.y + floatStrength, floatSpeed)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                transform
                    .DOLocalMoveY(initialPosition.y, floatSpeed)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        float delay = GetLoopDelay();
                        DOVirtual.DelayedCall(delay, LoopFloat);
                    });
            });
    }

    private void LoopWind()
    {
        float dir    = windToRight ?  1f : -1f;
        float target = initialWindZ + dir * windAngleStrength;

        windContainer
            .DOLocalRotate(new Vector3(0f, 0f, target), windSpeed)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                windContainer
                    .DOLocalRotate(new Vector3(0f, 0f, initialWindZ), windSpeed)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        DOVirtual.DelayedCall(GetLoopDelay(), LoopWind);
                    });
            });
    }


    private void LoopSquishy()
    {
        transform
            .DOScale(new Vector3(initialScale.x + squishStrength,
                                 initialScale.y - squishStrength,
                                 initialScale.z), squishSpeed)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                transform
                    .DOScale(initialScale, squishSpeed)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        float delay = GetLoopDelay();
                        DOVirtual.DelayedCall(delay, LoopSquishy);
                    });
            });
    }

    private void LoopRotate()
    {
        float dir = rotateClockwise ? -1f : 1f;
        transform
            .DORotate(new Vector3(0, 0, 360f * dir), rotateSpeed, RotateMode.FastBeyond360)
            .SetRelative()
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                float delay = GetLoopDelay();
                DOVirtual.DelayedCall(delay, LoopRotate);
            });
    }

    private void LoopCircle()
    {
        float angle = 0f;
        float dir = circleClockwise ? -1f : 1f;
        DOTween.To(() => angle, x =>
        {
            angle = x;
            float rad = Mathf.Deg2Rad * angle;
            transform.localPosition = initialPosition + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * circleRadius;
        }, 360f * dir, circleSpeed)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            float delay = GetLoopDelay();
            DOVirtual.DelayedCall(delay, LoopCircle);
        });
    }

    private void LoopPositionVibration()
    {
        transform
            .DOShakePosition(1f, positionVibrationStrength, (int)positionVibrationSpeed, 90, false, true)
            .OnComplete(() =>
            {
                float delay = GetLoopDelay();
                DOVirtual.DelayedCall(delay, LoopPositionVibration);
            });
    }

    private void LoopRotationShake()
    {
        transform
            .DOShakeRotation(1f, new Vector3(0, 0, rotationShakeStrength), (int)rotationShakeSpeed, 90)
            .OnComplete(() =>
            {
                float delay = GetLoopDelay();
                DOVirtual.DelayedCall(delay, LoopRotationShake);
            });
    }

    private void LoopScaleShake()
    {
        transform
            .DOShakeScale(1f, new Vector3(scaleShakeStrength, scaleShakeStrength, 0), (int)scaleShakeSpeed, 90)
            .OnComplete(() =>
            {
                float delay = GetLoopDelay();
                DOVirtual.DelayedCall(delay, LoopScaleShake);
            });
    }

    private void SetupWindContainer()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float spriteHeight = sr.sprite.bounds.size.y * transform.localScale.y;
        Vector3 bottomOffset = new Vector3(0, -spriteHeight / 2f, 0);

        GameObject container = new GameObject("WindContainer");
        container.transform.SetParent(transform.parent);
        container.transform.localRotation = transform.localRotation;
        container.transform.localPosition = initialPosition + bottomOffset;

        transform.SetParent(container.transform);
        transform.localPosition = -bottomOffset;

        windContainer = container.transform;
    }
}
