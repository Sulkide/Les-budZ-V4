# if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace WorldPainter2D
{
    namespace WorldPainter2D
    {
        [ExecuteInEditMode]
        public class WorldGenCanvas : MonoBehaviour
        {
            public float depth = 1;

            private void OnValidate() { SceneView.duringSceneGui += OnScene;  } 

            private void Awake()
            {
                SceneView.duringSceneGui += OnScene; 
            }

            private void OnDestroy()
            {
                SceneView.duringSceneGui -= OnScene;
            }

            public void OnScene(SceneView scene)
            {
                if (this == null)
                    return;
                if (scene.in2DMode)
                {
                    transform.position = new Vector3(scene.pivot.x, scene.pivot.y, depth);
                    var lowCorner = scene.camera.ScreenToWorldPoint(Vector3.zero);
                    transform.localScale = new Vector3(2 * Mathf.Abs(lowCorner.x - transform.position.x), 2 * Mathf.Abs(lowCorner.y - transform.position.y), 1);
                }
                else
                {
                    var cameraPos = scene.camera.ScreenToWorldPoint(Vector3.zero);
                    transform.position = new Vector3(cameraPos.x, cameraPos.y, depth);
                    transform.localScale = (Vector3.one - Vector3.forward) * 1000 + Vector3.forward;
                }
            }
        }
    }
}
#endif
