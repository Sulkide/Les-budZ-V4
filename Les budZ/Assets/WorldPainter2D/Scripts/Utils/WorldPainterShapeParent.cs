using UnityEngine;
# if UNITY_EDITOR
namespace WorldPainter2D
{
    [ExecuteInEditMode]
    public class WorldPainterShapeParent : MonoBehaviour
    {
        private WorldGenerator2D wg2d;
        void Update()
        {
            if (wg2d == null)
            {
                wg2d = FindObjectOfType<WorldGenerator2D>();
            }
            else
            {
                if (wg2d.shapeParent == null || !wg2d.shapeParent.gameObject.Equals(gameObject))
                    DestroyImmediate(this);
            }

        }
    }
}
#endif