using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Collectible : MonoBehaviour
{
    
    public bool isCollectible;
    public int score;
    public int XP = 100;
    public float detectionRadius = 0.5f;
    public string uniqueId; // Identifiant généré automatiquement

    List<string> clipsRandomSnap = new List<string> { "snap1" };
    public bool isHealthBonus;
    void Awake()
    {
        // Génération d'un identifiant unique basé sur la scène, le nom de l'objet et sa position
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = SceneManager.GetActiveScene().name + "_" +
                       gameObject.name + "_" +
                       transform.position.x + "_" +
                       transform.position.y + "_" +
                       transform.position.z;
        }
    }

    private void Update()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == gameObject)
                continue;

            int layer = col.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Player") ||
                layer == LayerMask.NameToLayer("Projectile") ||
                layer == LayerMask.NameToLayer("ProjectileCollision"))
            {
                
                
                
                
                PlayerMovement pm = col.GetComponent<PlayerMovement>();
                
                if (isCollectible && pm)
                {
                    string playerName = pm.parentName;
                    GameManager.instance.addXP(XP);
                    SoundManager.Instance.PlayRandomSFX(clipsRandomSnap, 1, 1.5f);
                    Collect(playerName);
                }

                if (isHealthBonus)
                {
                    SoundManager.Instance.PlayRandomSFX(clipsRandomSnap, 1, 1.5f);
                    GameManager.instance.addXP(XP);
                    AddLife(col);
                }
                
                break;
            }
        }
    }
    
    private void Collect(string playerID)
    {
        GameManager.instance.addScore(score, playerID);
        // Ajoute l'ID dans la liste temporaire (et non directement dans la liste permanente)
        if (!GameManager.instance.tempCollectedCollectibles.Contains(uniqueId))
        {
            GameManager.instance.tempCollectedCollectibles.Add(uniqueId);
        }
        Destroy(gameObject);
    }

    private void AddLife(Collider2D other)
    {
        PlayerMovement pm = other.gameObject.GetComponent<PlayerMovement>();
        
        if (pm)
        {
            pm.HasCurrentlyHealthbonus = true;
            GameManager.instance.addOrRemovePlayerBonus(pm.parentName, true);
        }
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}