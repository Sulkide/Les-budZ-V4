using UnityEngine;
using System.Collections.Generic;
using UnityEngine.U2D.Animation;

public class SpriteShadow : MonoBehaviour
{
    [Tooltip("Décalage de l’ombre par rapport au personnage (en unités monde).")]
    public Vector2 decalageOmbre = new Vector2(0.5f, 0f);
    [Tooltip("Couleur de l’ombre (par défaut noir semi-transparent).")]
    public Color couleurOmbre = new Color(0f, 0f, 0f, 0.5f);

    private GameObject ombre;
    private Transform[] osOriginaux;
    private Transform[] osOmbre;
    private SpriteRenderer[] srOriginaux;
    private SpriteRenderer[] srOmbre;

    void Start()
    {
        // Si le root a le tag DontClone ou est déjà une ombre, ne rien faire
        if (gameObject.CompareTag("DontClone") || name.EndsWith("_Shadow"))
            return;

        // 1. Construire mapping des transforms originaux (pour gérer DontClone)
        var originalMap = new Dictionary<string, Transform>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (!originalMap.ContainsKey(t.name))
                originalMap[t.name] = t;
        }

        // 2. Instancier le clone
        Transform parentOrigine = transform.parent;
        Vector3 posInit = transform.position;
        Quaternion rotInit = transform.rotation;
        ombre = Instantiate(gameObject, posInit, rotInit, parentOrigine);
        ombre.name = gameObject.name + "_Shadow";

        // 3. Supprimer les sous-éléments dont l'original possède le tag DontClone
        foreach (Transform tClone in ombre.GetComponentsInChildren<Transform>(true))
        {
            if (originalMap.TryGetValue(tClone.name, out Transform origT) && origT.gameObject.CompareTag("DontClone"))
            {
                DestroyImmediate(tClone.gameObject);
            }
        }

        // 4. Nettoyer le clone (composants non nécessaires)
        DestroyImmediate(ombre.GetComponent<SpriteShadow>());
        DestroyImmediate(ombre.GetComponent<Animator>());
        foreach (MonoBehaviour comp in ombre.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(comp is SpriteSkin) && !(comp is SpriteRenderer))
                DestroyImmediate(comp);
        foreach (Collider2D col in ombre.GetComponentsInChildren<Collider2D>(true))
            DestroyImmediate(col);
        

        // 5. Mapping des bones (os)
        var mapBones = new Dictionary<string, Transform>();
        foreach (Transform t in ombre.GetComponentsInChildren<Transform>(true))
        {
            if (!mapBones.ContainsKey(t.name))
                mapBones[t.name] = t;
        }
        var listOsOrig = new List<Transform>();
        var listOsOmb = new List<Transform>();
        foreach (Transform os in GetComponentsInChildren<Transform>(true))
        {
            if (mapBones.TryGetValue(os.name, out Transform osClone) && os != transform)
            {
                listOsOrig.Add(os);
                listOsOmb.Add(osClone);
            }
        }
        osOriginaux = listOsOrig.ToArray();
        osOmbre = listOsOmb.ToArray();

        // 6. Mapping des SpriteRenderer
        var srsOrig = GetComponentsInChildren<SpriteRenderer>(true);
        var listSrOrig = new List<SpriteRenderer>();
        var listSrOmb = new List<SpriteRenderer>();
        foreach (var sr in srsOrig)
        {
            if (mapBones.TryGetValue(sr.transform.name, out Transform tClone))
            {
                var srClone = tClone.GetComponent<SpriteRenderer>();
                if (srClone != null)
                {
                    listSrOrig.Add(sr);
                    listSrOmb.Add(srClone);
                }
            }
        }
        srOriginaux = listSrOrig.ToArray();
        srOmbre = listSrOmb.ToArray();

        // 7. Appliquer couleur et sortingOrder selon tags
        for (int i = 0; i < srOmbre.Length; i++)
        {
            var oriSr = srOriginaux[i];
            var ombSr = srOmbre[i];
            ombSr.color = couleurOmbre;
            if (oriSr.gameObject.CompareTag("HigherShadow"))
                ombSr.sortingOrder = oriSr.sortingOrder-1;
            else
                ombSr.sortingOrder = 0;
        }
    }

    void LateUpdate()
    {
        if (ombre == null) return;

        // Synchroniser les os et leur activation
        for (int i = 0; i < osOriginaux.Length; i++)
        {
            var ori = osOriginaux[i];
            var omb = osOmbre[i];
            if (ori != null && omb != null)
            {
                omb.localPosition = ori.localPosition;
                omb.localRotation = ori.localRotation;
                omb.localScale = ori.localScale;
                omb.gameObject.SetActive(ori.gameObject.activeSelf);
            }
        }

        // Synchroniser les SpriteRenderer (enabled, activation)
        for (int i = 0; i < srOriginaux.Length; i++)
        {
            var oriSr = srOriginaux[i];
            var ombSr = srOmbre[i];
            if (oriSr != null && ombSr != null)
            {
                ombSr.enabled = oriSr.enabled;
                ombSr.gameObject.SetActive(oriSr.gameObject.activeSelf);
            }
        }

        // Mise à jour de la racine clone
        Vector3 offsetMonde = new Vector3(decalageOmbre.x, decalageOmbre.y, 0f);
        ombre.transform.position = transform.position + offsetMonde;
        ombre.transform.rotation = transform.rotation;
        ombre.transform.localScale = transform.localScale;
    }
}
