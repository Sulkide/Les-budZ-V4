using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Bumper : MonoBehaviour
{
    [Header("Paramètres de déplacement")]
    [Tooltip("Point final vers lequel l'objet sera projeté")]
    public Transform targetPoint;

    [Tooltip("Hauteur maximale de l'arc")]
    public float amplitude = 2f;

    [Tooltip("Multiplicateur d'amplitude lorsque le joueur dash")]
    public float dashAmplitudeMultiplier = 1.2f;

    [Tooltip("Durée totale de l'animation (en secondes)")]
    public float duration = 1f;

    [Tooltip("Multiplicateur de durée lorsque le joueur dash")]
    public float dashDurationMultiplier = 1.2f;

    [Header("Paramètres de détection")]
    [Tooltip("Rayon du cercle de détection")]
    public float detectionRadius = 1f;

    [Tooltip("Layers des objets à détecter")]
    public LayerMask detectLayers;

    List<string> clipsRandomBump = new List<string> { "bumper" };
    
    // Pour ne pas lancer plusieurs coroutines simultanément
    private bool isMoving = false;

    private void Update()
    {
        if (isMoving) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectLayers);
        if (hits.Length > 0)
        {
            SoundManager.Instance.PlayRandomSFX(clipsRandomBump, 1, 1.2f);
            Transform obj = hits[0].transform;
            StartBump(obj);
            StartCoroutine(MoveAlongArc(obj));
        }
    }
    
    


    private void StartBump(Transform obj)
    {
        isMoving = true;

        bool hasPM = obj.TryGetComponent<PlayerMovement>(out var pm);
        float finalAmplitude = amplitude;
        float finalDuration  = duration;
        if (hasPM && pm.isDashing)
        {
            finalAmplitude *= dashAmplitudeMultiplier;
            finalDuration  *= dashDurationMultiplier;
            pm.Rotate360Z(finalDuration);
        }

        Vector3 endPos = targetPoint.position;

        // Séquence DOTween pour le jump de l'objet projeté + shake du bumper
        Sequence seq = DOTween.Sequence();

        // 1) Jump de l'objet projeté
        seq.Append(obj
            .DOJump(endPos,
                finalAmplitude,
                1,
                finalDuration)
            .SetEase(Ease.OutQuad));

        // 2) ShakeScale du bumper en parallèle
        seq.Join(transform
            .DOShakeScale(
                duration: finalDuration * 0.5f,          // durée du shake
                strength: new Vector3(1.2f, 0.15f, 0f), // amplitude du shake
                vibrato: 12,                              // nombre d'oscillations
                randomness: 5f,                           // pas de randomness
                fadeOut: true                             // atténuation en fin de shake
            )
            .SetEase(Ease.InOutQuad));

        // 3) À la fin, on s’assure de bien remettre l'état
        seq.OnComplete(() =>
        {
            obj.position = endPos;
            isMoving = false;
        });
    }



    private IEnumerator MoveAlongArc(Transform obj)
    {
        isMoving = true;

        // On récupère le composant PlayerMovement, s'il existe
        bool hasPM = obj.TryGetComponent<PlayerMovement>(out var pm);

        // Calcul de l'amplitude et de la durée finales
        float finalAmplitude = amplitude;
        float finalDuration  = duration;
        if (hasPM && pm.isDashing)
        {
            finalAmplitude *= dashAmplitudeMultiplier;
            finalDuration  *= dashDurationMultiplier;
        }

        // Appel de la rotation + shake, si PlayerMovement présent
        if (hasPM)
        {
            pm.Rotate360Z(finalDuration);
        }

        Vector3 startPos = obj.position;
        Vector3 endPos   = targetPoint.position;
        float elapsed    = 0f;

        while (elapsed < finalDuration)
        {
            float t = elapsed / finalDuration;

            // Position linéaire entre start et end
            Vector3 basePos = Vector3.Lerp(startPos, endPos, t);

            // Décalage vertical en sinusoïde pour l'arc en cloche
            float heightOffset = Mathf.Sin(Mathf.PI * t) * finalAmplitude;
            obj.position = basePos + Vector3.up * heightOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Position finale exacte
        obj.position = endPos;
        isMoving = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
