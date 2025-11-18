using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public AudioSource musicSource;
    public AudioSource sfxSource;

    public List<AudioClip> musicClips = new List<AudioClip>();
    public List<AudioClip> sfxClips = new List<AudioClip>();

    public UnityEngine.UI.Slider musicSlider;
    public UnityEngine.UI.Slider sfxSlider;

    [Range(0f, 1f)] public float musicVolume = 1.0f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;

    public bool enableBeatManager = true;

    [SerializeField] private int currentTopNumber = 4;
    [SerializeField] private int currentBottomNumber = 4;
    [SerializeField] private bool beatManagerActive = false;
    [SerializeField] private float currentBPM = 120f;
    [SerializeField] private float currentBeatOffset = 0f;
    [SerializeField] private float currentBarDuration = 2f;
    [SerializeField] private int lastBeatBarIndex = -1;

    private float lastAudioTime = 0f;

    private bool barEndPlaybackPending = false;
    private Coroutine barEndPlaybackCoroutine = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

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
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
        //ATTENTION A ENLEVER EN BUILD
        SoundManager.Instance.PlayMusic("Bourée (Steven Wilson Remix)", 120, 0.452f, 2,4, true);
    }

    private void Update()
    {
        if (musicSlider != null)
        {
            musicVolume = musicSlider.value;
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        if (sfxSlider != null)
        {
            sfxVolume = sfxSlider.value;
        }
    }

    private void FixedUpdate()
    {
        if (!beatManagerActive || musicSource == null || !musicSource.isPlaying)
            return;

        if (currentBarDuration <= 0f)
            return;

        float audioTime = musicSource.time;

        if (audioTime < lastAudioTime)
        {
            ResetLastBeatIndex();
        }
        lastAudioTime = audioTime;

        float t = audioTime - currentBeatOffset;
        if (t < 0f)
            return;

        int completedBars = Mathf.FloorToInt(t / currentBarDuration);

        if (completedBars != lastBeatBarIndex)
        {
            Debug.Log("BEAT");
            lastBeatBarIndex = completedBars;
        }
    }

    private void RecalculateBarDuration()
    {
        currentTopNumber = Mathf.Max(1, currentTopNumber);
        currentBottomNumber = Mathf.Max(1, currentBottomNumber);

        float quarterDuration = 60f / currentBPM;
        float beatNoteDuration = quarterDuration * (4f / currentBottomNumber);
        currentBarDuration = beatNoteDuration * currentTopNumber;
    }

    private void ResetLastBeatIndex()
    {
        if (musicSource == null || currentBarDuration <= 0f)
        {
            lastBeatBarIndex = -1;
            return;
        }

        float audioTime = musicSource.time;
        float t = audioTime - currentBeatOffset;

        if (t < 0f)
        {
            lastBeatBarIndex = -1;
        }
        else
        {
            lastBeatBarIndex = Mathf.FloorToInt(t / currentBarDuration);
        }
    }

    public void PlayMusic(string name)
    {
        AudioClip clip = musicClips.Find(c => c != null && c.name == name);

        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();

            StopBeatManager();
        }
        else
        {
            Debug.LogWarning($"[SoundManager] PlayMusic : impossible de trouver une musique nommée « {name} » dans musicClips.");
        }
    }

    public void PlayMusic(string name, float beatManagerBPM, float beatManagerOffset, int topNumber, int bottomNumber, bool activeBeatManager = true)
    {
        AudioClip clip = musicClips.Find(c => c != null && c.name == name);

        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();

            if (enableBeatManager && activeBeatManager)
            {
                StartBeatManager(beatManagerBPM, beatManagerOffset, topNumber, bottomNumber);
            }
            else
            {
                StopBeatManager();
            }
        }
        else
        {
            Debug.LogWarning($"[SoundManager] PlayMusic : impossible de trouver une musique nommée « {name} » dans musicClips.");
        }
    }

    private void StartBeatManager(float bpm, float beatOffset, int topNumber, int bottomNumber)
    {
        if (!enableBeatManager)
        {
            beatManagerActive = false;
            return;
        }

        currentBPM = Mathf.Max(1f, bpm);
        currentBeatOffset = Mathf.Max(0f, beatOffset);
        currentTopNumber = Mathf.Max(1, topNumber);
        currentBottomNumber = Mathf.Max(1, bottomNumber);

        RecalculateBarDuration();
        beatManagerActive = true;

        lastAudioTime = musicSource != null ? musicSource.time : 0f;
        ResetLastBeatIndex();
    }

    private void StopBeatManager()
    {
        beatManagerActive = false;
        lastBeatBarIndex = -1;
        lastAudioTime = 0f;

        if (barEndPlaybackPending && barEndPlaybackCoroutine != null)
        {
            StopCoroutine(barEndPlaybackCoroutine);
        }

        barEndPlaybackPending = false;
        barEndPlaybackCoroutine = null;
    }

    public void PlayMusicOneShot(string name)
    {
        AudioClip clip = musicClips.Find(c => c != null && c.name == name);

        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] PlayMusicOneShot : impossible de trouver une musique nommée « {name} » dans musicClips.");
            return;
        }

        AudioSource tempSource = CreateTempAudioSource($"MusicOneShot_{name}", true);
        tempSource.clip = clip;
        tempSource.loop = false;
        tempSource.Play();
        StartCoroutine(DestroyAfterPlay(tempSource));
    }

    public void PlaySFX(string name)
    {
        AudioClip clip = sfxClips.Find(c => c != null && c.name == name);

        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] PlaySFX : impossible de trouver un effet nommé « {name} » dans sfxClips.");
            return;
        }

        AudioSource tempSource = CreateTempAudioSource($"SFX_{name}", false);
        tempSource.clip = clip;
        tempSource.loop = false;
        tempSource.Play();

        StartCoroutine(DestroyAfterPlay(tempSource));
    }

    public void PlaySFXOnNextBarEnd(string name)
    {
        if (barEndPlaybackPending)
            return;

        if (!beatManagerActive || musicSource == null || !musicSource.isPlaying || currentBarDuration <= 0f)
        {
            PlaySFX(name);
            return;
        }

        float audioTime = musicSource.time;
        float t = Mathf.Max(0f, audioTime - currentBeatOffset);
        int completedBars = Mathf.FloorToInt(t / currentBarDuration);
        float nextBarEndAudioTime = currentBeatOffset + (completedBars + 1) * currentBarDuration;
        float delay = Mathf.Max(0f, nextBarEndAudioTime - audioTime);

        barEndPlaybackPending = true;
        barEndPlaybackCoroutine = StartCoroutine(PlaySFXDelayed(name, delay));
    }

    private IEnumerator PlaySFXDelayed(string name, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (beatManagerActive && musicSource != null && musicSource.isPlaying)
            PlaySFX(name);

        barEndPlaybackPending = false;
        barEndPlaybackCoroutine = null;
    }

    public void PlayRandomSFX(List<string> clipNames, float minPitch, float maxPitch)
    {
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

        int randomIndex = Random.Range(0, clipNames.Count);
        string randomName = clipNames[randomIndex];

        AudioClip clip = sfxClips.Find(c => c != null && c.name == randomName);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] PlayRandomSFX : impossible de trouver le clip nommé « {randomName} » dans sfxClips.");
            return;
        }

        float randomPitch = Random.Range(minPitch, maxPitch);

        AudioSource tempSource = CreateTempAudioSource($"SFX_Random_{randomName}", false);
        tempSource.pitch = randomPitch;
        tempSource.clip = clip;
        tempSource.loop = false;
        tempSource.Play();

        StartCoroutine(DestroyAfterPlay(tempSource));
    }

    public void PlayRandomSFXOnNextBarEnd(List<string> clipNames, float minPitch, float maxPitch)
    {
        if (barEndPlaybackPending)
            return;

        if (!beatManagerActive || musicSource == null || !musicSource.isPlaying || currentBarDuration <= 0f)
        {
            PlayRandomSFX(clipNames, minPitch, maxPitch);
            return;
        }

        float audioTime = musicSource.time;
        float t = Mathf.Max(0f, audioTime - currentBeatOffset);
        int completedBars = Mathf.FloorToInt(t / currentBarDuration);
        float nextBarEndAudioTime = currentBeatOffset + (completedBars + 1) * currentBarDuration;
        float delay = Mathf.Max(0f, nextBarEndAudioTime - audioTime);

        barEndPlaybackPending = true;
        barEndPlaybackCoroutine = StartCoroutine(PlayRandomSFXDelayed(clipNames, minPitch, maxPitch, delay));
    }

    private IEnumerator PlayRandomSFXDelayed(List<string> clipNames, float minPitch, float maxPitch, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (beatManagerActive && musicSource != null && musicSource.isPlaying)
            PlayRandomSFX(clipNames, minPitch, maxPitch);

        barEndPlaybackPending = false;
        barEndPlaybackCoroutine = null;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void FadeMusic(float duration)
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        StartCoroutine(FadeMusicCoroutine(duration));
    }

    private IEnumerator FadeMusicCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = musicVolume;

        StopBeatManager();
    }

    private AudioSource CreateTempAudioSource(string objectName, bool isMusic)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform);

        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.volume = isMusic ? musicVolume : sfxVolume;

        return src;
    }

    private IEnumerator DestroyAfterPlay(AudioSource src)
    {
        if (src == null) yield break;

        GameObject go = src.gameObject;

        while (src != null && src.isPlaying)
        {
            yield return null;
        }

        if (go != null)
            Destroy(go);
    }
}
