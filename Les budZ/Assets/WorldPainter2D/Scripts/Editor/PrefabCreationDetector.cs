using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WorldPainter2D
{
    public class PrefabCreationDetector : AssetModificationProcessor
    {
        static void OnWillCreateAsset(string assetName)
        {
            if (assetName.EndsWith(".prefab"))
            {
                EditorApplication.delayCall += () =>
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetName);
                    if (prefab != null)
                    {
                        var instances = PrefabUtility.FindAllInstancesOfPrefab(prefab);
                        if (instances.Length > 0)
                        {
                            var instance = instances[0];
                            AddMeshInfoRecursive(prefab, instance, prefab);
                        }
                    }
                };                
            }
        }

        private static void AddMeshInfoRecursive(GameObject prefabAssetRoot, GameObject instanceObj, GameObject prefabObj)
        {
            var shape = prefabObj.GetComponent<WorldGenShape>();
            if (shape != null)
            {
                var instanceMF = instanceObj.GetComponent<MeshFilter>();
                var parentMF = prefabObj.GetComponent<MeshFilter>();
                var mesh = CloneMesh(instanceMF.sharedMesh);

                AssetDatabase.AddObjectToAsset(mesh, prefabAssetRoot);
                parentMF.mesh = mesh;                
            }
            for (int i = 0; i < instanceObj.transform.childCount; i++)
            {
                AddMeshInfoRecursive(prefabAssetRoot, instanceObj.transform.GetChild(i).gameObject, prefabObj.transform.GetChild(i).gameObject);
            }
        }

        static Mesh CloneMesh(Mesh origMesh)
        {
            if (origMesh == null) 
                return null;
            Mesh mesh = new Mesh();

            mesh.name = origMesh.name;
            mesh.vertices = origMesh.vertices;
            mesh.triangles = origMesh.triangles;
            mesh.normals = origMesh.normals;
            mesh.uv = origMesh.uv;
            mesh.colors = origMesh.colors;

            return mesh;
        }
    }
}
