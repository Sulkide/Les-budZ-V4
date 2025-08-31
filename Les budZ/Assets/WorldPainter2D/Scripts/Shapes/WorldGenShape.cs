# if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace WorldPainter2D
{
    [ExecuteInEditMode]
    public class WorldGenShape : MonoBehaviour
    {
        public SnapGrid snapGrid;

        public Vector3[] Verts
        {
            get
            {
                var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                return mesh != null ? mesh.vertices : null;
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnScene;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
        }

        public void OnScene(SceneView scene)
        {
            UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

            if (snapGrid == null)
                snapGrid = SnapGrid.Instance;
            if (snapGrid.snapping && Selection.Contains(gameObject))
            {
                var vertices = Verts;
                snapGrid.vertexIdx = Mathf.Clamp(snapGrid.vertexIdx, 0, vertices.Length - 1);
                var pivotVertex = transform.localToWorldMatrix.MultiplyPoint(vertices[snapGrid.vertexIdx]);

                var closestPos = Util.ClosestPointToMultiple(pivotVertex, snapGrid.gridGranularity);
                var transformDirection = closestPos - new Vector2(pivotVertex.x, pivotVertex.y);

                transform.position = new Vector3(transform.position.x + transformDirection.x, transform.position.y + transformDirection.y, transform.position.z);
            }
        }

        public void OnDrawGizmosSelected()
        {
            if (snapGrid == null)
                snapGrid = SnapGrid.Instance;
            if (snapGrid.snapping)
            {
                var vertices = Verts;
                snapGrid.vertexIdx = Mathf.Clamp(snapGrid.vertexIdx, 0, vertices.Length - 1);
                var pivotVertex = vertices[snapGrid.vertexIdx];

                Util.DrawGrid(transform.localToWorldMatrix.MultiplyPoint(pivotVertex),
                    snapGrid.gridRadius, snapGrid.gridGranularity, Color.blue, Color.green, drawCenter:true);

                Vector2[] targetVerts = new Vector2[4];
                var bounds = GetComponent<MeshFilter>().sharedMesh.bounds;
                var extents = bounds.extents;
                targetVerts[0] = new Vector2(bounds.center.x + extents.x, bounds.center.y + extents.y);
                targetVerts[1] = new Vector2(bounds.center.x + extents.x, bounds.center.y - extents.y);
                targetVerts[2] = new Vector2(bounds.center.x - extents.x, bounds.center.y + extents.y);
                targetVerts[3] = new Vector2(bounds.center.x - extents.x, bounds.center.y - extents.y);
                foreach (var vertex in targetVerts)
                {                    
                    Util.DrawGrid(transform.localToWorldMatrix.MultiplyPoint(vertex),
                        snapGrid.gridRadius, snapGrid.gridGranularity, Color.blue, Color.green, drawCenter:false);
                }
            }
        }
    }
}
#endif