using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AudioSpectrumManager : MonoBehaviour
{
    public static AudioSpectrumManager Instance { get; private set; }

    [Header("FFT settings")]
    [Tooltip("Puissance de 2, ex 1024.")]
    public int sampleSize = 1024;

    private float[] spectrumLeft;
    private float[] spectrumRight;
    private float binWidth;
    private AudioSource audioSource;

    // Un abonné : quel slider, quelle bande, quel canal, etc.
    [System.Serializable]
    public class Subscriber
    {
        public UnityEngine.UI.Slider slider;
        public AudioSliderMeter.Band band;
        public AudioSliderMeter.ChannelOption channel;
        public float multiplier = 1f;
        [Range(0f,1f)] public float smoothSpeed = 0.2f;

        [HideInInspector] public float currentLevel = 0f;
    }

    [Header("Subscribers")]
    public List<Subscriber> subscribers = new List<Subscriber>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        audioSource = GetComponent<AudioSource>();
        spectrumLeft  = new float[sampleSize];
        spectrumRight = new float[sampleSize];
        binWidth = (AudioSettings.outputSampleRate / 2f) / sampleSize;
    }

    void Update()
    {
        // 1) FFT une seule fois
        audioSource.GetSpectrumData(spectrumLeft,  0, FFTWindow.BlackmanHarris);
        audioSource.GetSpectrumData(spectrumRight, 1, FFTWindow.BlackmanHarris);

        // 2) Pour chaque abonné, on calcule et on met à jour
        foreach (var sub in subscribers)
        {
            // Choix du buffer selon le canal
            float[] spec = sub.channel == AudioSliderMeter.ChannelOption.Left
                ? spectrumLeft
                : sub.channel == AudioSliderMeter.ChannelOption.Right
                    ? spectrumRight
                    : null;
            // En mono, on fait un mix on-the-fly sans créer de tableau
            bool isMono = (sub.channel == AudioSliderMeter.ChannelOption.Mono);

            // Définition de la bande
            float fMin=0, fMax=0;
            switch (sub.band)
            {
                case AudioSliderMeter.Band.SubBass:  fMin=20;    fMax=62;    break;
                case AudioSliderMeter.Band.Bass:     fMin=62;    fMax=250;   break;
                case AudioSliderMeter.Band.LowMid:   fMin=250;   fMax=500;   break;
                case AudioSliderMeter.Band.Mid:      fMin=500;   fMax=2000;  break;
                case AudioSliderMeter.Band.High:     fMin=2000;  fMax=20000; break;
            }

            int iMin = Mathf.FloorToInt(fMin/binWidth);
            int iMax = Mathf.Min(sampleSize-1, Mathf.CeilToInt(fMax/binWidth));

            // Somme de l'énergie
            float bandPower = 0f;
            for (int i = iMin; i <= iMax; i++)
            {
                if (isMono)
                    bandPower += (spectrumLeft[i] + spectrumRight[i]) * 0.5f;
                else
                    bandPower += spec[i];
            }

            // Multipl. + lissage
            float amplified = bandPower * sub.multiplier;
            sub.currentLevel = Mathf.Lerp(sub.currentLevel, amplified,
                                          1f - Mathf.Pow(sub.smoothSpeed, Time.deltaTime));

            // Update slider
            if (sub.slider != null)
                sub.slider.value = sub.currentLevel;
        }
    }
}
