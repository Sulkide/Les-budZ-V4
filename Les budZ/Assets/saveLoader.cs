// SaveLoader.cs
using UnityEngine;
using System.IO;

public static class SaveLoader
{
    /// <summary>
    /// Lit et désérialise le JSON du slot donné, renvoie un GameData ou null si introuvable.
    /// </summary>
    public static GameManager.GameData LoadGameData(int slot)
    {
        string path = Path.Combine(Application.persistentDataPath, $"slot{slot}.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveLoader] Aucun fichier de sauvegarde en '{path}'");
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<GameManager.GameData>(json);
    }
}