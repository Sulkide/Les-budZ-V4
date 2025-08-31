using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Custom/Spawn Prefab On Texture")]
public class SpawnPrefabOnTexture : MonoBehaviour
{
    [Tooltip("Nom de la texture à rechercher")]
    public string targetTextureName;

    [Tooltip("Prefab à instancier sur chaque objet correspondant")]
    public GameObject prefabToSpawn;

    // Liste interne des objets enfants correspondants
    private List<Transform> matchingChildren = new List<Transform>();

    // Appeler cette méthode pour lancer le processus via code
    public void SpawnOnTexture(string textureName)
    {
        FillMatchingChildren(textureName);
        InstantiatePrefabs();
    }

    // Exemple : si vous voulez que ça se déclenche automatiquement au démarrage
    private void Start()
    {
        if (!string.IsNullOrEmpty(targetTextureName) && prefabToSpawn != null)
        {
            SpawnOnTexture(targetTextureName);
        }
    }

    // Parcourt tous les enfants, récupère ceux ayant Terrain2DBlock avec Texture correspondante
    private void FillMatchingChildren(string textureName)
    {
        matchingChildren.Clear();
        foreach (Transform child in transform)
        {
            Terrain2DBlock block = child.GetComponent<Terrain2DBlock>();
            if (block == null)
                continue;

            // On suppose que block.Texture est de type Texture2D ou string
            string texName = null;
            // Si c'est une Texture2D
            var textureObj = block.texture as Texture;
            if (textureObj != null)
            {
                texName = textureObj.name;
            }
            else if (block.texture is string)
            {
                texName = (string)(object)block.texture;
            }

            if (texName == textureName)
            {
                matchingChildren.Add(child);
            }
        }
    }

    // Instancie le prefab sur chaque enfant trouvé
    private void InstantiatePrefabs()
    {
        foreach (var t in matchingChildren)
        {
            // Instanciation au même emplacement et rotation, en tant qu'enfant
            Instantiate(prefabToSpawn, t.localPosition, t.localRotation, t);
        }
    }
}
