using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFlip3D2D : MonoBehaviour
{
    [Header("Durée & easing")]
    public float flipDuration = 1.0f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("État courant")]
    public bool is3D = true;

    [Header("Capture auto au Start (si is3D = true)")]
    [ReadOnly] public Vector3 posOffset3D;   // on conserve pour rot/FOV de référence
    [ReadOnly] public Vector3 rotOffset3D;
    [ReadOnly] public float fov3D = 60f;

    [Header("Cible 2D (Orthographic)")]
    public Vector3 rotOffset2D = new Vector3(0f, 0f, 0f);
    public float orthoSize = 7f;

    [Header("Préservation d'échelle")]
    public bool preserveScale = true;
    [Range(0.1f, 5f)] public float minFOV = 1.0f;

    [Header("Rail & Suivi")]
    public Transform target;                 // par défaut: GameManager.instance.players[0].transform
    public float followSpeed = 6f;
    public bool disableRailChildrenAfterBake = true;

    [Header("3D Target Distance")]
    [Tooltip("Distance finale (magnitude, positive) de la caméra en mode 3D. Le Z final sera -target3DDistance.")]
    public float target3DDistance = 20f;

    private readonly List<Vector3> _railLocalPoints = new List<Vector3>();

    Camera _cam;
    bool _isFlipping;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        minFOV = Mathf.Max(0.1f, minFOV);
        target3DDistance = Mathf.Max(0.01f, target3DDistance);
    }

    void Start()
    {
        if (is3D)
        {
            _cam.orthographic = false;
            posOffset3D = transform.localPosition;     // on garde comme info, mais on ne réutilise plus son Z pour la cible
            rotOffset3D = transform.localEulerAngles;
            if (_cam.fieldOfView > 0f) fov3D = _cam.fieldOfView;
        }
        else
        {
            _cam.orthographic = true;
            _cam.orthographicSize = orthoSize;
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
            transform.localEulerAngles = rotOffset2D;
        }

        if (target == null && GameManager.instance != null &&
            GameManager.instance.players != null && GameManager.instance.players.Length > 0 &&
            GameManager.instance.players[0] != null)
        {
            target = GameManager.instance.players[0].transform;
        }

        BakeRailFromChildren();
    }

    void LateUpdate()
    {
        if (_isFlipping) return;

        if (_railLocalPoints.Count >= 2 && target != null)
        {
            Vector3 targetLocal = WorldToParentLocal(target.position);
            Vector3 projectedLocal = ProjectPointOnRailLocal(targetLocal);

            float desiredZ = _cam.orthographic ? 0f : transform.localPosition.z;

            Vector3 desiredLocal = new Vector3(projectedLocal.x, projectedLocal.y, desiredZ);
            transform.localPosition = Vector3.Lerp(transform.localPosition, desiredLocal, followSpeed * Time.deltaTime);
        }
    }

    // -------------------- API PUBLIQUE --------------------

    public void Flip3Dto2D()
    {
        if (!is3D || _isFlipping) return;
        StartCoroutine(CoFlip3Dto2D());
    }

    public void Flip2Dto3D()
    {
        if (is3D || _isFlipping) return;
        StartCoroutine(CoFlip2Dto3D());
    }

    // -------------------- TRANSITIONS --------------------

    IEnumerator CoFlip3Dto2D()
    {
        _isFlipping = true;

        // ÉTAT COURANT (pas les valeurs inspector)
        Vector3 startPosLocal = transform.localPosition;
        Vector3 startRotLocal = transform.localEulerAngles;
        float   startFOV      = _cam.fieldOfView;

        Vector3 endRotLocal   = rotOffset2D;
        float   endFOV        = minFOV;

        float startLmag = Mathf.Max(0.01f, Mathf.Abs(startPosLocal.z));
        float H3D = startLmag * Mathf.Tan(Mathf.Deg2Rad * startFOV * 0.5f);
        float H2D = orthoSize;

        float t = 0f;
        while (t < flipDuration)
        {
            float u = ease.Evaluate(t / flipDuration);

            // Suivi rail XY
            Vector3 followXY = transform.localPosition;
            if (_railLocalPoints.Count >= 2 && target != null)
            {
                Vector3 trgL = WorldToParentLocal(target.position);
                Vector3 prjL = ProjectPointOnRailLocal(trgL);
                Vector3 curr = transform.localPosition;
                followXY = Vector3.Lerp(curr, new Vector3(prjL.x, prjL.y, curr.z), followSpeed * Time.deltaTime);
            }

            Vector3 rotNow = SlerpEuler(startRotLocal, endRotLocal, u);
            float fovNow   = Mathf.Lerp(startFOV, endFOV, u);

            float Hnow = preserveScale ? Mathf.Lerp(H3D, H2D, u) : H3D;
            float LmagNow;
            if (preserveScale)
            {
                float tanHalf = Mathf.Tan(Mathf.Deg2Rad * Mathf.Max(0.001f, fovNow) * 0.5f);
                LmagNow = Mathf.Max(0.01f, Hnow / Mathf.Max(0.0001f, tanHalf));
            }
            else
            {
                float zNow = Mathf.Lerp(startPosLocal.z, 0f, u);
                LmagNow = Mathf.Abs(zNow);
            }

            transform.localEulerAngles = rotNow;
            transform.localPosition = new Vector3(followXY.x, followXY.y, -LmagNow);
            _cam.orthographic = false;
            _cam.fieldOfView = fovNow;

            t += Time.deltaTime;
            yield return null;
        }

        // ORTHO : Z = 0, XY reste celui du suivi
        transform.localEulerAngles = endRotLocal;
        transform.localPosition    = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);
        _cam.orthographic          = true;
        _cam.orthographicSize      = orthoSize;

        is3D = false;
        GameManager.instance?.ChangeDimensionState(is3D);
        _isFlipping = false;
    }

    IEnumerator CoFlip2Dto3D()
    {
        _isFlipping = true;

        // Part d'Ortho (Z=0), on passe en perspective proprement
        Vector3 startRotLocal = transform.localEulerAngles;

        // On définit la **cible** 3D claire :
        float endFOV   = (fov3D > 0f) ? fov3D : 60f;
        float endLmag  = target3DDistance;                 // <= ICI : Z final = -target3DDistance
        Vector3 endRot = rotOffset3D;

        // Basculer en Perspective sans pop :
        float startLmag = HeightToDistance(orthoSize, minFOV);
        _cam.orthographic      = false;
        _cam.fieldOfView       = minFOV;
        transform.localEulerAngles = startRotLocal;
        transform.localPosition    = new Vector3(transform.localPosition.x, transform.localPosition.y, -startLmag);

        // Hauteurs de frustum pour préservation d’échelle pendant le flip
        float H2D = orthoSize;
        float H3D = endLmag * Mathf.Tan(Mathf.Deg2Rad * endFOV * 0.5f);

        float t = 0f;
        while (t < flipDuration)
        {
            float u = ease.Evaluate(t / flipDuration);

            // Suivi rail XY pendant le flip
            Vector3 followXY = transform.localPosition;
            if (_railLocalPoints.Count >= 2 && target != null)
            {
                Vector3 trgL = WorldToParentLocal(target.position);
                Vector3 prjL = ProjectPointOnRailLocal(trgL);
                Vector3 curr = transform.localPosition;
                followXY = Vector3.Lerp(curr, new Vector3(prjL.x, prjL.y, curr.z), followSpeed * Time.deltaTime);
            }

            Vector3 rotNow = SlerpEuler(startRotLocal, endRot, u);
            float fovNow   = Mathf.Lerp(minFOV, endFOV, u);

            float Hnow = preserveScale ? Mathf.Lerp(H2D, H3D, u) : H3D;
            float tanHalf = Mathf.Tan(Mathf.Deg2Rad * Mathf.Max(0.001f, fovNow) * 0.5f);
            float LmagNow = preserveScale
                ? Mathf.Max(0.01f, Hnow / Mathf.Max(0.0001f, tanHalf))
                : Mathf.Lerp(startLmag, endLmag, u);

            transform.localEulerAngles = rotNow;
            transform.localPosition    = new Vector3(followXY.x, followXY.y, -LmagNow);
            _cam.fieldOfView           = fovNow;

            t += Time.deltaTime;
            yield return null;
        }

        // Fin 3D : objectif net
        transform.localEulerAngles = endRot;
        transform.localPosition    = new Vector3(transform.localPosition.x, transform.localPosition.y, -endLmag);
        _cam.orthographic          = false;
        _cam.fieldOfView           = endFOV;

        is3D = true;
        GameManager.instance?.ChangeDimensionState(is3D);
        _isFlipping = false;
    }

    // -------------------- RAIL --------------------

    void BakeRailFromChildren()
    {
        _railLocalPoints.Clear();
        Transform parent = transform.parent;

        List<Transform> children = new List<Transform>();
        foreach (Transform c in transform) children.Add(c);

        if (children.Count < 2)
        {
            Debug.LogWarning("[CameraFlip3D2D] Il faut au moins 2 enfants pour définir le rail.");
        }

        foreach (var c in children)
        {
            Vector3 world = c.position;
            Vector3 localInParent = (parent != null) ? parent.InverseTransformPoint(world) : world;
            _railLocalPoints.Add(localInParent);

            if (disableRailChildrenAfterBake)
                c.gameObject.SetActive(false);
        }
    }

    Vector3 ProjectPointOnRailLocal(Vector3 pLocal)
    {
        Vector3 best = _railLocalPoints[0];
        float bestSqr = float.MaxValue;

        for (int i = 0; i < _railLocalPoints.Count - 1; i++)
        {
            Vector3 A = _railLocalPoints[i];
            Vector3 B = _railLocalPoints[i + 1];

            Vector2 a2 = new Vector2(A.x, A.y);
            Vector2 b2 = new Vector2(B.x, B.y);
            Vector2 p2 = new Vector2(pLocal.x, pLocal.y);

            Vector2 AB = b2 - a2;
            float len2 = AB.sqrMagnitude;
            if (len2 < 1e-5f) continue;

            float t = Mathf.Clamp01(Vector2.Dot(p2 - a2, AB) / len2);
            Vector2 proj = a2 + t * AB;

            float d2 = (p2 - proj).sqrMagnitude;
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                float z = Mathf.Lerp(A.z, B.z, t);
                best = new Vector3(proj.x, proj.y, z);
            }
        }
        return best;
    }

    Vector3 WorldToParentLocal(Vector3 world)
    {
        Transform parent = transform.parent;
        return (parent != null) ? parent.InverseTransformPoint(world) : world;
    }

    // -------------------- UTIL --------------------

    static Vector3 SlerpEuler(Vector3 aDeg, Vector3 bDeg, float t)
    {
        Quaternion qa = Quaternion.Euler(aDeg);
        Quaternion qb = Quaternion.Euler(bDeg);
        return Quaternion.Slerp(qa, qb, Mathf.Clamp01(t)).eulerAngles;
    }

    static float HeightToDistance(float halfHeight, float fovDeg)
    {
        float tanHalf = Mathf.Tan(Mathf.Deg2Rad * Mathf.Max(0.001f, fovDeg) * 0.5f);
        return Mathf.Max(0.01f, halfHeight / Mathf.Max(0.0001f, tanHalf));
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute {}
}
