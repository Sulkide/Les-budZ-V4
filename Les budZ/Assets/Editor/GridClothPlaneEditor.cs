#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridClothPlane))]
public class GridClothPlaneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var comp = (GridClothPlane)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Mesh"))
        {
            Undo.RegisterCompleteObjectUndo(comp.gameObject, "Generate Cloth Grid");
            comp.Generate();
            EditorUtility.SetDirty(comp);
        }
    }
}
#endif