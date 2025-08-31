#if UNITY_EDITOR
namespace WorldPainter2D
{
    public class SnapGrid
    {
        private static SnapGrid _instance;

        public static SnapGrid Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SnapGrid();
                }
                return _instance;
            }
        }

        public float gridRadius = 7;
        public float gridGranularity = 1;
        public bool snapping;
        public int vertexIdx = 0;

        public void StartStopSnapToGrid()
        {
            snapping = !snapping;
        }
    }
}
#endif