using UnityEngine;

public class MeshToSpriteClone : MonoBehaviour
{
    public Vector3 localOffset = Vector3.zero;

    void Start()
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        var meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null || meshFilter == null)
        {
            Debug.LogWarning("Aucun MeshRenderer ou MeshFilter trouvé !");
            return;
        }

        // Essaye d'extraire la texture
        var material = meshRenderer.sharedMaterial;
        var texture = material?.mainTexture as Texture2D;
        if (texture == null)
        {
            Debug.LogWarning("Pas de texture valide trouvée dans le material.");
            return;
        }

        // Crée un sprite à partir de la texture
        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f  // pixels per unit, à ajuster
        );

        // Crée le clone enfant
        GameObject clone = new GameObject("SpriteClone");
        clone.transform.SetParent(transform);
        clone.transform.localPosition = localOffset;
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale = Vector3.one;

        var sr = clone.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder =  meshRenderer.sortingOrder + 1;
        sr.color = Color.white;
    }
}