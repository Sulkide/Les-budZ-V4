using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BluePrint : MonoBehaviour
{
    public bool isCollectible;
    public int score;
    public int XP = 1000;
    public float detectionRadius = 1f;
    public string uniqueId;

    public bool isHealthBonus;

    private SpriteRenderer spriteRenderer;
    
    List<string> clipsRandom = new List<string> { "oooh1" };
    
    [Tooltip("Position de ce blueprint dans la liste UI (0 = premier slot à gauche)")]
    public int blueprintIndex;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = SceneManager.GetActiveScene().name + "_" +
                       gameObject.name + "_" +
                       transform.position.x + "_" +
                       transform.position.y + "_" +
                       transform.position.z;
        }
    }

    void Start()
    {
        // Extrait le nombre X dans "BluePrint (X)"
        string objName = gameObject.name;
        var match = Regex.Match(objName, @"\((\d+)\)$");
        if (match.Success)
        {
            blueprintIndex = int.Parse(match.Groups[1].Value);
        }
        else
        {
            Debug.LogWarning($"[{name}] impossible d'extraire un index : nom attendu 'BluePrint (X)'");
        }
    }

    private void Update()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            int layer = col.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Player") ||
                layer == LayerMask.NameToLayer("Projectile") ||
                layer == LayerMask.NameToLayer("ProjectileCollision"))
            {
                GameManager.instance.addXP(XP);

                SoundManager.Instance.PlayRandomSFX(clipsRandom, 1, 1.1f);
                
                if (isCollectible)
                {
                    string playerName = col.GetComponent<PlayerMovement>().parentName;
                    Collect(playerName);
                }
                else if (isHealthBonus)
                {
                    AddLife(col);
                }
                break;
            }
        }
    }

    private void Collect(string playerID)
    {
        GameManager.instance.addBluPrint(score, playerID);
        if (!GameManager.instance.tempCollectedBluePrint.Contains(uniqueId))
            GameManager.instance.tempCollectedBluePrint.Add(uniqueId);

        BlueprintUIManager.Instance.AnimateCollectAt(
            blueprintIndex,
            spriteRenderer.sprite,
            transform.position
        );

        Destroy(gameObject);
    }

    private void AddLife(Collider2D other)
    {
        var pm = other.gameObject.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.HasCurrentlyHealthbonus = true;
            GameManager.instance.addOrRemovePlayerBonus(pm.parentName, true);
        }
        Destroy(gameObject);
    }
}
