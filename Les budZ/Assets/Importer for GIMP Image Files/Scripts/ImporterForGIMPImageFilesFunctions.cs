using System.IO;
using UnityEngine;

namespace ImporterForGIMPImageFiles {
    public static class ImporterForGIMPImageFilesFunctions {

        //Classes.
        public class ImageDimensions {
            public int width, height;
        }

        //Return the asset version number.
        /// <summary>
        /// Returns the version number of the Importer for GIMP Image Files asset.
        /// </summary>
        /// <returns>The version number of the Importer for GIMP Image Files asset.</returns>
        public static string GetVersionNumber() => $"{Constants.currentVersionMajor}.{Constants.currentVersionMinor}.{Constants.currentVersionRelease}";

        /// <summary>
        /// Returns the dimensions of an XCF image given the filename.
        /// </summary>
        /// <param name="filename">The filename of the XCF image to get dimensions for.</param>
        /// <returns>The image dimensions.</returns>
        /// <exception cref="ImporterForGIMPImageFilesException"></exception>
        public static ImageDimensions GetImageDimensions(string filename) {
            byte[] XCFFileData = null;
            try {
                XCFFileData = File.ReadAllBytes(filename);
            }
            catch { }
            if (XCFFileData == null)
                throw new ImporterForGIMPImageFilesException(1, "Unable to read file to get image dimensions.");
            else
                return GetImageDimensions(XCFFileData);
        }

        /// <summary>
        /// Returns the dimensions of an XCF image given a byte array of its file contents.
        /// </summary>
        /// <param name="XCFFileData">The array of bytes that represents the XCF file.</param>
        /// <returns>The image dimensions.</returns>
        public static ImageDimensions GetImageDimensions(byte[] XCFFileData) {
            LoadXCF.LoadedTexture loadedTexture = LoadXCF.load(XCFFileData, true);
            return new ImageDimensions {
                width = (int) loadedTexture.width,
                height = (int) loadedTexture.height
            };
        }

        /// <summary>
        /// Returns a texture from an XCF file given the filename.
        /// </summary>
        /// <param name="filename">The filename of the XCF image to get the texture for.</param>
        /// <returns>The XCF file as a texture.</returns>
        /// <exception cref="ImporterForGIMPImageFilesException"></exception>
        public static Texture2D GetImage(string filename) {
            byte[] XCFFileData = null;
            try {
                XCFFileData = File.ReadAllBytes(filename);
            }
            catch { }
            if (XCFFileData == null)
                throw new ImporterForGIMPImageFilesException(41, "Unable to read file to get image.");
            else
                return GetImage(XCFFileData);
        }

        /// <summary>
        /// Returns a texture from an XCF file given a byte array of its file contents.
        /// </summary>
        /// <param name="XCFFileData">The array of bytes that represents the XCF file.</param>
        /// <returns>The XCF file as a texture.</returns>
        public static Texture2D GetImage(byte[] XCFFileData) => LoadXCF.load(XCFFileData, false).texture;
    }
}