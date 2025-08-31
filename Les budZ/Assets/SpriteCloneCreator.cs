using UnityEngine;
using Kolibri2d;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteCloneCreator : MonoBehaviour
{
    [Header("Clone Settings")]
    public Vector3 positionOffset = Vector3.zero;
    public Color overrideColor = Color.white;
    public Material overrideMaterial;

    [Header("Z→Alpha Range")]
    public float rangeActual;
    public float rangeMin = 0f;
    public float rangeMax = 1f;

    private SpriteRenderer _src;
    private GameObject _cloneGO;
    private SpriteRenderer _cloneSR;

    void Start()
    {
        _src = GetComponent<SpriteRenderer>();

        // Si on a un SplineDecorator, récupérer rangeMin / rangeMax depuis lui
        var spline = FindSplineDecoratorUpwards(transform);
        if (spline != null)
        {
            rangeMax = spline.decorationMaterial.global_range_manual.range3D.x;
            rangeMin = spline.decorationMaterial.global_range_manual.range3D.y;
        }

        // Déterminer rangeActual depuis le parent si présent
        if (transform.parent != null)
            rangeActual = transform.parent.localPosition.z;
        else
            rangeActual = transform.localPosition.z;

        // Crée le clone uniquement en Play Mode
        if (Application.isPlaying)
            CreateClone();
    }

    private void CreateClone()
    {
        _cloneGO = new GameObject(name + " (CloneSR)");
        _cloneGO.transform.SetParent(transform, false);
        _cloneGO.transform.localPosition = positionOffset;
        _cloneGO.transform.localRotation = Quaternion.identity;
        _cloneGO.transform.localScale = Vector3.one;

        _cloneSR = _cloneGO.AddComponent<SpriteRenderer>();

        // Copie du sprite et paramètres de base
        _cloneSR.sprite = _src.sprite;
        _cloneSR.flipX = _src.flipX;
        _cloneSR.flipY = _src.flipY;
        _cloneSR.drawMode = _src.drawMode;
        _cloneSR.size = _src.size;

        // Sorting : même layer mais order + 1
        _cloneSR.sortingLayerID = _src.sortingLayerID;
        _cloneSR.sortingOrder = _src.sortingOrder + 1;

        // Matériau override ou copie du matériau original
        if (overrideMaterial != null)
            _cloneSR.sharedMaterial = overrideMaterial;
        else if (_src.sharedMaterial != null)
            _cloneSR.material = new Material(_src.sharedMaterial);

        // Calcul alpha en fonction de la plage Z
        float t = Mathf.InverseLerp(rangeMin, rangeMax, rangeActual);
        float a = Mathf.Lerp(overrideColor.a, 0f, t);
        var col = new Color(overrideColor.r, overrideColor.g, overrideColor.b, a);
        _cloneSR.color = col;
    }

    private void OnDisable()
    {
        DestroyCloneImmediateIfAny();
    }

    private void OnDestroy()
    {
        DestroyCloneImmediateIfAny();
    }

    private void DestroyCloneImmediateIfAny()
    {
        if (_cloneGO != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(_cloneGO);
            else
                Destroy(_cloneGO);
#else
            Destroy(_cloneGO);
#endif
            _cloneGO = null;
            _cloneSR = null;
        }
    }

    private SplineDecorator FindSplineDecoratorUpwards(Transform from)
    {
        Transform t = from.parent;
        while (t != null)
        {
            if (t.TryGetComponent(out SplineDecorator sd))
                return sd;
            t = t.parent;
        }
        return null;
    }
}
