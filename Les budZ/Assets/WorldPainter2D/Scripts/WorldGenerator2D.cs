# if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace WorldPainter2D
{
    using System.Linq;
    using WorldPainter2D;
    public enum ColliderType
    {
        Standard,
        Trigger
    }

    public enum BrushMode
    {
        Free,
        Grid
    }

    public enum DrawingMode
    {
        PaintShape,
        IntantiatePrefab
    }

    public enum Shape
    {
        Rect,
        Polygon,
        Circle
    }

    [ExecuteInEditMode]
    public class WorldGenerator2D : MonoBehaviour
    {
#if UNITY_EDITOR

        // Singletone
        private static WorldGenerator2D _instance;
        public static WorldGenerator2D Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldGenerator2D>();
                    if (_instance == null)
                    {
                        _instance = new GameObject("WorldPainter2D Tool").AddComponent<WorldGenerator2D>(); 
                        _instance.gameObject.hideFlags = HideFlags.NotEditable; 
                        _instance.gameObject.tag = "EditorOnly";
                    }
                }
                return _instance;
            }
        }

        //Public drawing configuration variables
        public ColliderType shapeCategory = ColliderType.Standard;
        public BrushMode brushMode = BrushMode.Grid;
        public DrawingMode drawingMode = DrawingMode.PaintShape;

        public LayerMask drawingLayer;
        public int sortingLayer;
        public int sortingOrder = 0;
        public float objectDepth = 0;
        public float scrollWheelStrength = 0.1f;
        public Color fillColor = Color.black;
        public GameObject prefab;

        // General tool variables
        public bool autosaveConfig = true;
        public bool prevAutoSaveConfig = true;
        public bool loadedConfig = false;
        public bool gizmosEnabled;
        public Transform shapeParent;
        public Transform prefabParent;
        public Shape shapeType = Shape.Polygon;

        // Canvas and grid settings
        public Color canvasColor = new Color(1, 1, 1, 0.25f);
        public Color gridColor = Color.green;
        public Color gridCenterColor = Color.blue;
        public float gridRadius = 5f;
        public float gridGranularity = 1f;
        public LayerMask canvasLayer;

        // Private variables
        int instCount = 0;
        GameObject tempInst;
        Vector2 sceneMousePos;
        Vector2 sceneMousePosWithoutGrid;
        WorldGenCanvas canvas;
        GameObject canvasObj;
        bool drawing = false;
        float updateTime = 0;
        float updateRefreshTime = 0.02f;

        // Private shape related variables
        readonly List<DrawShape> _allShapes = new ();
        Dictionary<Shape, DrawShape> _drawModeToPrefab;
        Dictionary<Shape, DrawShape> DrawModeToPrefab {
            get
            {
                if (_drawModeToPrefab == null)
                {
                    _drawModeToPrefab = new Dictionary<Shape, DrawShape> {
                        {Shape.Rect, Resources.Load<DrawRectangle>("Rectangle")},
                        {Shape.Circle, Resources.Load<DrawCircle>("Circle")},
                        {Shape.Polygon, Resources.Load<DrawPolygon>("Polygon")}
                    };
                }
                return _drawModeToPrefab;
            }
        }
        public DrawShape CurrentShapeToDraw { get; set; }
        bool IsDrawingShape { get; set; }

        private void OnValidate()
        {
            if (drawing && CurrentShapeToDraw != null && !CurrentShapeToDraw.Equals(null)) 
            {
                EditorApplication.delayCall += () => 
                {
                    foreach (var hiddenObj in FindObjectsOfType<HiddenObj>())
                    {
                        if(hiddenObj.gameObject != canvasObj) 
                            DestroyImmediate(hiddenObj.gameObject);
                    }                   
                };
                CurrentShapeToDraw = null;
            }
        }

        private void Awake()
        {
            LoadConfig();
            DestroyCanvas();
        }        

        private void OnDestroy() 
        {
            DestroyCanvas();
            DestroyHidden();
        }

        private void DestroyHidden()
        {
            foreach (var item in FindObjectsOfType<HiddenObj>())
            {
                DestroyImmediate(item.gameObject);
            }
        }

        private void DestroyCanvas()
        {
            foreach (var item in FindObjectsOfType<WorldGenCanvas>(true))
            {
                DestroyImmediate(item.gameObject);
            }
            canvas = null;
            canvasObj = null;
        }

        private void InitCanvas()
        {
            DestroyCanvas();
            canvas = Resources.Load<WorldGenCanvas>("WorldGenCanvas");
            canvas.depth = objectDepth;
            canvasObj = Instantiate(canvas).gameObject;
            canvasObj.hideFlags = HideFlags.HideInHierarchy;
            canvasObj.GetComponent<MeshRenderer>().sharedMaterial.color = canvasColor;
            canvasObj.layer = canvasLayer;
        }

        void InitPrefab(Vector2 pos)
        {
            if (prefab == null) return;

            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
                tempInst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            else
                tempInst = Instantiate(prefab);
            tempInst.transform.SetPositionAndRotation(new Vector3(pos.x, pos.y, objectDepth), prefab.transform.rotation);
            tempInst.transform.parent = prefabParent;
            tempInst.name = prefab.name + instCount;
            tempInst.layer = drawingLayer;
            var sr = tempInst.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = SortingLayer.layers[sortingLayer].name;
                sr.sortingOrder = sortingOrder;
            }
            // SetShapePrefabMesh();
        }

        public void StartStopDrawing()
        {
            drawing = !drawing;
            if (autosaveConfig)
                SaveConfig();

            if (drawing)
            {
                InitCanvas();

                if (drawingMode == DrawingMode.IntantiatePrefab)
                {
                    InitPrefab(Vector2.zero);
                }
            }
            else
                StopDrawing();
        }

        private void StopDrawing()
        {
            DestroyCanvas();
            if (tempInst)
            {
                DestroyImmediate(tempInst);
                tempInst = null;
            }

            StopDrawingShape(true);
        }

        public bool IsDrawing() => drawing;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnScene;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
        }


        /// Credit to this guy for they help
        ///  https://answers.unity.com/questions/1260602/how-to-get-mouse-click-world-position-in-the-scene.html
        public void OnScene(SceneView scene)
        {
            Event e = Event.current;
            gizmosEnabled = scene.drawGizmos;

            if (drawing)
            {
                if (!gizmosEnabled)
                {
                    StartStopDrawing();
                    return;
                }

                Vector3 mousePos = e.mousePosition;
                float ppp = EditorGUIUtility.pixelsPerPoint;
                mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
                mousePos.x *= ppp;

                Ray ray = scene.camera.ScreenPointToRay(mousePos);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    sceneMousePos = new Vector2(hit.point.x, hit.point.y);
                    if (brushMode == BrushMode.Grid)
                    {
                        sceneMousePosWithoutGrid = sceneMousePos;
                        sceneMousePos = Util.ClosestPointToMultiple(sceneMousePos, gridGranularity);
                    }

                    if (e.type == EventType.MouseDown) 
                    {
                        if (e.button == 0)
                        {
                            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                            if (drawingMode == DrawingMode.IntantiatePrefab)
                            {
                                if (tempInst != null)
                                {
                                    Undo.RegisterCreatedObjectUndo(tempInst, "instantiate " + tempInst.name);
                                    instCount++;
                                    InitPrefab(sceneMousePos);
                                    Undo.FlushUndoRecordObjects();
                                }
                                else
                                    Debug.LogWarning("WorldPainterEditor2D: Prebaf to instantiate is not set!");
                            }
                            else 
                                AddShapeVertex(sceneMousePos);
                            e.Use();
                        }
                        else if (e.button == 1)
                        {
                            if (drawingMode == DrawingMode.PaintShape && CurrentShapeToDraw != null)
                            {
                                StopDrawingShape(false);
                                e.Use();
                            }
                        }
                    }
                    else if (e.type == EventType.ScrollWheel && e.control)
                    {
                        objectDepth += e.delta.y < 0 ? scrollWheelStrength : -scrollWheelStrength;
                        objectDepth = Mathf.RoundToInt(objectDepth / scrollWheelStrength) * scrollWheelStrength;

                        e.Use();
                    }
                }
                if (CurrentShapeToDraw != null)
                {
                    var pos = CurrentShapeToDraw.gameObject.transform.position;
                    CurrentShapeToDraw.gameObject.transform.position = new Vector3(pos.x, pos.y, objectDepth);
                }

                //if (!SceneView.currentDrawingSceneView.in2DMode && Time.realtimeSinceStartup > updateTime + updateRefreshTime)
                if (Time.realtimeSinceStartup > updateTime + updateRefreshTime)
                {
                    updateTime = Time.realtimeSinceStartup;
                    EditorApplication.QueuePlayerLoopUpdate();
                    SceneView.RepaintAll();
                }
            }
        }


        void OnDrawGizmos()
        {
            if (drawing)
            {
                if (brushMode == BrushMode.Grid)
                {
                    Util.DrawGrid(sceneMousePos, gridRadius, gridGranularity, gridCenterColor, gridColor, objectDepth);
                }
                else
                {
                    Gizmos.color = gridCenterColor;
                    Gizmos.DrawSphere(sceneMousePos, 0.2f);
                }
            }
        }

        void Update()
        {
            if (drawing)
            {
                if (canvas == null)
                    InitCanvas();
                canvasObj.GetComponent<MeshRenderer>().sharedMaterial.color = canvasColor;
                canvasObj.GetComponent<WorldGenCanvas>().depth = objectDepth;

                if (drawingMode == DrawingMode.IntantiatePrefab)
                {
                    if (tempInst != null)
                        DestroyImmediate(tempInst);
                    InitPrefab(sceneMousePos);
                    if (tempInst != null)
                        UpdatePrefab();
                }
                else
                {
                    if (CurrentShapeToDraw != null)
                    {
                        if (IsDrawingShape)
                            CurrentShapeToDraw.UpdateShape(sceneMousePos);
                    }
                    if (tempInst)
                    {
                        DestroyImmediate(tempInst);
                        tempInst = null;
                    }
                }
            }
        }

        private void UpdatePrefab()
        {
            var objPos = sceneMousePos;
            if (brushMode == BrushMode.Grid)
            {
                objPos = Util.ClosestPointToMultiple(sceneMousePosWithoutGrid, gridGranularity); ;
            }
            tempInst.transform.position = new Vector3(objPos.x, objPos.y, objectDepth);

            tempInst.layer = drawingLayer;
            var sr = tempInst.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = SortingLayer.layers[sortingLayer].name;
                sr.sortingOrder = sortingOrder;
            }
        }


        // Adds a new vertex to the current shape at the given position, 
        // or creates a new shape if it doesn't exist
        private void AddShapeVertex(Vector2 position)
        {
            if (CurrentShapeToDraw == null)
            {
                // No current shape -> instantiate a new shape and add two vertices:
                // one for the initial position, and the other for the current cursor position
                var shape = DrawModeToPrefab[shapeType];
                CurrentShapeToDraw = Instantiate(shape, shapeParent);
                CurrentShapeToDraw.name = "Shape " + _allShapes.Count;
                CurrentShapeToDraw.gameObject.hideFlags = HideFlags.HideInHierarchy;

                CurrentShapeToDraw.AddVertex(position);
                CurrentShapeToDraw.AddVertex(position);
                CurrentShapeToDraw.GetComponent<DrawShape>().FillColor = fillColor;
                CurrentShapeToDraw.GetComponent<MeshColorPicker>().color = fillColor;
                CurrentShapeToDraw.gameObject.layer = drawingLayer.value;

                CurrentShapeToDraw.GetComponent<Collider2D>().isTrigger = shapeCategory.Equals(ColliderType.Trigger);
                IsDrawingShape = true;

                _allShapes.Add(CurrentShapeToDraw);
            }
            else
            {
                // Current shape exists -> add vertex if finished and reset reference
                IsDrawingShape = !CurrentShapeToDraw.ShapeFinished;

                if (IsDrawingShape)
                {
                    if (shapeType == Shape.Polygon)
                    {
                        var vertices = CurrentShapeToDraw.Vertices;
                        if (vertices.Count > 2 && (vertices[^1] == vertices[^2] || vertices[^1] == vertices[0]))
                        {
                            StopDrawingShape(false);
                            return;
                        }
                    }
                    CurrentShapeToDraw.AddVertex(position);
                }
                else
                {
                    Undo.RegisterCreatedObjectUndo(CurrentShapeToDraw.gameObject, "finish shape " + CurrentShapeToDraw.gameObject.name);
                    CurrentShapeToDraw.gameObject.GetComponent<Renderer>().enabled = !CurrentShapeToDraw.gameObject.GetComponent<Collider2D>().isTrigger;
                    CurrentShapeToDraw.gameObject.hideFlags = 0;
                    var obj = CurrentShapeToDraw.gameObject;
                    var pos = obj.transform.position;
                    obj.transform.position = new Vector3(pos.x, pos.y, objectDepth);
                    obj.AddComponent<WorldGenShape>();
                    DestroyImmediate(obj.GetComponent<DrawShape>());
                    DestroyImmediate(obj.GetComponent<HiddenObj>());
                    CurrentShapeToDraw = null;
                    Undo.FlushUndoRecordObjects();
                }
            }
        }

        private void StopDrawingShape(bool forceDestroy)
        {
            if (drawingMode == DrawingMode.PaintShape && CurrentShapeToDraw != null)
            {
                if (shapeType == Shape.Polygon)
                {
                    if (((DrawPolygon)CurrentShapeToDraw).CanBeFinished && !forceDestroy)
                    {
                        Undo.RegisterCreatedObjectUndo(CurrentShapeToDraw.gameObject, "finish shape " + CurrentShapeToDraw.gameObject.name);
                        ((DrawPolygon)CurrentShapeToDraw).SetFinishedShape();

                        CurrentShapeToDraw.gameObject.GetComponent<Renderer>().enabled = !CurrentShapeToDraw.gameObject.GetComponent<Collider2D>().isTrigger;

                        CurrentShapeToDraw.gameObject.hideFlags = 0;
                        var obj = CurrentShapeToDraw.gameObject;
                        var pos = obj.transform.position;
                        obj.transform.position = new Vector3(pos.x, pos.y, objectDepth);
                        obj.AddComponent<WorldGenShape>();
                        DestroyImmediate(obj.GetComponent<DrawShape>());
                        DestroyImmediate(obj.GetComponent<LineRenderer>());
                        DestroyImmediate(obj.GetComponent<HiddenObj>());
                        Undo.FlushUndoRecordObjects();
                    }
                    else
                    {
                        DestroyImmediate(CurrentShapeToDraw.gameObject);
                        _allShapes.RemoveAt(_allShapes.Count - 1);
                    }
                }
                else
                {
                    DestroyImmediate(CurrentShapeToDraw.gameObject);
                    _allShapes.RemoveAt(_allShapes.Count - 1);
                }
                CurrentShapeToDraw = null;
            }
        }

        //private Mesh CloneMesh(Mesh origMesh)
        //{
        //    if (origMesh == null)
        //        return null;
        //    Mesh mesh = new Mesh();

        //    mesh.name = origMesh.name;
        //    mesh.vertices = origMesh.vertices;
        //    mesh.triangles = origMesh.triangles;
        //    mesh.normals = origMesh.normals;
        //    mesh.uv = origMesh.uv;
        //    mesh.colors = origMesh.colors;

        //    return mesh;
        //}

        //private void SetShapePrefabMesh()
        //{
        //    if (prefab.GetComponent<WorldGenShape>() != null && prefab.GetComponent<MeshFilter>().sharedMesh == null)
        //    {
        //        Mesh mesh = CloneMesh(Resources.Load<Mesh>("prefab_mesh"));
        //        var meshFilter = prefab.GetComponent<MeshFilter>();
        //        meshFilter.mesh = mesh;
        //        meshFilter.sharedMesh = mesh;
        //    }
        //}

        private string GetResourcePath(string resourceName)
        {
            return AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(this)).Replace("Scripts/WorldGenerator2D.cs", "Resources/" + resourceName);
        }

        public void SaveConfig()
        {
            WorldGenData data = new WorldGenData();

            data.autoSave = autosaveConfig;

            data.canvasLayer = canvasLayer;
            data.canvasColor = new float[] { canvasColor.r, canvasColor.g, canvasColor.b, canvasColor.a };
            data.gridColor = new float[] { gridColor.r, gridColor.g, gridColor.b, gridColor.a };
            data.gridCenterColor = new float[] { gridCenterColor.r, gridCenterColor.g, gridCenterColor.b, gridCenterColor.a };

            if (shapeParent != null && shapeParent.gameObject.GetComponent<WorldPainterShapeParent>() == null)
                shapeParent.gameObject.AddComponent<WorldPainterShapeParent>();
            if (prefabParent != null && prefabParent.gameObject.GetComponent<WorldPainterPrefabParent>() == null)
                prefabParent.gameObject.AddComponent<WorldPainterPrefabParent>();

            data.brushMode = (int)brushMode;
            data.gridRadius = gridRadius;
            data.gridGranularity = gridGranularity;

            data.drawingLayer = drawingLayer;

            data.drawingMode = (int)drawingMode;

            data.colliderType = (int)shapeCategory;
            data.shapeType = (int)shapeType;
            data.shapeColor = new float[] { fillColor.r, fillColor.g, fillColor.b, fillColor.a };

            
            if (prefab != null)
            {
                data.prefabIsAsset = PrefabUtility.IsPartOfPrefabAsset(prefab);
                data.prefabName = prefab.name;

                if (data.prefabIsAsset)
                {
                    data.prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
                }
                else
                {
                    data.prefabPath = prefab.GetInstanceID().ToString();
                }   
            }
            else
                data.prefabName = "";

            data.sortingLayer = sortingLayer;
            data.sortingLayerOrder = sortingOrder;
            data.objectDepth = objectDepth;
            data.scrollWheelStrength = scrollWheelStrength;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetResourcePath("config.json"), json);

            LoadConfig();
        }

        public void LoadConfig()
        {
            string json = File.ReadAllText(GetResourcePath("config.json"));
            WorldGenData data = JsonUtility.FromJson<WorldGenData>(json);

            autosaveConfig = data.autoSave;

            canvasLayer = data.canvasLayer;
            canvasColor = new Color(data.canvasColor[0], data.canvasColor[1], data.canvasColor[2], data.canvasColor[3]);
            gridColor = new Color(data.gridColor[0], data.gridColor[1], data.gridColor[2], data.gridColor[3]);
            gridCenterColor = new Color(data.gridCenterColor[0], data.gridCenterColor[1], data.gridCenterColor[2], data.gridCenterColor[3]);

            var shParent = FindObjectOfType<WorldPainterShapeParent>();
            if (shParent != null)
                shapeParent = shParent.gameObject.GetComponent<Transform>();
            var prfParent = FindObjectOfType<WorldPainterPrefabParent>();
            if (prfParent != null)
                prefabParent = prfParent.gameObject.GetComponent<Transform>();

            brushMode = (BrushMode)data.brushMode;
            gridRadius = data.gridRadius;
            gridGranularity = data.gridGranularity;

            drawingLayer = data.drawingLayer;

            drawingMode = (DrawingMode)data.drawingMode;

            shapeCategory = (ColliderType)data.colliderType;
            shapeType = (Shape)data.shapeType;
            fillColor = new Color(data.shapeColor[0], data.shapeColor[1], data.shapeColor[2], data.shapeColor[3]);

            if (data.prefabName != "")
            {
                if (data.prefabIsAsset)
                {
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefabPath);
                }
                else
                {
                    prefab = EditorUtility.InstanceIDToObject(int.Parse(data.prefabPath)) as GameObject;
                }

                if (prefab)
                    prefab.name = data.prefabName;
            }

            sortingLayer = data.sortingLayer;
            sortingOrder = data.sortingLayerOrder;
            objectDepth = data.objectDepth;
            scrollWheelStrength = data.scrollWheelStrength;

            loadedConfig = true;
        }
#endif
    }
}
# endif