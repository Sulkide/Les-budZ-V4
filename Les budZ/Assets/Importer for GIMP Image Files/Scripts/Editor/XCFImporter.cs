namespace ImporterForGIMPImageFilesEditor {
    using ImporterForGIMPImageFiles;
    using System;
    using System.Diagnostics;
    using System.IO;
    using UnityEditor;
    using UnityEditor.AssetImporters;
    using UnityEngine;

    [ScriptedImporter(1000000, "xcf")]
    internal class XCFImporter : ScriptedImporter {

        //On import asset.
        public override void OnImportAsset(AssetImportContext assetImportContext) {

#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Asset Import ({assetImportContext.assetPath})");
#endif

            //Time the import.
            Stopwatch stopwatch = Stopwatch.StartNew();

            //XCF files are imported firstly to parse the file and get a texture, then post-processing creates a PNG from that texture for which the assets are
            //generated based on texture import settings (e.g. sprites, etc). If this is the latter of those imports, just take the assets from the associated
            //PNG, otherwise go ahead and parse the file.
            string temporaryPNGAssetFilename = TemporaryPNGAsset.getTemporaryPNGAssetFilename(assetImportContext.assetPath,
                    TemporaryPNGAsset.TextureCreationReason.Import, this);
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(temporaryPNGAssetFilename);
            if (mainAsset != null) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                UnityEngine.Debug.Log($"Asset Import ({assetImportContext.assetPath}) - temporary PNG asset found, copying assets across.");
#endif
                UnityEngine.Object temporaryPNGMainAsset = AssetDatabase.LoadMainAssetAtPath(temporaryPNGAssetFilename);
                assetImportContext.AddObjectToAsset(temporaryPNGMainAsset.name, temporaryPNGMainAsset);
                UnityEngine.Object[] temporaryPNGSubAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(temporaryPNGAssetFilename);
                for (int i = 0; i < temporaryPNGSubAssets.Length; i++)
                    assetImportContext.AddObjectToAsset(temporaryPNGSubAssets[i].name, temporaryPNGSubAssets[i]);
                assetImportContext.SetMainObject(temporaryPNGMainAsset);
                return;
            }
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Asset Import ({assetImportContext.assetPath}) - temporary PNG asset not found.");
#endif

            //Get the user data.
            UserData importerUserData = UserData.getUserDataInstance(userData);

#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Asset Import ({assetImportContext.assetPath}) - got importer user data (sprites in sprite sheet: " +
                    $"{(importerUserData.spriteRects == null ? 0 : importerUserData.spriteRects.Length)}).");
#endif

            //Import, catching and reporting any exceptions encountered.
            Texture2D texture = null;
            try {

                //Load the XCF file into a texture and set the various importer settings.
                LoadXCF.LoadedTexture loadedTexture = LoadXCF.load(File.ReadAllBytes(assetImportContext.assetPath), false);
                texture = loadedTexture.texture;
                TextureCache.setCachedTexture(assetImportContext.assetPath, texture.EncodeToPNG());
                importerUserData.fileVersionNumber = loadedTexture.fileVersionNumber;
                importerUserData.precision = loadedTexture.precision;
                importerUserData.compression = loadedTexture.compression;
                importerUserData.width = loadedTexture.width;
                importerUserData.height = loadedTexture.height;
                importerUserData.colourMode = loadedTexture.colourMode;
                importerUserData.error = "";
                stopwatch.Stop();
                importerUserData.importTime = stopwatch.ElapsedMilliseconds;
            }

            //Catch any errors and set the error text in the importer user data. If the import fails, just return a white texture.
            catch (Exception exception) {
                importerUserData.error = exception.Message + exception.StackTrace;
                if (exception.InnerException != null)
                    importerUserData.error += " " + exception.InnerException;
                if (texture != null)
                    DestroyImmediate(texture);
                texture = new Texture2D(32, 32);
                for (int i = 0; i < texture.width; i++)
                    for (int j = 0; j < texture.height; j++)
                        texture.SetPixel(i, j, Color.white);
                texture.Apply();
                importerUserData.importTime = -1;
            }

#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Asset Import ({assetImportContext.assetPath}) - XCF file imported successfully.");
#endif

            //Store the user data as a Json string.
            userData = importerUserData.getJSONStringFromUserData((TextureImporter) GetAtPath(temporaryPNGAssetFilename));

            //Set the texture as the main object on the asset.
            assetImportContext.AddObjectToAsset("Texture", texture);
            assetImportContext.SetMainObject(texture);

#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"Asset Import ({assetImportContext.assetPath}) - added imported texture as main asset object.");
#endif
        }
    }
}