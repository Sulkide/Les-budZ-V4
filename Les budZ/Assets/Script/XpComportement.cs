using System.Collections.Generic;
using UnityEngine;

public class XpComportement : MonoBehaviour
{
    public int xp = 50;
    public TagHandle tagSelected;
    // Rayon de détection autour de l'objet (modifiable dans l'Inspector)
    public float detectionRadius = 5f;
    // Accélération appliquée lorsque la cible est détectée
    public float acceleration = 2f;
    // Vitesse actuelle accumulée lors de l'approche de la cible
    private float currentSpeed = 0f;
    List<string> clipsRandomBop = new List<string> { "bop1", "bop2", "bop3", "bop4"};
    void Start()
    {
        // Appeler ChangeLayer après 1 seconde
        Invoke("ChangeLayer", 1f);
    }

    void Update()
    {
        // Recherche de tous les colliders dans le rayon de détection
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        Transform nearestTarget = null;
        float minDistance = Mathf.Infinity;

        // Parcours de tous les colliders détectés pour repérer ceux qui ont le tag "Target"
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Target"))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestTarget = hit.transform;
                }
            }
        }

        if (nearestTarget != null)
        {
            // Augmente progressivement la vitesse (pour un effet d'accélération)
            currentSpeed += acceleration * Time.deltaTime;
            // Calcul de la direction vers la cible
            Vector2 direction = (nearestTarget.position - transform.position).normalized;
            // Déplacement de l'objet dans la direction de la cible
            transform.position = (Vector2)transform.position + direction * currentSpeed * Time.deltaTime;
        }
        else
        {
            // Si aucune cible n'est détectée, on réinitialise la vitesse
            currentSpeed = 0f;
        }
    }

    // Méthode appelée après 1 seconde pour changer le layer
    private void ChangeLayer()
    {
        gameObject.layer = LayerMask.NameToLayer("Collectible");
    }

    // Cette fonction est appelée lorsqu'un collider configuré en trigger entre en contact
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si l'objet rencontré a le tag "Target", on supprime cet objet (ici, l'objet xp est détruit)
        if (other.CompareTag("Target"))
        {
            GameManager.instance.addXP(xp);
            SoundManager.Instance.PlayRandomSFX(clipsRandomBop, 0.9f, 1.1f);
            Destroy(gameObject);
        }
    }

    // Optionnel : affichage d'un gizmo dans l'éditeur pour visualiser le rayon de détection
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
