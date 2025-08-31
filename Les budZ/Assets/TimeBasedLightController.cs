using UnityEngine;
using System;

public class TimeBasedLightController : MonoBehaviour
{
    [Header("Référence lumière")]
    public Light directionalLight;           // assigner la Directional Light ici

    [Header("Profils horaires")]
    public TimeLightProfile[] timeProfiles;  // liste des profils de plages horaires

    [Header("Source de l'heure")]
    public bool useRealTime = true;          // si true, prend GameManager.instance.realTime, sinon fixedTime
    [Tooltip("Format HH:mm:ss, utilisé si useRealTime est false")]
    public string fixedTime = "12:00:00";    // heure de substitution pour tests

    void Update()
    {
        if (!Application.isPlaying) return; // sécurité : ne fonctionne qu'en Play Mode

        // 1. Obtenir l'heure active (réelle ou fixe)
        string currentTimeStr;
        if (useRealTime)
        {
            currentTimeStr = GameManager.instance.realTime;
        }
        else
        {
            currentTimeStr = fixedTime;
        }

        // Tentative de parsing, avec fallback silencieux
        if (!TimeSpan.TryParse(currentTimeStr, out TimeSpan currentTime))
        {
            Debug.LogWarning($"[TimeBasedLightController] Impossible de parser l'heure '{currentTimeStr}'. Format attendu HH:mm:ss.");
            return;
        }

        double currentSec = currentTime.TotalSeconds;

        // 2. Chercher et appliquer le profil actif
        foreach (TimeLightProfile profile in timeProfiles)
        {
            if (!TimeSpan.TryParse(profile.startTime, out TimeSpan start)) continue;
            if (!TimeSpan.TryParse(profile.endTime, out TimeSpan end)) continue;

            double startSec = start.TotalSeconds;
            double endSec = end.TotalSeconds;

            // Cas standard : start < end (dans la même journée)
            if (currentSec >= startSec && currentSec < endSec)
            {
                ApplyProfile(profile, (float)((currentSec - startSec) / (endSec - startSec)));
                return;
            }

            // Cas où la plage passe par minuit (ex : 23:00 -> 02:00)
            if (endSec < startSec)
            {
                // On considère deux segments : [start, 24h) et [0, end)
                if (currentSec >= startSec || currentSec < endSec)
                {
                    // Calcul de t en tenant compte du wrap-around
                    double duration = (24 * 3600 - startSec) + endSec;
                    double elapsed;
                    if (currentSec >= startSec)
                        elapsed = currentSec - startSec;
                    else
                        elapsed = (24 * 3600 - startSec) + currentSec;

                    ApplyProfile(profile, (float)(elapsed / duration));
                    return;
                }
            }
        }
    }

    private void ApplyProfile(TimeLightProfile profile, float t)
    {
        t = Mathf.Clamp01(t);

        // Rotation (Slerp pour robustesse)
        Quaternion rotA = Quaternion.Euler(profile.startRotation);
        Quaternion rotB = Quaternion.Euler(profile.endRotation);
        directionalLight.transform.rotation = Quaternion.Slerp(rotA, rotB, t);

        // Couleur
        directionalLight.color = Color.Lerp(profile.startColor, profile.endColor, t);
    }
}
