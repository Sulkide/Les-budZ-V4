namespace ImporterForGIMPImageFilesEditor {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;

    internal class XCFAssetPostProcessor : AssetPostprocessor {

        //Classes.
        class ImportedXCFAsset {
            public string path;
            public XCFImporter importer;
        }
        class ImportedPNGAsset {
            public string XCFPath;
            public string PNGPath;
            public TextureImporter importer;
        }
        class EditorDelayedCallParameters {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            public int debugTag;
#endif
            public ImportedXCFAsset[] importedXCFAssets;
            public ImportedPNGAsset[] importedPNGAssetsForXCFImport;
            public ImportedPNGAsset importedPNGAssetForTextureSettings;
        }

        //Variables.
        static List<EditorDelayedCallParameters> _editorDelayedCallParameters = null;
        static List<EditorDelayedCallParameters> editorDelayedCallParameters {
            get {
                if (_editorDelayedCallParameters == null)
                    _editorDelayedCallParameters = new List<EditorDelayedCallParameters>();
                return _editorDelayedCallParameters;
            }
        }

        //Called post-process of all assets.
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {

            //Loop over all the XCF assets that were imported.
            List<ImportedXCFAsset> importedXCFAssetsList = new List<ImportedXCFAsset>();
            for (int i = 0; i < importedAssets.Length; i++)
                if (Path.GetExtension(importedAssets[i]).ToLower().TrimStart('.') == "xcf")
                    importedXCFAssetsList.Add(new ImportedXCFAsset {
                        path = importedAssets[i],
                        importer = (XCFImporter) AssetImporter.GetAtPath(importedAssets[i])
                    });
            ImportedXCFAsset[] importedXCFAssets = importedXCFAssetsList.ToArray();

            //Loop over all the PNG assets that were imported and are associated with an XCF asset.
            List<ImportedPNGAsset> importedPNGAssetsForXCFImportList = new List<ImportedPNGAsset>();
            ImportedPNGAsset importedPNGAssetForTextureSettings = null;
            string[] XCFAssets = null, temporaryPNGImportAssets = null, temporaryPNGSettingsAssets = null;
            EditorWindow[] openEditorWindows = null;
            for (int i = 0; i < importedAssets.Length; i++) {
                if (Path.GetExtension(importedAssets[i]).ToLower().TrimStart('.') == "png") {
                    if (XCFAssets == null) {
                        XCFAssets = AssetDatabase.GetAllAssetPaths().Where(p => p.ToLower().StartsWith("assets/") && p.ToLower().EndsWith(".xcf")).ToArray();
                        temporaryPNGImportAssets = new string[XCFAssets.Length];
                        temporaryPNGSettingsAssets = new string[XCFAssets.Length];
                        for (int j = 0; j < XCFAssets.Length; j++) {
                            temporaryPNGImportAssets[j] = TemporaryPNGAsset.getTemporaryPNGAssetFilename(XCFAssets[j],
                                    TemporaryPNGAsset.TextureCreationReason.Import);
                            temporaryPNGSettingsAssets[j] = TemporaryPNGAsset.getTemporaryPNGAssetFilename(XCFAssets[j],
                                    TemporaryPNGAsset.TextureCreationReason.Settings);
                        }
                    }
                    if (openEditorWindows == null)
                        openEditorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                    for (int j = 0; j < XCFAssets.Length; j++)
                        if (importedAssets[i] == temporaryPNGImportAssets[j]) {
                            importedPNGAssetsForXCFImportList.Add(new ImportedPNGAsset() {
                                XCFPath = XCFAssets[j],
                                PNGPath = importedAssets[i],
                                importer = (TextureImporter) AssetImporter.GetAtPath(importedAssets[i])
                            });
                            break;
                        }
                        else if (importedAssets[i] == temporaryPNGSettingsAssets[j]) {

                            //Determine if a modal window is open for modifying the texture import settings, and don't open another one if it is.
                            EditorWindow textureSettingsWindow = openEditorWindows.Where(w => w.name == importedAssets[i]).FirstOrDefault();
                            if (textureSettingsWindow != null) {

                                //If the sprite editor window is open, update it so it refreshes. Otherwise the texture within it disappears.
                                try {
                                    for (int k = 0; k < openEditorWindows.Length; k++)
                                        if (openEditorWindows[k].GetType().Name == "SpriteEditorWindow") {
                                            int kCopy = k;
                                            Task.Run(async () => {
                                                await Task.Delay(1);
                                                openEditorWindows[kCopy].GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                                                        .Invoke(openEditorWindows[kCopy], null);
                                            });
                                        }
                                }
                                catch { }
                            }

                            //If no texture settings window is open, flag for one to open.
                            else
                                importedPNGAssetForTextureSettings = new ImportedPNGAsset() {
                                    XCFPath = XCFAssets[j],
                                    PNGPath = importedAssets[i],
                                    importer = (TextureImporter) AssetImporter.GetAtPath(importedAssets[i])
                                };
                            break;
                        }
                }
            }
            ImportedPNGAsset[] importedPNGAssetsForXCFImport = importedPNGAssetsForXCFImportList.ToArray();

            //If at least one XCF/PNG asset was imported, use the editor delay call functionality to perform the next step of asset import after one frame.
            if (importedXCFAssets.Length > 0 || importedPNGAssetsForXCFImport.Length > 0 || importedPNGAssetForTextureSettings != null) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                int debugTag = UnityEngine.Random.Range(1000, 10000);
                string importedXCFAssetsString = importedXCFAssets.Length.ToString();
                for (int i = 0; i < importedXCFAssets.Length; i++)
                    importedXCFAssetsString += (i == 0 ? " - " : ", ") + importedXCFAssets[i].path;
                string importedPNGAssetsString = importedPNGAssetsForXCFImport.Length.ToString();
                for (int i = 0; i < importedPNGAssetsForXCFImport.Length; i++)
                    importedPNGAssetsString += (i == 0 ? " - " : ", ") + importedPNGAssetsForXCFImport[i].PNGPath;
                UnityEngine.Debug.Log($"Asset Post Processing ({debugTag}) - imported XCF assets: {importedXCFAssetsString}; imported PNG assets: " +
                        $"{importedPNGAssetsString}; imported PNG asset for texture settings: {(importedPNGAssetForTextureSettings != null ? "yes" : "no")}");
#endif
                editorDelayedCallParameters.Add(new EditorDelayedCallParameters() {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                    debugTag = debugTag,
#endif
                    importedXCFAssets = importedXCFAssets,
                    importedPNGAssetsForXCFImport = importedPNGAssetsForXCFImport,
                    importedPNGAssetForTextureSettings = importedPNGAssetForTextureSettings
                });
                EditorApplication.delayCall += delayedCall;
            }
        }

        //Method called one frame after post-processing.
        static void delayedCall() {

            while (editorDelayedCallParameters.Count > 0) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                int debugTag = editorDelayedCallParameters[0].debugTag;
                UnityEngine.Debug.Log($"Asset Post Processing Delayed Call ({debugTag})");
#endif
                ImportedXCFAsset[] importedXCFAssets = editorDelayedCallParameters[0].importedXCFAssets;
                ImportedPNGAsset importedPNGAssetForTextureSettings = editorDelayedCallParameters[0].importedPNGAssetForTextureSettings;
                ImportedPNGAsset[] importedPNGAssetsForXCFImport = editorDelayedCallParameters[0].importedPNGAssetsForXCFImport;
                editorDelayedCallParameters.RemoveAt(0);

                //Loop over the imported XCF assets.
                for (int i = 0; i < importedXCFAssets.Length; i++) {
                    string temporaryFilename = TemporaryPNGAsset.getTemporaryPNGAssetFilename(importedXCFAssets[i].path,
                            TemporaryPNGAsset.TextureCreationReason.Import, importedXCFAssets[i].importer);
                    UnityEngine.Object temporaryFilenameMainAsset = AssetDatabase.LoadMainAssetAtPath(temporaryFilename);
                    if (temporaryFilenameMainAsset == null) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                        UnityEngine.Debug.Log($"Asset Post Processing Delayed Call ({debugTag}) - processing imported XCF asset {importedXCFAssets[i].path} " +
                                "and PNG not found so creating it.");
#endif
                        TemporaryPNGAsset.create(importedXCFAssets[i].path, TemporaryPNGAsset.TextureCreationReason.Import);
                    }
                    else {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                        UnityEngine.Debug.Log($"Asset Post Processing Delayed Call ({debugTag}) - processing imported XCF asset {importedXCFAssets[i].path} " +
                                "and PNG found so deleting it.");
#endif
                        AssetDatabase.DeleteAsset(temporaryFilename);
                        AssetDatabase.DeleteAsset(Path.GetDirectoryName(temporaryFilename));
                    }
                }

                //Loop over the PNG assets that are associated with the import of an XCF asset and re-import the XCF ones.
                for (int i = 0; i < importedPNGAssetsForXCFImport.Length; i++) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                    UnityEngine.Debug.Log($"Asset Post Processing Delayed Call ({debugTag}) - processing imported PNG asset " +
                            $"{importedPNGAssetsForXCFImport[i].PNGPath} so re-importing associated XCF ({importedPNGAssetsForXCFImport[i].XCFPath}).");
#endif
                    AssetDatabase.ImportAsset(importedPNGAssetsForXCFImport[i].XCFPath);
                }

                //Loop over the PNG assets that are associated with changing the settings of an XCF asset and open a modal window allowing the settings to be
                //changed.
                if (importedPNGAssetForTextureSettings != null) {

                    //Get the asset name.
                    string assetName = Path.GetFileNameWithoutExtension(importedPNGAssetForTextureSettings.XCFPath);

#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                    UnityEngine.Debug.Log($"Asset Post Processing Delayed Call ({debugTag}) - processing imported PNG asset for texture settings ({assetName}).");
#endif

                    //Create an inspector editor window.
                    Rect mainWindowPosition = EditorGUIUtility.GetMainWindowPosition();
                    int width = Mathf.RoundToInt(mainWindowPosition.width * 0.25f);
                    int height = Mathf.RoundToInt(mainWindowPosition.height * 0.875f);
                    EditorWindow[] openEditorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                    for (int i = 0; i < openEditorWindows.Length; i++)
                        if (openEditorWindows[i].GetType().Name == "InspectorWindow") {
                            width = Mathf.RoundToInt(openEditorWindows[i].position.width);
                            height = Mathf.RoundToInt(openEditorWindows[i].position.height);
                            break;
                        }
                    Type inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
                    EditorWindow inspectorInstance = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;
                    inspectorInstance.name = importedPNGAssetForTextureSettings.PNGPath;
                    inspectorInstance.position = new Rect(mainWindowPosition.center.x - (width / 2), mainWindowPosition.center.y - (height / 2), width, height);

                    //Select the temporary PNG file, lock the inspector window so this selection doesn't change and re-select the old object (for other inspectors).
                    UnityEngine.Object previousSelection = Selection.activeObject;
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(importedPNGAssetForTextureSettings.PNGPath);
                    PropertyInfo isLocked = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
                    isLocked.GetSetMethod().Invoke(inspectorInstance, new object[] { true });
                    Selection.activeObject = previousSelection;

                    //Set the texture settings on the temporary PNG importer.
                    XCFImporter XCFImporter = (XCFImporter) AssetImporter.GetAtPath(importedPNGAssetForTextureSettings.XCFPath);
                    UserData.getUserDataInstance(XCFImporter.userData).applyToAssetImporter(importedPNGAssetForTextureSettings.importer);

                    //Cache the paths in case they change when the modal window is open.
                    string cachedXCFPath = importedPNGAssetForTextureSettings.XCFPath;
                    string cachedPNGPath = importedPNGAssetForTextureSettings.PNGPath;
                    TextureImporter cachedImporter = importedPNGAssetForTextureSettings.importer;

                    //Set the title of the modal window and show it.
                    inspectorInstance.titleContent = new GUIContent($"{assetName} Texture Import Settings");
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                    UnityEngine.Debug.Log($"Asset Post Processing Delayed Call ({debugTag}) - opening modal inspector window.");
#endif
                    inspectorInstance.ShowModalUtility();

                    //Close any sprite sheet editor windows that are open for this texture.
                    openEditorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                    for (int i = 0; i < openEditorWindows.Length; i++)
                        if (openEditorWindows[i].GetType().Name == "SpriteEditorWindow") {
                            try {
                                if (openEditorWindows[i].GetType().GetField("m_SelectedAssetPath", BindingFlags.Instance | BindingFlags.NonPublic)
                                        .GetValue(openEditorWindows[0]).ToString() == cachedPNGPath)
                                    openEditorWindows[i].Close();
                            }
                            catch { }
                        }

                    //Copy the texture importer settings from the temporary PNG texture importer to the XCF file.
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                    UnityEngine.Debug.Log($"Asset Post Processing Delayed Call ({debugTag}) - copying temporary PNG texture importer files to the XCF file.");
#endif
                    XCFImporter = (XCFImporter) AssetImporter.GetAtPath(cachedXCFPath);
                    XCFImporter.userData = UserData.getUserDataInstance(XCFImporter.userData).getJSONStringFromUserData(cachedImporter);

                    //Once the modal window is closed, move the asset from the settings folder to the import folder.
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                    UnityEngine.Debug.Log(
                            $"Asset Post Processing Delayed Call ({debugTag}) - copying temporary PNG texture from settings to import and forcing update.");
#endif
                    string settingsDirectory = Path.GetDirectoryName(cachedPNGPath);
                    string importDirectory = Path.Combine(Path.GetDirectoryName(cachedXCFPath),
                            Path.GetFileName(settingsDirectory).Replace("_Settings_", "_Import_"));
                    string importFile = Path.Combine(importDirectory, Path.GetFileName(cachedPNGPath));
                    if (!AssetDatabase.IsValidFolder(importDirectory))
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(importDirectory), Path.GetFileName(importDirectory));
                    if (File.Exists(importFile))
                        AssetDatabase.DeleteAsset(importFile);
                    AssetDatabase.MoveAsset(cachedPNGPath, importFile);
                    AssetDatabase.DeleteAsset(settingsDirectory);
                    AssetDatabase.ImportAsset(importFile, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}