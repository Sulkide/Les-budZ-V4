using System.Collections;
using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    
    private Rigidbody2D rb;
    
    public float dampingFactor = 0.8f;
    
    public float destroyAfterTime;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(DestroyAfter(destroyAfterTime));
    }

    private IEnumerator DestroyAfter(float TimeToDestroy)
    {
        yield return new WaitForSeconds(TimeToDestroy);
        Destroy(gameObject);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) || (collision.gameObject.layer == LayerMask.NameToLayer("EnemyProjectile")))
        {
            gameObject.layer = LayerMask.NameToLayer("Default");
            Vector2 newDirection = Vector2.Reflect(rb.linearVelocity, collision.contacts[0].normal);
            rb.linearVelocity = newDirection * dampingFactor;
            rb.gravityScale = 2f;
        }
    }
}