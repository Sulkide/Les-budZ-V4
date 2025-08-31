using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attaché à un GameObject qui possède un Collider2D (Is Trigger activé) 
/// ou un Collider2D normal (et Rigidbody2D si nécessaire). 
/// Détecte l’entrée en collision avec le joueur, joue un son (Music, SFX ou SFX aléatoire),
/// puis détruit l’objet.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AudioTrigger : MonoBehaviour
{
    public enum AudioType { Music, SFX }

    [Header("Paramètres audio du trigger")]
    [Tooltip("Choisir si ce déclencheur joue une musique ou un effet sonore.")]
    public AudioType audioType = AudioType.SFX;

    [Tooltip("Si l'effet est un SFX unique, entrer ici le nom exact du clip (AudioClip.name).")]
    public string clipName;

    [Space(5)]
    [Tooltip("Si l'on souhaite jouer un SFX aléatoirement, cocher cette case.")]
    public bool useRandomSFX = false;

    [Tooltip("Liste des noms de clips parmi lesquels choisir (AudioClip.name).")]
    public List<string> randomClipNames = new List<string>();

    [Tooltip("Pitch aléatoire minimum (ex: 0.9f).")]
    public float minPitch = 1f;
    [Tooltip("Pitch aléatoire maximum (ex: 1.1f).")]
    public float maxPitch = 1f;

    [Space(5)]
    [Tooltip("Tag utilisé pour identifier le joueur (par défaut : \"Player\").")]
    public string playerTag = "Player";

    private void Reset()
    {
        // Si le Collider2D n'est pas déjà en mode trigger, on l'active par défaut
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[AudioTrigger] Impossible de jouer le son : SoundManager.Instance est null.");
            Destroy(gameObject);
            return;
        }

        switch (audioType)
        {
            case AudioType.Music:
                // Lecture d'une musique unique
                SoundManager.Instance.PlayMusic(clipName);
                break;

            case AudioType.SFX:
                if (useRandomSFX)
                {
                    // Lecture d'un SFX aléatoire parmi la liste, avec variation de pitch
                    SoundManager.Instance.PlayRandomSFX(randomClipNames, minPitch, maxPitch);
                }
                else
                {
                    // Lecture d'un SFX unique
                    SoundManager.Instance.PlaySFX(clipName);
                }
                break;
        }

        // Détruit ce GameObject une fois le son déclenché
        Destroy(gameObject);
    }

}
