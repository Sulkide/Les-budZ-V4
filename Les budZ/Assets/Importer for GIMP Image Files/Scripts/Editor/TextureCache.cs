namespace ImporterForGIMPImageFilesEditor {
    using System.Collections.Generic;

    internal static class TextureCache {

        //Variables.
        static Dictionary<string, byte[]> _textureCache = null;
        static Dictionary<string, byte[]> textureCache {
            get {
                if (_textureCache == null)
                    _textureCache = new Dictionary<string, byte[]>();
                return _textureCache;
            }
        }

        //Get/set a cached texture.
        public static byte[] getCachedTexture(string assetPath) {
            if (textureCache.ContainsKey(assetPath))
                return textureCache[assetPath];
            else
                return null;
        }
        public static void setCachedTexture(string assetPath, byte[] PNGEncoding) {
            if (textureCache.ContainsKey(assetPath))
                textureCache[assetPath] = PNGEncoding;
            else
                textureCache.Add(assetPath, PNGEncoding);
        }
    }
}