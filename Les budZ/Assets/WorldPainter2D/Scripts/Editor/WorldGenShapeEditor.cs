using UnityEngine;
using UnityEditor;

namespace WorldPainter2D
{
    [CustomEditor(typeof(WorldGenShape))]
    public class WorldGenShapeEditor : Editor
    {
        WorldGenShape script;
        SnapGrid snapGrid;
        public override void OnInspectorGUI()
        {
            if (script == null)
                script = (WorldGenShape)target;

            snapGrid = SnapGrid.Instance;
            snapGrid.gridRadius = EditorGUILayout.Slider("Grid radius", snapGrid.gridRadius, 0.2f, 25);
            snapGrid.gridGranularity = EditorGUILayout.Slider("Grid granularity", snapGrid.gridGranularity, 0.05f, 5);
            var meshVerts = script.Verts;
            if (meshVerts != null)
                snapGrid.vertexIdx = EditorGUILayout.IntSlider("Vertex to snap", snapGrid.vertexIdx, 0, meshVerts.Length - 1);

            if (GUI.changed)
                SceneView.RepaintAll();

            if (Mathf.Pow(snapGrid.gridRadius / snapGrid.gridGranularity, 2) > 200)
            {
                EditorGUILayout.HelpBox("Grid with too many dots! Try reducing the grid radius or increasing the granularity", MessageType.Warning);
            }

            string msg;
            if (SnapGrid.Instance.snapping)
            {
                GUI.backgroundColor = new Color(0.5f, 1, 0);
                msg = "Disable";
            }
            else
                msg = "Enable";
                
            if (GUILayout.Button(msg + " Grid Snapping", GUILayout.Height(20)))
            {
                snapGrid.StartStopSnapToGrid();
                SceneView.RepaintAll();
            }            
        }
    }
}