using UnityEngine;
using System;
using UnityEngine.Events;

[Serializable]
public class TimeWindow
{
    [Tooltip("Heure de début (HH:mm:ss)")]
    public string startTime;
    [Tooltip("Heure de fin (HH:mm:ss)")]
    public string endTime;

    [Tooltip("Si vrai : active la cible pendant la fenêtre, sinon la désactive pendant la fenêtre")]
    public bool enableDuringWindow = true;
}

public class TimeWindowActivator : MonoBehaviour
{
    [Header("Source de l'heure")]
    public bool useRealTime = true;
    [Tooltip("Format HH:mm:ss si useRealTime == false")]
    public string fixedTime = "12:00:00";

    [Header("Fenêtres temporelles")]
    public TimeWindow[] windows;

    [Header("Cibles à toggler")]
    public GameObject[] gameObjectsToToggle;
    public Behaviour[] behavioursToToggle; // ex: scripts, components

    [Header("Événements optionnels")]
    public UnityEvent onActivate;
    public UnityEvent onDeactivate;

    // État interne pour éviter de ré-appeler plusieurs fois inutilement
    private bool lastActiveState;

    void Start()
    {
        // Initialisation de l'état pour forcer un premier update effectif
        lastActiveState = !GetComputedActiveState();
        ApplyState(lastActiveState); // forcé pour que Update() voit un changement
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        bool shouldBeActive = GetComputedActiveState();

        if (shouldBeActive != lastActiveState)
        {
            ApplyState(shouldBeActive);
            lastActiveState = shouldBeActive;
        }
    }

    private bool GetComputedActiveState()
    {
        // Récupérer l'heure courante
        string currentTimeStr = useRealTime ? GameManager.instance.realTime : fixedTime;

        if (!TimeSpan.TryParse(currentTimeStr, out TimeSpan currentTime))
        {
            Debug.LogWarning($"[TimeWindowActivator] Impossible de parser l'heure '{currentTimeStr}'. Format attendu HH:mm:ss.");
            return false;
        }

        double currentSec = currentTime.TotalSeconds;

        // On parcourt les fenêtres, la première correspondante détermine l'état
        foreach (var window in windows)
        {
            if (!TimeSpan.TryParse(window.startTime, out TimeSpan start)) continue;
            if (!TimeSpan.TryParse(window.endTime, out TimeSpan end)) continue;

            double startSec = start.TotalSeconds;
            double endSec = end.TotalSeconds;

            bool inWindow = false;

            if (startSec <= endSec)
            {
                // fenêtre normale (ex : 08:00 -> 12:00)
                inWindow = currentSec >= startSec && currentSec < endSec;
            }
            else
            {
                // fenêtre qui traverse minuit (ex : 22:00 -> 03:00)
                inWindow = currentSec >= startSec || currentSec < endSec;
            }

            if (inWindow)
            {
                return window.enableDuringWindow;
            }
        }

        // Si aucune fenêtre ne matche, par défaut : inverse de enableDuringWindow de la première fenêtre si elle existe, sinon false
        if (windows != null && windows.Length > 0)
        {
            return !windows[0].enableDuringWindow;
        }

        return false;
    }

    private void ApplyState(bool active)
    {
        // Toggle GameObjects
        foreach (var go in gameObjectsToToggle)
        {
            if (go != null) go.SetActive(active);
        }

        // Toggle Behaviours (scripts/components)
        foreach (var beh in behavioursToToggle)
        {
            if (beh != null) beh.enabled = active;
        }

        if (active)
            onActivate?.Invoke();
        else
            onDeactivate?.Invoke();
    }
}
