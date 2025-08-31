using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlatformEffector2D))]
public class OneWayPlatform : MonoBehaviour
{
    [Header("Effector Settings")]
    [Tooltip("Arc de surface autorisant le passage par-dessous (180 = demi-tour complet)")]
    public float surfaceArc = 180f;
    [Tooltip("Active le one-way (passage par-dessous)")]
    public bool useOneWay = true;
    [Tooltip("Regroupe les collisions pour éviter les problèmes de multi-colliders")]
    public bool useOneWayGrouping = true;

    [Header("Drop-Through Settings")]
    [Tooltip("Durée pendant laquelle la collision est désactivée après appui ↓ (secondes)")]
    public float dropDuration = 0.5f;
    [Tooltip("Tag du joueur pour le drop-through")]
    public string playerTag = "Player";
    [Tooltip("Nom de l’axe vertical (ex. \"Vertical\")")]
    public string verticalAxis = "Vertical";

    private Collider2D platformCollider;
    private PlatformEffector2D effector;
    private HashSet<Collider2D> ignoredColliders = new HashSet<Collider2D>();

    void Awake()
    {
        // Récupère et configure le Collider2D
        platformCollider = GetComponent<Collider2D>();
        platformCollider.usedByEffector = true;

        // Récupère et configure le PlatformEffector2D
        effector = GetComponent<PlatformEffector2D>();
        effector.useOneWay = useOneWay;
        effector.useOneWayGrouping = useOneWayGrouping;
        effector.surfaceArc = surfaceArc;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // Si c'est le joueur, qu'il appuie vers le bas et qu'on n'a pas déjà désactivé la collision
        if (collision.collider.CompareTag(playerTag)
            && Input.GetAxisRaw(verticalAxis) < 0f
            && !ignoredColliders.Contains(collision.collider))
        {
            StartCoroutine(ReenableCollision(collision.collider));
            Physics2D.IgnoreCollision(platformCollider, collision.collider, true);
            ignoredColliders.Add(collision.collider);
        }
    }

    private IEnumerator ReenableCollision(Collider2D playerCollider)
    {
        // Attend le temps défini, puis réactive la collision
        yield return new WaitForSeconds(dropDuration);
        Physics2D.IgnoreCollision(platformCollider, playerCollider, false);
        ignoredColliders.Remove(playerCollider);
    }
}
