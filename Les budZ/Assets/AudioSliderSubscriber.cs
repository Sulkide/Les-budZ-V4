using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AudioSliderSubscriber : MonoBehaviour
{
    [Header("Bande de fréquences")]
    public AudioSliderMeter.Band band = AudioSliderMeter.Band.Bass;

    [Header("Canal audio")]
    public AudioSliderMeter.ChannelOption channel = AudioSliderMeter.ChannelOption.Mono;

    [Header("Réglages")]
    [Tooltip("Multiplicateur de l'amplitude")]
    public float multiplier = 1f;
    [Tooltip("Inertie du lissage (0 = pas de lissage, 1 = très lent)")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.2f;

    private Slider _slider;
    private AudioSpectrumManager.Subscriber _subscription;

    void Start()
    {
        _slider = GetComponent<Slider>();

        // Crée la subscription et ajoute-la au manager
        _subscription = new AudioSpectrumManager.Subscriber
        {
            slider      = _slider,
            band        = band,
            channel     = channel,
            multiplier  = multiplier,
            smoothSpeed = smoothSpeed,
            currentLevel = 0f
        };

        if (AudioSpectrumManager.Instance != null)
            AudioSpectrumManager.Instance.subscribers.Add(_subscription);
        else
            Debug.LogError("[AudioSliderSubscriber] AudioSpectrumManager introuvable dans la scène.");
    }

    void OnDisable()
    {
        // Désinscription si on désactive/détruit le GameObject
        if (_subscription != null && AudioSpectrumManager.Instance != null)
            AudioSpectrumManager.Instance.subscribers.Remove(_subscription);
    }
}