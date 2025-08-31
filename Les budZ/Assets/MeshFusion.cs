using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using System.Linq;
#endif

[DisallowMultipleComponent]
public class MeshFusion : MonoBehaviour
{
    [Header("Matériau")]
    public Material overrideMaterial;

    [Header("Options")]
    public bool includeInactive = false;

#if UNITY_EDITOR
    [Header("Sauvegarde du mesh")]
    [Tooltip("En Prefab Mode : enregistre le mesh combiné comme sous-asset du prefab.\nHors Prefab Mode : crée un asset dans le projet.")]
    public bool saveAsAsset = true;

    [Tooltip("Dossier de sortie si hors Prefab Mode.")]
    public string projectFolderForSceneMeshes = "Assets/GeneratedMeshes";

    [ContextMenu("Fusionner (depuis menu contextuel)")]
    public void DoFusionContextMenu() => DoFusion();

    public void DoFusion()
    {
        var root = transform;

        // Récupérer tous les MeshFilter descendants
        var filters = root.GetComponentsInChildren<MeshFilter>(includeInactive)
                          .Where(f => f && f.sharedMesh && f.transform != root)
                          .ToList();
        if (filters.Count == 0)
        {
            Debug.LogWarning($"[MeshFusion] Aucun mesh valide à fusionner sous {name}");
            return;
        }

        // Déterminer le matériau
        Material targetMat = overrideMaterial;
        if (targetMat == null)
        {
            foreach (var mf in filters)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr && mr.sharedMaterial)
                {
                    targetMat = mr.sharedMaterial;
                    break;
                }
            }
        }

        // Préparer les CombineInstance vers l'espace local du root
        var combines = new List<CombineInstance>(filters.Count);
        foreach (var mf in filters)
        {
            var ci = new CombineInstance
            {
                mesh = mf.sharedMesh,
                transform = root.worldToLocalMatrix * mf.transform.localToWorldMatrix,
                subMeshIndex = 0
            };
            combines.Add(ci);
        }

        if (combines.Count == 0)
        {
            Debug.LogWarning("[MeshFusion] Aucun mesh combinable détecté.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Fusion Meshes");

        var dstMF = root.GetComponent<MeshFilter>();
        var dstMR = root.GetComponent<MeshRenderer>();
        if (!dstMF) dstMF = Undo.AddComponent<MeshFilter>(root.gameObject);
        if (!dstMR) dstMR = Undo.AddComponent<MeshRenderer>(root.gameObject);

        // Créer le mesh combiné
        var combined = new Mesh
        {
            name = $"{name}_CombinedMesh",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        combined.CombineMeshes(combines.ToArray(), true, true, false);
        combined.RecalculateBounds();

        // --- NOUVEAU : Sauvegarde en asset pour persistance ---
        if (saveAsAsset)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                // En Prefab Mode : sauver en SOUS-ASSET du prefab
                SaveMeshAsSubAssetOfPrefab(combined, stage.prefabContentsRoot);
            }
            else
            {
                // En scène : créer un asset dans le projet
                SaveMeshAsProjectAsset(combined, projectFolderForSceneMeshes);
            }
        }

        dstMF.sharedMesh = combined; // référence l’asset (ou l’objet mémoire si saveAsAsset=false)
        if (targetMat) dstMR.sharedMaterial = targetMat;

        // Supprimer tous les enfants
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
        }

        // Marquer dirty
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            EditorSceneManager.MarkSceneDirty(prefabStage.scene);
        else
            EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Debug.Log($"[MeshFusion] Fusion OK sur \"{name}\". Verts: {dstMF.sharedMesh.vertexCount}");
    }

    private static void SaveMeshAsSubAssetOfPrefab(Mesh mesh, GameObject prefabRoot)
    {
        // Nettoyer un éventuel ancien sous-asset de même nom pour éviter les doublons
        var assetPath = PrefabStageUtility.GetCurrentPrefabStage().assetPath;
        var mainObj = AssetDatabase.LoadMainAssetAtPath(assetPath);
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        foreach (var sa in subAssets)
        {
            if (sa is Mesh m && m.name == mesh.name)
            {
                Object.DestroyImmediate(m, allowDestroyingAssets: true);
                break;
            }
        }

        mesh.hideFlags = HideFlags.None;
        AssetDatabase.AddObjectToAsset(mesh, mainObj);
        EditorUtility.SetDirty(mainObj);
        AssetDatabase.SaveAssets();
    }

    private static void SaveMeshAsProjectAsset(Mesh mesh, string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            // Crée la hiérarchie de dossiers si besoin
            var parts = folder.Trim('/').Split('/');
            string current = parts[0];
            if (!AssetDatabase.IsValidFolder(current)) return; // doit commencer par "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mesh.name}.asset");
        mesh.hideFlags = HideFlags.None;
        AssetDatabase.CreateAsset(mesh, path);
        var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(MeshFusion))]
public class MeshFusionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fusionner", GUILayout.Height(32), GUILayout.MinWidth(140)))
            {
                ((MeshFusion)target).DoFusion();
            }
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.HelpBox(
            "Astuce : Active 'Sauvegarde du mesh' pour que le mesh combiné soit conservé.\n" +
            "• En Prefab Mode : le mesh est stocké comme sous-asset du prefab.\n" +
            "• Hors Prefab Mode : un asset est créé dans le dossier indiqué.",
            MessageType.Info);
    }
}
#endif
