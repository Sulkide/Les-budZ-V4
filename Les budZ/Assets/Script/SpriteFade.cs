using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpriteFade : MonoBehaviour
{
    [Tooltip("Durée de la transition en secondes.")]
    public float fadeDuration = 1f; 

    [Tooltip("Layer du joueur (Player)")]
    public LayerMask playerLayer; 

    private SpriteRenderer spriteRenderer;
    private Coroutine currentCoroutine;

    // On stocke les colliders des joueurs présents pour éviter les doublons
    private HashSet<Collider2D> playersInZone = new HashSet<Collider2D>();

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // On s'assure d'être complètement opaque au départ
        Color baseColor = spriteRenderer.color;
        baseColor.a = 1f;
        spriteRenderer.color = baseColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Si c'est un nouveau joueur, on l'ajoute et on fade out si c'est le premier
        if (playersInZone.Add(other) && playersInZone.Count == 1)
        {
            StartFade(0f);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        // On enlève le joueur : si plus personne, on fade in
        if (playersInZone.Remove(other) && playersInZone.Count == 0)
        {
            StartFade(1f);
        }
    }

    // Lance proprement la coroutine de fade en stoppant l'ancienne
    private void StartFade(float targetAlpha)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(FadeTo(targetAlpha));
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = spriteRenderer.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            Color c = spriteRenderer.color;
            c.a = newAlpha;
            spriteRenderer.color = c;
            yield return null;
        }

        // On corrige pour être sûr d'atteindre exactement la valeur
        Color finalColor = spriteRenderer.color;
        finalColor.a = targetAlpha;
        spriteRenderer.color = finalColor;
        currentCoroutine = null;
    }
}
