using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class AudioSliderMeter : MonoBehaviour
{
    public enum Band
    {
        SubBass,  // 20 – 62 Hz
        Bass,     // 62 – 250 Hz
        LowMid,   // 250 – 500 Hz
        Mid,      // 500 – 2000 Hz
        High      // 2000 – 20000 Hz
    }

    public enum ChannelOption
    {
        Left,    // canal 0
        Right,   // canal 1
        Mono     // moyenne des deux
    }

    [Header("Références")]
    public Slider volumeSlider;
    public AudioSource audioSource;

    [Header("Paramètres FFT")]
    [Tooltip("Doit être une puissance de 2.")]
    public int sampleSize = 1024;
    private float[] spectrumLeft;
    private float[] spectrumRight;
    private float binWidth;

    [Header("Choix de la bande")]
    public Band bandToMeasure = Band.Bass;

    [Header("Choix du canal")]
    public ChannelOption channelOption = ChannelOption.Mono;

    [Header("Réglages slider")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.2f;
    public float amplitudeMultiplier = 1f;
    private float currentLevel = 0f;

    void Start()
    {
        volumeSlider = GetComponent<Slider>();
        audioSource = SoundManager.Instance.musicSource;
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Prépare deux buffers pour chaque canal
        spectrumLeft  = new float[sampleSize];
        spectrumRight = new float[sampleSize];
        // Largeur d'une case FFT en Hz
        binWidth = (AudioSettings.outputSampleRate / 2f) / sampleSize;

        // Configure le slider
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
        }
    }

    void Update()
    {
        // 1) Récupère le spectre des deux canaux
        audioSource.GetSpectrumData(spectrumLeft,  0, FFTWindow.BlackmanHarris);
        audioSource.GetSpectrumData(spectrumRight, 1, FFTWindow.BlackmanHarris);

        // 2) Prépare un buffer "utilisé" selon l'option sélectionnée
        float[] spectrumUsed = new float[sampleSize];
        if (channelOption == ChannelOption.Left)
        {
            spectrumUsed = spectrumLeft;
        }
        else if (channelOption == ChannelOption.Right)
        {
            spectrumUsed = spectrumRight;
        }
        else // Mono
        {
            for (int i = 0; i < sampleSize; i++)
                spectrumUsed[i] = (spectrumLeft[i] + spectrumRight[i]) * 0.5f;
        }

        // 3) Détermine l'intervalle de fréquences selon la bande choisie
        float fMin = 0f, fMax = 0f;
        switch (bandToMeasure)
        {
            case Band.SubBass:
                fMin = 20f;    fMax = 62f;    break;
            case Band.Bass:
                fMin = 62f;    fMax = 250f;   break;
            case Band.LowMid:
                fMin = 250f;   fMax = 500f;   break;
            case Band.Mid:
                fMin = 500f;   fMax = 2000f;  break;
            case Band.High:
                fMin = 2000f;  fMax = 20000f; break;
        }

        int iMin = Mathf.FloorToInt(fMin / binWidth);
        int iMax = Mathf.Min(spectrumUsed.Length - 1, Mathf.CeilToInt(fMax / binWidth));

        // 4) Somme de l'énergie dans la bande (plus stable que la moyenne sur larges plages)
        float bandPower = 0f;
        for (int i = iMin; i <= iMax; i++)
            bandPower += spectrumUsed[i];

        // 5) Applique le multiplicateur et le lissage
        float amplified = bandPower * amplitudeMultiplier;
        currentLevel = Mathf.Lerp(currentLevel, amplified, 1f - Mathf.Pow(smoothSpeed, Time.deltaTime));

        // 6) Met à jour le slider
        if (volumeSlider != null)
            volumeSlider.value = currentLevel;
    }
}
