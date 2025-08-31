using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Attractor : MonoBehaviour
{
    
    public LayerMask AttractionLayer ;
    public float gravity = 10f;
    public float LocalPlayerGravityForce = 45f;
    [SerializeField] private float Radius = 10f;
    public List<Collider2D> AttractedObjects = new List<Collider2D>();
    private Transform attractorTransform;

    void Awake()
    {
        AttractionLayer = LayerMask.GetMask("Player", "Projectile", "ProjectileCollision", "Bullet", "BulletCollison", "Enemy");
        attractorTransform = transform;
    }

    void Update()
    {
        SetAttractedObjects();
        
        foreach (Collider2D collider in AttractedObjects)
        {
            PlayerMovement playerMovement = collider.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.gravityJumpForce = LocalPlayerGravityForce;
            }
        }
    }

    void FixedUpdate()
    {
        AttractObjects();
    }

    void SetAttractedObjects()
    {
        AttractedObjects = Physics2D.OverlapCircleAll(attractorTransform.position, Radius, AttractionLayer).ToList();
    }
    
    void AttractObjects()
    {
        AttractedObjects.RemoveAll(item => item == null);
    
        foreach (Collider2D collider in AttractedObjects)
        {
            Attractable attractable = collider.GetComponent<Attractable>();
            if (attractable != null)
            {
                attractable.Attract(this);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}
