namespace ImporterForGIMPImageFilesEditor {
    using ImporterForGIMPImageFiles;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    internal static class TemporaryPNGAsset {

        //Enums.
        public enum TextureLoadType {
            FromImportedAsset,
            FromAssetFile
        }
        public enum TextureCreationReason {
            Import,
            Settings
        }

        //Create a temporary PNG asset from an asset or a texture.
        public static void create(string XCFPath, TextureCreationReason textureCreationReason) {

#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Create Temporary PNG ({XCFPath}, {textureCreationReason})");
#endif

            //Get the importer from the asset path and load the texture.
            XCFImporter XCFImporter = (XCFImporter) AssetImporter.GetAtPath(XCFPath);

            //If the importer is not found, force an update of the asset.
            if (XCFImporter == null) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                UnityEngine.Debug.Log($"Create Temporary PNG ({XCFPath}, {textureCreationReason}) - importer not found so forcing asset update.");
#endif
                AssetDatabase.ImportAsset(XCFPath, ImportAssetOptions.ForceUpdate);
                return;
            }

            //Get the texture, either by taking it from the asset or loading it from disk and importing the XCF file.
            byte[] textureEncodedPNG = TextureCache.getCachedTexture(XCFPath);
            if (textureEncodedPNG == null) {
                TextureCache.setCachedTexture(XCFPath, LoadXCF.load(File.ReadAllBytes(XCFPath), false).texture.EncodeToPNG());
                textureEncodedPNG = TextureCache.getCachedTexture(XCFPath);
            }
            if (textureEncodedPNG == null)
                throw new ImporterForGIMPImageFilesException(2,
                        $"Importer for GIMP Image Files texture asset not found at path '{XCFPath}' (creation reason {textureCreationReason}).");

#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Create Temporary PNG ({XCFPath}, {textureCreationReason}) - got encoded PNG, ready to write PNG asset.");
#endif

            //Write the PNG file to the assets folder using the texture from the XCF file.
            string temporaryPNGAssetFilename = getTemporaryPNGAssetFilename(XCFPath, textureCreationReason, XCFImporter,
                    textureCreationReason == TextureCreationReason.Import);
            string temporaryPNGAssetFilenameToMoveTo = "";
            if (textureCreationReason == TextureCreationReason.Import)
                temporaryPNGAssetFilenameToMoveTo = getTemporaryPNGAssetFilename(XCFPath, textureCreationReason, XCFImporter, false);
            string directoryName = Path.GetDirectoryName(temporaryPNGAssetFilename);
            if (!AssetDatabase.IsValidFolder(directoryName))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(directoryName), Path.GetFileName(directoryName));
            if (File.Exists(temporaryPNGAssetFilename))
                AssetDatabase.DeleteAsset(temporaryPNGAssetFilename);
            File.WriteAllBytes(temporaryPNGAssetFilename, textureEncodedPNG);
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Create Temporary PNG ({XCFPath}, {textureCreationReason}) - written PNG asset, now forcing asset re-import.");
#endif
            AssetDatabase.ImportAsset(temporaryPNGAssetFilename, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Create Temporary PNG ({XCFPath}, {textureCreationReason}) - forced PNG asset re-import.");
#endif

            //Get the texture import settings stored on the PNG file importer.
            TextureImporter PNGImporter = (TextureImporter) AssetImporter.GetAtPath(temporaryPNGAssetFilename);
            UserData userData = UserData.getUserDataInstance(XCFImporter.userData);
            if (userData.firstImport) {
                userData.firstImport = false;
                XCFImporter.userData = userData.getJSONStringFromUserData(PNGImporter);
            }
            userData.applyToAssetImporter(PNGImporter);
            if (textureCreationReason == TextureCreationReason.Import) {
                string directoryNameToMoveTo = Path.GetDirectoryName(temporaryPNGAssetFilenameToMoveTo);
                AssetDatabase.CreateFolder(Path.GetDirectoryName(directoryNameToMoveTo), Path.GetFileName(directoryNameToMoveTo));
                AssetDatabase.MoveAsset(temporaryPNGAssetFilename, temporaryPNGAssetFilenameToMoveTo);
                AssetDatabase.ImportAsset(temporaryPNGAssetFilenameToMoveTo, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AssetDatabase.DeleteAsset(directoryName);
            }
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log(
                    $"Create Temporary PNG ({XCFPath}, {textureCreationReason}) - set import settings on PNG importer.");
#endif
        }

        //Returns the filename of the temporary PNG file for an XCF file given its asset path and importer.
        public static string getTemporaryPNGAssetFilename(string path, TextureCreationReason textureCreationReason, XCFImporter importer = null,
                bool noImport = false) {
            if (importer == null)
                importer = (XCFImporter) AssetImporter.GetAtPath(path);
            string randomNumericCharacters = importer == null ? "0".PadLeft(UserData.numberOfRandomNumericCharacters, '0') :
                    UserData.getUserDataInstance(importer.userData).randomNumericCharacters;
            return Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) +
                    (textureCreationReason == TextureCreationReason.Import ? (noImport ? "_NoImport_" : "_Import_") : "_Settings_") +
                    randomNumericCharacters, Path.GetFileNameWithoutExtension(path)), "png").Replace(@"\", "/");
        }
    }
}