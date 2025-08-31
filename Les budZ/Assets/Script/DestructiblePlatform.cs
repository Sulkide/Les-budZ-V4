using System.Collections.Generic;
using UnityEngine;

public class DestructiblePlatform : MonoBehaviour
{
    [SerializeField] private float force = 10f;
    [SerializeField] private float destroyDelay = 3f;
    List<string> clipsRandom = new List<string> { "pin" };
    private void Start()
    {
        foreach (Transform child in transform)
        {
            Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
        if (playerMovement != null && playerMovement.isDashing)
        {
            Debug.Log("Destructible");
            SoundManager.Instance.PlayRandomSFX(clipsRandom, 0.9f, 1.1f);
            gameObject.layer = LayerMask.NameToLayer("Default");
            
            Vector2 pushDirection = playerMovement.moveInput.normalized;

            foreach (Transform child in transform)
            {
                Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.constraints = RigidbodyConstraints2D.None;

                    Vector2 randomOffset = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));
                    Vector2 finalForce = (pushDirection + randomOffset).normalized * force;

                    rb.AddForce(finalForce, ForceMode2D.Impulse);
                }
            }

            Destroy(gameObject, destroyDelay);
        }
    }
}