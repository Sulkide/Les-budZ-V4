using UnityEngine;
using UnityEditor;
using System;

namespace WorldPainter2D
{
    [CustomEditor(typeof(MeshColorPicker))]
    public class MeshColorPickerEditor : Editor
    {
        Color prevColor;
        public override void OnInspectorGUI()
        {
            MeshColorPicker myScript = (MeshColorPicker)target;
            prevColor = myScript.color;
            myScript.color = EditorGUILayout.ColorField("Fill color", myScript.color);

            if (prevColor != myScript.color)
            {
                myScript.SetColor();
            }
        }
    }
}