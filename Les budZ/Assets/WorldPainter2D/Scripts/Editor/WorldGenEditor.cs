using UnityEngine;
using UnityEditor;
using System;

namespace WorldPainter2D
{
    [CustomEditor(typeof(WorldGenerator2D))]
    public class WorldGenEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            string msg = "NOTE:" + Environment.NewLine +
                         " - This gameobject is used by the WorldPainter2D tool. It will be automatically deleted once you close the tool window.";

            EditorGUILayout.HelpBox(msg, MessageType.Warning);
        }
    }
}