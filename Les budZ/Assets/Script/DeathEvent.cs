using UnityEngine;

public class DeathEvent : MonoBehaviour
{

    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerMovement move;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        move = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
}
