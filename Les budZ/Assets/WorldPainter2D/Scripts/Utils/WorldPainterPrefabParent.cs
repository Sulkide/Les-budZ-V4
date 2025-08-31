using UnityEngine;
#if UNITY_EDITOR
namespace WorldPainter2D
{
    [ExecuteInEditMode]
    public class WorldPainterPrefabParent : MonoBehaviour
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
                if (wg2d.prefabParent == null || !wg2d.prefabParent.gameObject.Equals(gameObject))
                    DestroyImmediate(this);
            }
        }
    }
}
#endif