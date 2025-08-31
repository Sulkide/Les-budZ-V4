using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

/// <summary>
/// SoundManager gère la musique de fond et les effets sonores dans Unity 2D.
/// Utilise le pattern Singleton pour qu'une seule instance soit active en permanence.
/// On peut affecter directement dans l'Inspector deux listes d'AudioClip :
///   - musicClips : liste des musiques jouables
///   - sfxClips : liste des effets sonores jouables
/// Les méthodes PlayMusic(string name) et PlaySFX(string name) recherchent dans ces listes
/// un clip dont le nom (AudioClip.name) correspond au string fourni, puis le jouent.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // Instance Singleton accessible publiquement
    public static SoundManager Instance { get; private set; }

    [Header("Sources Audio (à assigner dans l'Inspector)")]
    [Tooltip("Source Audio pour la musique de fond")]
    public AudioSource musicSource;
    [Tooltip("Source Audio pour les effets sonores")]
    public AudioSource sfxSource;

    [Header("Listes de Clips (à remplir dans l'Inspector)")]
    [Tooltip("Liste des AudioClip pour les musiques (leur 'name' sera utilisé pour les jouer)")]
    public List<AudioClip> musicClips = new List<AudioClip>();
    [Tooltip("Liste des AudioClip pour les effets sonores (leur 'name' sera utilisé pour les jouer)")]
    public List<AudioClip> sfxClips = new List<AudioClip>();

    public UnityEngine.UI.Slider musicSlider;
    public UnityEngine.UI.Slider sfxSlider;
    
    [Header("Volumes (0.0 à 1.0)")]
    [Range(0f, 1f)] public float musicVolume = 1.0f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;

    private void Awake()
    {
        // Mise en place du Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Si les AudioSources n'ont pas été assignées, on les ajoute automatiquement
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            // Configuration initiale des AudioSources
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Applique les volumes initialement définis dans l'Inspector
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    private void Update()
    {
        musicSource.volume = musicSlider.value;
        sfxSource.volume = sfxSlider.value;
    }

    /// <summary>
    /// Joue la musique de fond dont le nom correspond à la clé fournie.
    /// Recherche l'AudioClip dans musicClips via AudioClip.name.
    /// </summary>
    /// <param name="name">Nom du clip à jouer (doit correspondre au AudioClip.name dans la liste).</param>
    public void PlayMusic(string name)
    {

        // Recherche dans la liste la première occurrence d'un clip dont le nom == name
        AudioClip clip = musicClips.Find(c => c != null && c.name == name);

        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"[SoundManager] PlayMusic : impossible de trouver une musique nommée « {name} » dans musicClips.");
        }
    }

    /// <summary>
    /// Joue un effet sonore dont le nom correspond à la clé fournie.
    /// Recherche l'AudioClip dans sfxClips via AudioClip.name.
    /// </summary>
    /// <param name="name">Nom de l'effet à jouer (doit correspondre au AudioClip.name dans la liste).</param>
    public void PlaySFX(string name)
    {
        AudioClip clip = sfxClips.Find(c => c != null && c.name == name);

        if (clip != null)
        {
            sfxSource.volume = sfxVolume;
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] PlaySFX : impossible de trouver un effet nommé « {name} » dans sfxClips.");
        }
    }

    /// <summary>
    /// Modifie le volume de la musique de fond (et l'applique immédiatement).
    /// </summary>
    /// <param name="volume">Nouveau volume (0.0 à 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    /// <summary>
    /// Modifie le volume des effets sonores (et l'applique immédiatement).
    /// </summary>
    /// <param name="volume">Nouveau volume (0.0 à 1.0).</param>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
    
    public void PlayRandomSFX(List<string> clipNames, float minPitch, float maxPitch)
    {
        // Vérification basique des paramètres
        if (clipNames == null || clipNames.Count == 0)
        {
            Debug.LogWarning("[SoundManager] PlayRandomSFX : la liste de noms est vide ou nulle.");
            return;
        }
        if (minPitch < 0f || maxPitch < minPitch)
        {
            Debug.LogWarning($"[SoundManager] PlayRandomSFX : bornes de pitch invalides (minPitch={minPitch}, maxPitch={maxPitch}).");
            return;
        }

        // Choisir un nom aléatoirement dans la liste
        int randomIndex = Random.Range(0, clipNames.Count);
        string randomName = clipNames[randomIndex];

        // Chercher l'AudioClip correspondant dans la liste sfxClips (AudioClip.name == randomName)
        AudioClip clip = sfxClips.Find(c => c != null && c.name == randomName);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] PlayRandomSFX : impossible de trouver le clip nommé « {randomName} » dans sfxClips.");
            return;
        }

        // Calculer un pitch aléatoire entre minPitch et maxPitch
        float randomPitch = Random.Range(minPitch, maxPitch);

        // Appliquer le pitch et le volume, puis jouer le clip une seule fois
        sfxSource.pitch = randomPitch;
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip);
        //sfxSource.pitch = 1.0f;
    }
    
    public void FadeMusic(float duration)
    {
        // Si aucune musique n’est en cours, on ne fait rien
        if (musicSource == null || !musicSource.isPlaying)
            return;

        // Démarre la coroutine de fade
        StartCoroutine(FadeMusicCoroutine(duration));
    }
    
    
    
    private IEnumerator FadeMusicCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        // Tant qu’on n’a pas atteint la durée totale, on diminue le volume
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Calcule le volume courant (linéaire entre startVolume et 0)
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        // À la fin du fade, on s’assure que le volume est bien à zéro
        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.clip = null;// On arrête la lecture de la musique
        musicSource.volume = musicVolume;
    }


}
