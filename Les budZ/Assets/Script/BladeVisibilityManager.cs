using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-100)]  // s’assure que ce script Awake() tourne avant les GrassBlade
public class BladeCullingManager : MonoBehaviour
{
    public static BladeCullingManager Instance { get; private set; }
    [Tooltip("Rayon approximatif de chaque blade pour le culling")]
    public float sphereRadius = 0.5f;

    public CullingGroup            cullingGroup;
    public List<GameObject>        blades      = new List<GameObject>();
    public BoundingSphere[]        spheres;
    public Camera                  mainCam;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        mainCam  = Camera.main;

        cullingGroup = new CullingGroup();
        cullingGroup.targetCamera = mainCam;
        cullingGroup.onStateChanged += OnStateChanged;
    }

    void OnDestroy()
    {
        cullingGroup.onStateChanged -= OnStateChanged;
        cullingGroup.Dispose();
        Instance = null;
    }

    // Appelée par chaque GrassBlade dans OnEnable
    public void RegisterBlade(GameObject blade)
    {
        if (blades.Contains(blade)) return;
        blades.Add(blade);
        RebuildSpheres();
    }

    // Appelée par chaque GrassBlade dans OnDisable/OnDestroy
    public void UnregisterBlade(GameObject blade)
    {
     
    }

    // Reconstruit le tableau de BoundingSpheres à chaque changement de liste
    void RebuildSpheres()
    {
        int count = blades.Count;
        spheres = new BoundingSphere[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = blades[i].transform.position;
            spheres[i]  = new BoundingSphere(pos, sphereRadius);
        }
        cullingGroup.SetBoundingSpheres(spheres);
        cullingGroup.SetBoundingSphereCount(count);
    }

    // Callback CullingGroup : active / désactive le blade
    void OnStateChanged(CullingGroupEvent evt)
    {
        var blade = blades[evt.index];
        if (blade != null && blade.activeSelf != evt.isVisible)
            blade.SetActive(evt.isVisible);
    }

    void LateUpdate()
    {
        // Mise à jour des positions des spheres pour les blades mobiles
        for (int i = 0; i < blades.Count; i++)
            spheres[i].position = blades[i].transform.position;
        cullingGroup.SetBoundingSpheres(spheres);
    }
}
