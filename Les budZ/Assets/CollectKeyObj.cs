using System.Collections;
using UnityEngine;

/// <summary>
/// Ramasse un objet clé défini par un ScriptableObject KeyObjData.
/// - Ajoute l'item à l'inventaire via GameManager.AddKeyObject.
/// - Affiche un bandeau (KeyPickupBanner.Show) puis détruit le GO.
/// - Empêche les doublons grâce à GameManager.HasKeyItem(id).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CollectKeyObj : MonoBehaviour
{
    [Header("Donnée (ScriptableObject)")]
    [SerializeField] private KeyObjData item;          // assigne un asset KeyObjData dans l’inspecteur

    [Header("Déclencheur")]
    [SerializeField] private string playerTag = "Target"; // tes joueurs portent ce tag
    [SerializeField] private bool requireUseInput = true; // attend "Use" du joueur (via PlayerMovement.useInputRegistered)

    [Header("Feedback")]
    [SerializeField] private AudioClip pickupSfx;      // optionnel
    [SerializeField] private float bannerSeconds = 7f; // durée d’affichage du bandeau

    private bool _collected;

    private void Reset()
    {
        // sécurise le collider en trigger
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_collected) return;
        if (!other.CompareTag(playerTag)) return;

        // Si on exige un appui "Use", lit le flag du PlayerMovement
        if (requireUseInput)
        {
            var pm = other.GetComponent<PlayerMovement>();
            if (pm == null || !pm.useInputRegistered) return;
        }

        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        _collected = true;

        // sécurité : pas de data -> pas de crash
        if (item == null)
        {
            Debug.LogWarning("[CollectKeyObj] Aucun KeyObjData assigné sur " + name);
            Destroy(gameObject);
            yield break;
        }

        // Empêche les doublons si déjà possédé
        var gm = GameManager.instance;
        if (gm != null && gm.HasKeyItem(item.id))
        {
            // On peut tout de même montrer un petit bandeau “déjà obtenu” si tu veux
            KeyPickupBanner.Show(item.icon, item.displayName, "<color=#cccccc>Déjà obtenu</color>", 2f, titleSize: 15, descSize: 10);
            Destroy(gameObject);
            yield break;
        }

        // Ajoute à l’inventaire
        gm?.AddKeyObject(item);

        // Feedback : son
        if (pickupSfx)
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position);

        // Cache les visuels/physiques du pickup immédiatement (évite tout résidu)
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.enabled = false;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        // Bandeau autonome (il se détruira tout seul)
        string title = string.IsNullOrEmpty(item.displayName) ? "Objet clé obtenu" : item.displayName;
        string desc  = string.IsNullOrEmpty(item.description) ? "" : item.description;
        KeyPickupBanner.Show(item.icon, title, desc, bannerSeconds, titleSize: 15, descSize: 10);

        // Laisse un frame pour éviter de détruire au même tick que l’UI
        yield return null;

        // Détruit définitivement le pickup
        Destroy(gameObject);
    }
}
