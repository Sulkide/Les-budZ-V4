namespace WorldPainter2D
{
    [System.Serializable]
    public class WorldGenData
    {
        public bool autoSave;

        public int canvasLayer;
        public float[] canvasColor;
        public float[] gridColor;
        public float[] gridCenterColor;

        public int brushMode;
        public float gridRadius;
        public float gridGranularity;

        public int drawingLayer;

        public int drawingMode;

        public int colliderType;
        public int shapeType;
        public float[] shapeColor;

        public string prefabName;
        public bool prefabIsAsset;
        public string prefabPath;

        public int sortingLayer;
        public int sortingLayerOrder;
        public float objectDepth;
        public float scrollWheelStrength;
    }
}
