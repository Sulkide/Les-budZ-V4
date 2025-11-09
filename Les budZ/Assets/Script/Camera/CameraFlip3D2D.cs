using System.Collections;
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
    [ReadOnly] public Vector3 posOffset3D;
    [ReadOnly] public Vector3 rotOffset3D;
    [ReadOnly] public float fov3D;

    [Header("Cible 2D (Orthographic)")]
    [Tooltip("Position locale cible (X,Y). Z sera toujours 0 en mode Ortho.")]
    public Vector2 posOffset2D = new Vector2(0f, 10f);
    [Tooltip("Rotation locale cible (en degrés).")]
    public Vector3 rotOffset2D = new Vector3(0f, 0f, 0f);
    [Tooltip("Orthographic Size (demi-hauteur du frustum ortho).")]
    public float orthoSize = 7f;

    [Header("Préservation d'échelle")]
    public bool preserveScale = true;
    [Range(0.1f, 5f)] public float minFOV = 1.0f;

    Camera _cam;
    bool _isFlipping;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        minFOV = Mathf.Max(0.1f, minFOV);
    }

    void Start()
    {
        if (is3D)
        {
            _cam.orthographic = false;
            posOffset3D = transform.localPosition;
            rotOffset3D = transform.localEulerAngles;
            fov3D = _cam.fieldOfView;
        }
        else
        {
            _cam.orthographic = true;
            _cam.orthographicSize = orthoSize;
            // Z = 0 en mode Ortho
            transform.localPosition = new Vector3(posOffset2D.x, posOffset2D.y, 0f);
            transform.localEulerAngles = rotOffset2D;
        }
    }

    // -------------------- API PUBLIQUE --------------------

    public void Flip3Dto2D()
    {
        if (!is3D || _isFlipping) return;
        is3D = false;
        GameManager.instance.ChangeDimensionState(is3D);
        StartCoroutine(CoFlip3Dto2D());
    }

    public void Flip2Dto3D()
    {
        if (is3D || _isFlipping) return;
        is3D = true;
        GameManager.instance.ChangeDimensionState(is3D);
        StartCoroutine(CoFlip2Dto3D());
    }

    // -------------------- TRANSITIONS --------------------

    IEnumerator CoFlip3Dto2D()
    {
        _isFlipping = true;

        Vector3 startPos = posOffset3D;
        Vector3 startRot = rotOffset3D;
        float startFOV = fov3D;

        Vector3 endRot = rotOffset2D;
        float endFOV = minFOV;

        float startLmag = Mathf.Max(0.01f, Mathf.Abs(startPos.z));
        float H3D = startLmag * Mathf.Tan(Mathf.Deg2Rad * startFOV * 0.5f);
        float H2D = orthoSize;

        float t = 0f;
        while (t < flipDuration)
        {
            float u = ease.Evaluate(t / flipDuration);

            Vector3 rotNow = SlerpEuler(startRot, endRot, u);
            float fovNow = Mathf.Lerp(startFOV, endFOV, u);

            float Hnow = preserveScale ? Mathf.Lerp(H3D, H2D, u) : H3D;
            float LmagNow;
            if (preserveScale)
            {
                float tanHalf = Mathf.Tan(Mathf.Deg2Rad * Mathf.Max(0.001f, fovNow) * 0.5f);
                LmagNow = Mathf.Max(0.01f, Hnow / Mathf.Max(0.0001f, tanHalf));
            }
            else
            {
                // On garde la direction –Z, mais ce chemin est rarement utile si preserveScale est true
                float zNow = Mathf.Lerp(startPos.z, 0f, u);
                LmagNow = Mathf.Abs(zNow);
            }

            Vector3 posNow = new Vector3(
                Mathf.Lerp(startPos.x, posOffset2D.x, u),
                Mathf.Lerp(startPos.y, posOffset2D.y, u),
                -LmagNow // recule vers –Z pendant la compression
            );

            transform.localEulerAngles = rotNow;
            transform.localPosition = posNow;
            _cam.orthographic = false;
            _cam.fieldOfView = fovNow;

            t += Time.deltaTime;
            yield return null;
        }

        // Switch Ortho : on fige Z = 0
        transform.localEulerAngles = endRot;
        transform.localPosition = new Vector3(posOffset2D.x, posOffset2D.y, 0f);
        _cam.orthographic = true;
        _cam.orthographicSize = orthoSize;

        is3D = false;
        _isFlipping = false;
    }

    IEnumerator CoFlip2Dto3D()
    {
        _isFlipping = true;

        // Départ Ortho (Z=0)
        Vector3 startPos = new Vector3(posOffset2D.x, posOffset2D.y, 0f);
        Vector3 startRot = rotOffset2D;

        // Pour éviter le pop, on passe en perspective avec FOV=minFOV et Z = -L(orthoSize, minFOV)
        float startLmag = HeightToDistance(orthoSize, minFOV);
        _cam.orthographic = false;
        _cam.fieldOfView = minFOV;
        transform.localEulerAngles = startRot;
        transform.localPosition = new Vector3(startPos.x, startPos.y, -startLmag);

        // Arrivée 3D
        Vector3 endPos = posOffset3D;
        Vector3 endRot = rotOffset3D;
        float endFOV = fov3D;

        float endLmag = Mathf.Max(0.01f, Mathf.Abs(endPos.z));
        float H2D = orthoSize;
        float H3D = endLmag * Mathf.Tan(Mathf.Deg2Rad * endFOV * 0.5f);

        float t = 0f;
        while (t < flipDuration)
        {
            float u = ease.Evaluate(t / flipDuration);

            Vector3 rotNow = SlerpEuler(startRot, endRot, u);
            float fovNow = Mathf.Lerp(minFOV, endFOV, u);

            float Hnow = preserveScale ? Mathf.Lerp(H2D, H3D, u) : H3D;
            float tanHalf = Mathf.Tan(Mathf.Deg2Rad * Mathf.Max(0.001f, fovNow) * 0.5f);
            float LmagNow = preserveScale
                ? Mathf.Max(0.01f, Hnow / Mathf.Max(0.0001f, tanHalf))
                : Mathf.Lerp(startLmag, endLmag, u);

            Vector3 posNow = new Vector3(
                Mathf.Lerp(startPos.x, endPos.x, u),
                Mathf.Lerp(startPos.y, endPos.y, u),
                -LmagNow // on “avance” vers l’état cible en –Z
            );

            transform.localEulerAngles = rotNow;
            transform.localPosition = posNow;
            _cam.fieldOfView = fovNow;

            t += Time.deltaTime;
            yield return null;
        }

        // Fin 3D
        transform.localEulerAngles = endRot;
        transform.localPosition = endPos;
        _cam.orthographic = false;
        _cam.fieldOfView = endFOV;

        is3D = true;
        _isFlipping = false;
    }

    // -------------------- UTILITAIRES --------------------

    static Vector3 SlerpEuler(Vector3 aDeg, Vector3 bDeg, float t)
    {
        Quaternion qa = Quaternion.Euler(aDeg);
        Quaternion qb = Quaternion.Euler(bDeg);
        return Quaternion.Slerp(qa, qb, Mathf.Clamp01(t)).eulerAngles;
    }

    static float HeightToDistance(float halfHeight, float fovDeg)
    {
        // L = H / tan(FOV/2)
        float tanHalf = Mathf.Tan(Mathf.Deg2Rad * Mathf.Max(0.001f, fovDeg) * 0.5f);
        return Mathf.Max(0.01f, halfHeight / Mathf.Max(0.0001f, tanHalf));
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute {}
}
