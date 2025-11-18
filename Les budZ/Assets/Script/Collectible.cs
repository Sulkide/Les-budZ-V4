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
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider col in colliders)
        {
            if (col.gameObject == gameObject)
                continue;

            int layer = col.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Player") ||
                layer == LayerMask.NameToLayer("Projectile") ||
                layer == LayerMask.NameToLayer("ProjectileCollision"))
            {
                PlayerMovement3D pm = col.GetComponent<PlayerMovement3D>();
                
                if (isCollectible && pm)
                {
                    string playerName = pm.parent.name;
                    GameManager.instance.addXP(XP);
                    SoundManager.Instance.PlayRandomSFXOnNextBarEnd(clipsRandomSnap, 0.9f, 1.1f);
                    Collect(playerName);
                }
                
                break;
            }
        }
    }

    private void Collect(string playerID)
    {
        GameManager.instance.addScore(score, playerID);
        if (!GameManager.instance.tempCollectedCollectibles.Contains(uniqueId))
        {
            GameManager.instance.tempCollectedCollectibles.Add(uniqueId);
        }
        Destroy(gameObject);
    }
    
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, detectionRadius);
    }
}