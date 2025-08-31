using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlateformeSuspendue : MonoBehaviour
{
    [System.Serializable]
    public class Cable
    {
        [Tooltip("Point d'ancrage sur la plateforme (Transform, enfant de la plateforme).")]
        public Transform pointAncragePlateforme;
        
        [Tooltip("Point d'ancrage fixe dans la scène (Transform, position fixe dans le monde).")]
        public Transform pointAncrageScene;
    }

    [Tooltip("Liste des câbles attachés à la plateforme, configurables dans l'inspecteur.")]
    public List<Cable> cables = new List<Cable>();

    [Tooltip("Matériau pour représenter visuellement les câbles (LineRenderer).")]
    public Material cableMaterial;
    
    [Tooltip("Couleur du visuel du câble.")]
    public Color cableColor = Color.gray;
    
    [Tooltip("Épaisseur visuelle du câble.")]
    public float cableWidth = 0.1f;

    // Listes pour stocker les câbles actifs, leurs joints et leurs LineRenderers
    private List<Cable> activeCables = new List<Cable>();
    private List<DistanceJoint2D> distanceJoints = new List<DistanceJoint2D>();
    private List<LineRenderer> cableLines = new List<LineRenderer>();

    // Utilisation d'un HashSet pour ne comptabiliser chaque objet qu'une seule fois
    private HashSet<GameObject> collidingPlayers = new HashSet<GameObject>();

    // Référence à la coroutine de suppression des câbles
    private Coroutine removeCoroutine = null;

    void Start()
    {
        // Vérifier la présence d'un Rigidbody2D et le configurer en dynamique
        Rigidbody2D rbPlateforme = GetComponent<Rigidbody2D>();
        if (rbPlateforme == null)
        {
            Debug.LogError("PlateformeSuspendue: Aucun Rigidbody2D trouvé sur la plateforme.");
            return;
        }
        rbPlateforme.bodyType = RigidbodyType2D.Dynamic;

        // Création des joints et des LineRenderers pour chaque câble défini
        foreach (Cable cable in cables)
        {
            if (cable.pointAncragePlateforme == null || cable.pointAncrageScene == null)
            {
                Debug.LogWarning("PlateformeSuspendue: Un câble n'a pas tous ses points d'ancrage assignés et sera ignoré.");
                continue;
            }

            activeCables.Add(cable);

            // Calcul de la position locale d'ancrage sur la plateforme
            Vector2 ancrageLocalPlateforme = cable.pointAncragePlateforme.IsChildOf(transform)
                ? cable.pointAncragePlateforme.localPosition
                : transform.InverseTransformPoint(cable.pointAncragePlateforme.position);
            Vector2 positionAncrageScene = cable.pointAncrageScene.position;

            // Création et configuration du DistanceJoint2D pour le câble
            DistanceJoint2D joint = gameObject.AddComponent<DistanceJoint2D>();
            joint.autoConfigureDistance = false;
            joint.maxDistanceOnly = true;
            joint.enableCollision = false;
            joint.anchor = ancrageLocalPlateforme;
            joint.connectedAnchor = positionAncrageScene;
            joint.connectedBody = null;
            float longueurCable = Vector2.Distance(transform.TransformPoint(ancrageLocalPlateforme), positionAncrageScene);
            joint.distance = longueurCable;
            distanceJoints.Add(joint);

            // Création du GameObject pour le LineRenderer et configuration de l'affichage du câble
            GameObject ligneCable = new GameObject("CableLine");
            ligneCable.transform.SetParent(transform);
            LineRenderer lr = ligneCable.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = cableWidth;
            lr.endWidth = cableWidth;
            lr.startColor = cableColor;
            lr.endColor = cableColor;
            lr.material = (cableMaterial != null) ? cableMaterial : new Material(Shader.Find("Sprites/Default"));
            cableLines.Add(lr);
        }
    }

    void LateUpdate()
    {
        // Mise à jour des positions des LineRenderers en fonction des points d'ancrage
        for (int i = 0; i < activeCables.Count && i < cableLines.Count; i++)
        {
            Cable cable = activeCables[i];
            LineRenderer lr = cableLines[i];
            if (cable.pointAncragePlateforme != null && cable.pointAncrageScene != null)
            {
                lr.SetPosition(0, cable.pointAncrageScene.position);
                lr.SetPosition(1, cable.pointAncragePlateforme.position);
            }
        }
    }

    void Update()
    {
        // Dès qu'il y a plus d'un objet unique détecté sur la plateforme, déclencher la destruction
        if (removeCoroutine == null && collidingPlayers.Count > 1)
        {
            removeCoroutine = StartCoroutine(RemoveCableRoutine());
        }
    }

    // Coroutine qui supprime un câble toutes les 4 secondes tant qu'il reste des câbles
    // Une fois déclenchée, elle continue même si la plateforme ne détecte plus de collision
    IEnumerator RemoveCableRoutine()
    {
        while (activeCables.Count > 0)
        {
            yield return new WaitForSeconds(4f);
            
            if (activeCables.Count > 0)
            {
                int indexToRemove = activeCables.Count - 1;

                // Suppression du DistanceJoint2D associé
                Destroy(distanceJoints[indexToRemove]);
                distanceJoints.RemoveAt(indexToRemove);

                // Suppression du LineRenderer associé
                Destroy(cableLines[indexToRemove].gameObject);
                cableLines.RemoveAt(indexToRemove);

                // Retrait du câble de la liste active
                activeCables.RemoveAt(indexToRemove);
                Debug.Log("Un câble a été rompu. Il en reste " + activeCables.Count);
            }
        }
        // La coroutine s'arrête naturellement lorsque tous les câbles ont été détruits
        removeCoroutine = null;
    }

    // Détection des collisions avec un GameObject possédant PlayerMovement et le tag "Target"
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Target") &&
            collision.gameObject.GetComponentInParent<PlayerMovement>() != null)
        {
            // Le HashSet ajoute l'objet uniquement s'il n'est pas déjà présent
            if (collidingPlayers.Add(collision.gameObject))
            {
                Debug.Log("Collision ajoutée : " + collision.gameObject.name);
            }
        }
    }

    // Retrait de l'objet du HashSet lors de la fin de la collision
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Target") &&
            collision.gameObject.GetComponentInParent<PlayerMovement>() != null)
        {
            if (collidingPlayers.Remove(collision.gameObject))
            {
                Debug.Log("Collision retirée : " + collision.gameObject.name);
            }
        }
    }
}
