using UnityEngine;
using UnityEditor;

namespace WorldPainter2D
{
    public class WorldGenWindow : EditorWindow
    {
        // GUI Variables
        private bool mainSettings = true;
        private bool brushOptions = true;
        private bool drawingOptions = true;
        private bool shapeOptions = true;
        private bool instanceOptions = true;
        Vector2 scrollPos;

        // WG2D
        WorldGenerator2D worldGenScript;

        [MenuItem("Window/2D/World Painter 2D")]
        public static void ShowWindow()
        {
            GetWindow<WorldGenWindow>("World Painter 2D").Show();
        }

        private void OnDestroy()
        {
            foreach (var wg2d in FindObjectsOfType<WorldGenerator2D>(true))
            {
                DestroyImmediate(wg2d.gameObject);
            }
        }


        private void OnGUI()
        {
            if (worldGenScript == null)
            {
                worldGenScript = WorldGenerator2D.Instance;
                if (!worldGenScript.loadedConfig)
                    worldGenScript.LoadConfig();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space(10);

            // ######### MAIN SETTINGS #########

            mainSettings = EditorGUILayout.Foldout(mainSettings, "General Settings");
            if (mainSettings)
            {
                EditorGUI.indentLevel++;
                worldGenScript.autosaveConfig = EditorGUILayout.Toggle("Autosave Settings", worldGenScript.autosaveConfig);
                if(worldGenScript.prevAutoSaveConfig != worldGenScript.autosaveConfig)
                {
                    worldGenScript.prevAutoSaveConfig = worldGenScript.autosaveConfig;
                    worldGenScript.SaveConfig();
                }
                if (worldGenScript.autosaveConfig)
                    GUI.enabled = false;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Settings"))
                {
                    worldGenScript.SaveConfig();
                }
                if (GUILayout.Button("Load Settings"))
                {
                    worldGenScript.LoadConfig();
                }
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Canvas Settings", EditorStyles.boldLabel);
                worldGenScript.canvasLayer = EditorGUILayout.LayerField("Canvas layer", worldGenScript.canvasLayer);
                worldGenScript.canvasColor = EditorGUILayout.ColorField("Canvas color", worldGenScript.canvasColor);
                worldGenScript.gridColor = EditorGUILayout.ColorField("Grid color", worldGenScript.gridColor);
                worldGenScript.gridCenterColor = EditorGUILayout.ColorField("Grid center color", worldGenScript.gridCenterColor);

                EditorGUILayout.LabelField("Gameobject's Parents", EditorStyles.boldLabel);
                worldGenScript.shapeParent = (Transform)EditorGUILayout.ObjectField("Shapes' parent", worldGenScript.shapeParent, typeof(Transform), true);
                worldGenScript.prefabParent = (Transform)EditorGUILayout.ObjectField("Prefabs' parent", worldGenScript.prefabParent, typeof(Transform), true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            brushOptions = EditorGUILayout.Foldout(brushOptions, "Brush Settings");
            if (brushOptions)
            {
                EditorGUI.indentLevel++;
                worldGenScript.brushMode = (BrushMode)EditorGUILayout.EnumPopup("Brush Mode", worldGenScript.brushMode);
                if (worldGenScript.brushMode == BrushMode.Grid)
                {
                    worldGenScript.gridRadius = EditorGUILayout.Slider("Grid radius", worldGenScript.gridRadius, 0.2f, 25);
                    worldGenScript.gridGranularity = EditorGUILayout.Slider("Grid granularity", worldGenScript.gridGranularity, 0.05f, 5);
                    if (Mathf.Pow(worldGenScript.gridRadius / worldGenScript.gridGranularity, 2) >= 1000)
                    {
                        EditorGUILayout.HelpBox("Grid with too many dots! Try reducing the grid radius or increasing the granularity", MessageType.Warning);
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            drawingOptions = EditorGUILayout.Foldout(drawingOptions, "Drawing Settings");

            if (drawingOptions)
            {
                EditorGUI.indentLevel++;
                worldGenScript.objectDepth = EditorGUILayout.FloatField("Object depth", worldGenScript.objectDepth);
                worldGenScript.scrollWheelStrength = EditorGUILayout.FloatField("Scroll wheel strength", worldGenScript.scrollWheelStrength);
                EditorGUILayout.Space();
                GUI.enabled = worldGenScript.CurrentShapeToDraw == null;
                worldGenScript.drawingLayer = EditorGUILayout.LayerField("Drawing Layer", worldGenScript.drawingLayer);
                worldGenScript.drawingMode = (DrawingMode)EditorGUILayout.EnumPopup("Drawing Mode", worldGenScript.drawingMode);
                GUI.enabled = true;
                EditorGUILayout.Space();

                if (worldGenScript.drawingMode == DrawingMode.IntantiatePrefab)
                {
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    instanceOptions = EditorGUILayout.Foldout(instanceOptions, "Prefab Instantiation Settings");
                    if (instanceOptions)
                    {
                        EditorGUI.indentLevel++;
                        worldGenScript.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", worldGenScript.prefab, typeof(GameObject), true);

                        EditorGUILayout.Space();
                        string[] sortingLayerNames = new string[SortingLayer.layers.Length];
                        for (int i = 0; i < SortingLayer.layers.Length; i++)
                        {
                            sortingLayerNames[i] = SortingLayer.layers[i].name;
                        }
                        worldGenScript.sortingLayer = EditorGUILayout.Popup("Sorting layer", worldGenScript.sortingLayer, sortingLayerNames);
                        worldGenScript.sortingOrder = EditorGUILayout.IntField("Sorting layer order", worldGenScript.sortingOrder);
                        EditorGUILayout.Space();
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    shapeOptions = EditorGUILayout.Foldout(shapeOptions, "Shape Settings");
                    if (shapeOptions)
                    {
                        EditorGUI.indentLevel++;
                        GUI.enabled = worldGenScript.CurrentShapeToDraw == null;
                        worldGenScript.shapeType = (Shape)EditorGUILayout.EnumPopup("Shape", worldGenScript.shapeType);
                        EditorGUILayout.Space();
                        worldGenScript.shapeCategory = (ColliderType)EditorGUILayout.EnumPopup("Collider Type", worldGenScript.shapeCategory);
                        worldGenScript.fillColor = EditorGUILayout.ColorField("Shape color", worldGenScript.fillColor);
                        GUI.enabled = true;
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }


            // ######### DRAW BUTTON ######### (disable when gizmos not enabled)
            EditorGUILayout.Space(20);


            if (worldGenScript.gizmosEnabled)
            {
                if (worldGenScript.IsDrawing())
                {
                    GUI.backgroundColor = new Color(0.5f,1,0);
                    if (GUILayout.Button("Stop drawing", GUILayout.Height(35)))
                    {
                        worldGenScript.StartStopDrawing();
                    }
                }
                else
                {
                    if (GUILayout.Button("Start drawing", GUILayout.Height(35)))
                    {
                        worldGenScript.StartStopDrawing();
                    }
                }                
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("Start drawing", GUILayout.Height(35));
                GUI.enabled = true;
                EditorGUILayout.HelpBox("You have to enable gizmos so the tool works correctly", MessageType.Error);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
            Repaint();
        }

    }
}