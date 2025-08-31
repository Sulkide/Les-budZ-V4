using System.Collections;
using UnityEngine;

public class ProjectileSulana : MonoBehaviour
{
    private Rigidbody2D rb;
    
    public float dampingFactor = 0.8f;
    public float destroyAfterTime;
    public float DivideTime;
    public GameObject ProjectileDivide;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(DestroyAfter(destroyAfterTime));
        StartCoroutine(Divide(DivideTime));
    }

    private IEnumerator DestroyAfter(float TimeToDestroy)
    {
        yield return new WaitForSeconds(TimeToDestroy);
        Destroy(gameObject);
    }
    
    private IEnumerator Divide(float DivideTime)
    {
        yield return new WaitForSeconds(DivideTime);
        
        Vector2 currentVelocity = rb.linearVelocity;

        float[] angles = new float[] { 10f, 5f, 0, -5f, -10f };

        foreach (float angle in angles)
        {
            GameObject newProjectile = Instantiate(ProjectileDivide, transform.position, transform.rotation);
            Rigidbody2D rbNew = newProjectile.GetComponent<Rigidbody2D>();
            if (rbNew != null)
            {
                rbNew.linearVelocity = RotateVector(currentVelocity, angle);
            }
        }
        
        Destroy(gameObject);
    }
    
    private Vector2 RotateVector(Vector2 v, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) || (collision.gameObject.layer == LayerMask.NameToLayer("EnemyProjectile")))
        {
            Destroy(gameObject);
        }
    }
}
