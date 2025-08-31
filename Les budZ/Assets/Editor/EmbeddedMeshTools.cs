#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class EmbeddedMeshTools
{
    [MenuItem("Tools/Scene/Report Embedded Meshes")]
    public static void Report()
    {
        var mfs = Object.FindObjectsOfType<MeshFilter>(true);
        long total = 0;
        foreach (var mf in mfs)
        {
            var mesh = mf.sharedMesh;
            if (!mesh) continue;

            // Chemin vide => mesh sérialisé dans la scène (pas un asset .asset)
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path))
            {
                int tris  = mesh.triangles.Length / 3;
                int verts = mesh.vertexCount;
                long bytes = (long)mesh.triangles.Length * 2 + (long)verts * 12; // estimation grossière
                total += bytes;
                Debug.Log($"Embedded mesh: {mf.gameObject.name} | verts:{verts} tris:{tris} ~{bytes/1048576f:F1} MB", mf);
            }
        }
        Debug.Log($"TOTAL embedded ~{total/1048576f:F1} MB (estimation).");
    }

    [MenuItem("Tools/Scene/Externalize Embedded Meshes")]
    public static void Externalize()
    {
        const string dir = "Assets/GeneratedMeshes";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        int count = 0;
        var mfs = Object.FindObjectsOfType<MeshFilter>(true);
        foreach (var mf in mfs)
        {
            var mesh = mf.sharedMesh;
            if (!mesh) continue;

            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path)) // embedded
            {
                var copy = Object.Instantiate(mesh);
                copy.name = mesh.name.Replace("(Instance)", "").Replace("(Clone)", "") + "_GEN";
                var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{copy.name}.asset");
                AssetDatabase.CreateAsset(copy, assetPath);
                mf.sharedMesh = copy; // on relie le MeshFilter à l’asset externe
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"Externalized {count} embedded meshes → {dir}");
    }
}
#endif
