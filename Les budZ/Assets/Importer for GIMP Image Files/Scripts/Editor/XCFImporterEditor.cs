namespace ImporterForGIMPImageFilesEditor {
    using ImporterForGIMPImageFiles;
    using UnityEditor;
    using UnityEditor.AssetImporters;
    using UnityEngine;

    [CustomEditor(typeof(XCFImporter))]
    internal class XCFImporterEditor : ScriptedImporterEditor {

        //Variables.
        bool infoFoldOut = true;
        bool importSettingsFoldOut = true;
        static GUIStyle _foldOutBold = null;
        static GUIStyle foldOutBold {
            get {
                if (_foldOutBold == null) {
                    _foldOutBold = new GUIStyle(EditorStyles.foldout);
                    _foldOutBold.fontStyle = FontStyle.Bold;
                }
                return _foldOutBold;
            }
        }
        static GUIStyle _versionStyle = null;
        static GUIStyle versionStyle {
            get {
                if (_versionStyle == null) {
                    _versionStyle = new GUIStyle(GUI.skin.label);
                    _versionStyle.fontStyle = FontStyle.Bold;
                    _versionStyle.normal.textColor = new Color(0, 0.4f, 0.8f);
                    _versionStyle.hover.textColor = new Color(0, 0.4f, 0.8f);
                    _versionStyle.active.textColor = new Color(0, 0.4f, 0.8f);
                    _versionStyle.focused.textColor = new Color(0, 0.4f, 0.8f);
                    _versionStyle.alignment = TextAnchor.MiddleRight;
                }
                return _versionStyle;
            }
        }

        //Don't show the imported object.
        public override bool showImportedObject => false;

        //Don't need to show apply/revert buttons.
        protected override bool needsApplyRevert => false;

        //Show the open button.
        protected override bool ShouldHideOpenButton() => false;

        //Draw the asset preview.
        protected override bool useAssetDrawPreview => true;

        //Draw the inspector.
        public override void OnInspectorGUI() {
            Rect rect;

            //Get the target object and user data.
            XCFImporter importer = (XCFImporter) target;
            UserData userData = UserData.getUserDataInstance(importer.userData);

            //Show any errors with importing.
            if (userData.error != "") {
                EditorGUILayout.HelpBox("There was an error importing the XCF file: " + userData.error, MessageType.Error);
                EditorGUILayout.GetControlRect();
            }

            //Add an import settings section with fold out.
            importSettingsFoldOut = EditorGUILayout.Foldout(importSettingsFoldOut, new GUIContent("Texture Import Settings"), true, foldOutBold);
            if (importSettingsFoldOut) {
                string textureType;
                switch (userData.textureImporterSettings.textureType) {
                    case TextureImporterType.Cookie:
                        textureType = "Cookie";
                        break;
                    case TextureImporterType.Cursor:
                        textureType = "Cursor";
                        break;
                    case TextureImporterType.Default:
                        textureType = "Default";
                        break;
                    case TextureImporterType.DirectionalLightmap:
                        textureType = "Directional Lightmap";
                        break;
                    case TextureImporterType.GUI:
                        textureType = "GUI";
                        break;
                    case TextureImporterType.Lightmap:
                        textureType = "Lightmap";
                        break;
                    case TextureImporterType.NormalMap:
                        textureType = "Normal Map";
                        break;
                    case TextureImporterType.Shadowmask:
                        textureType = "Shadowmask";
                        break;
                    case TextureImporterType.SingleChannel:
                        textureType = "Single Channel";
                        break;
                    case TextureImporterType.Sprite:
                        textureType = "Sprite";
                        break;
                    default:
                        textureType = userData.textureImporterSettings.textureType.ToString();
                        break;
                }
                EditorGUILayout.LabelField(new GUIContent("Texture Type"), new GUIContent(textureType));
                rect = EditorGUILayout.GetControlRect();
                if (GUI.Button(rect, new GUIContent("Edit Texture Import Settings")))
                    TemporaryPNGAsset.create(importer.assetPath, TemporaryPNGAsset.TextureCreationReason.Settings);
            }

            //Add an "Info" section with fold out.
            EditorGUILayout.GetControlRect();
            infoFoldOut = EditorGUILayout.Foldout(infoFoldOut, new GUIContent("Info"), true, foldOutBold);
            if (infoFoldOut) {
                if (!string.IsNullOrWhiteSpace(userData.error))
                    EditorGUILayout.HelpBox("Please import the asset to view its information.", MessageType.Info);
                else {
                    EditorGUILayout.LabelField(new GUIContent("Importer for GIMP Image Files Version",
                            "The version of Importer for GIMP Image Files used to import this XCF file. The latest version is available on the Unity Asset Store."),
                            new GUIContent($"{userData.versionMajor}.{userData.versionMinor}.{userData.versionRelease}"));
                    string colourMode = userData.colourMode.ToString();
                    if (userData.colourMode == LoadXCF.ColorMode.RGB)
                        colourMode = "RGB";
                    else if (userData.colourMode == LoadXCF.ColorMode.Greyscale)
                        colourMode = "Greyscale";
                    else if (userData.colourMode == LoadXCF.ColorMode.Indexed)
                        colourMode = "Indexed";
                    EditorGUILayout.LabelField(new GUIContent("Colour Mode", "The colour mode of the XCF file - RGB, Greyscale or Indexed."),
                            new GUIContent(colourMode));
                    string compression = userData.compression.ToString();
                    if (userData.compression == LoadXCF.Compression.None)
                        compression = "None";
                    else if (userData.compression == LoadXCF.Compression.RLEEncoding)
                        compression = "RLE Encoding";
                    else if (userData.compression == LoadXCF.Compression.zLib)
                        compression = "zLib";
                    EditorGUILayout.LabelField(new GUIContent("Compression",
                            "The compression method used in the XCF file - None, RLE Encoding (the default) or zLib."), new GUIContent(compression));
                    EditorGUILayout.LabelField(new GUIContent("Dimensions",
                            "The dimensions of the XCF file in pixels. An image of this size will be imported."),
                            new GUIContent($"{userData.width} x {userData.height}"));
                    string precision = userData.precision.ToString();
                    if (userData.precision == LoadXCF.Precision._8BitLinearInteger)
                        precision = "8-Bit Linear Integer";
                    else if (userData.precision == LoadXCF.Precision._8BitGammaInteger)
                        precision = "8-Bit Gamma Integer";
                    else if (userData.precision == LoadXCF.Precision._16BitLinearInteger)
                        precision = "16-Bit Linear Integer";
                    else if (userData.precision == LoadXCF.Precision._16BitGammaInteger)
                        precision = "16-Bit Gamma Integer";
                    else if (userData.precision == LoadXCF.Precision._32BitLinearInteger)
                        precision = "32-Bit Linear Integer";
                    else if (userData.precision == LoadXCF.Precision._32BitGammaInteger)
                        precision = "32-Bit Gamma Integer";
                    else if (userData.precision == LoadXCF.Precision._16BitLinearFloatingPoint)
                        precision = "16-Bit Linear Floating Point";
                    else if (userData.precision == LoadXCF.Precision._16BitGammaFloatingPoint)
                        precision = "16-Bit Gamma Floating Point";
                    else if (userData.precision == LoadXCF.Precision._32BitLinearFloatingPoint)
                        precision = "32-Bit Linear Floating Point";
                    else if (userData.precision == LoadXCF.Precision._32BitGammaFloatingPoint)
                        precision = "32-Bit Gamma Floating Point";
                    else if (userData.precision == LoadXCF.Precision._64BitLinearFloatingPoint)
                        precision = "64-Bit Linear Floating Point";
                    else if (userData.precision == LoadXCF.Precision._64BitGammaFloatingPoint)
                        precision = "64-Bit Gamma Floating Point";
                    EditorGUILayout.LabelField(new GUIContent("Precision",
                            "The precision of the image data in bits, and whether it uses linear or gamma colour space."), new GUIContent(precision));
                    EditorGUILayout.LabelField(new GUIContent("XCF File Version", "The version number of the XCF file."),
                            new GUIContent(userData.fileVersionNumber.ToString()));
                    if (userData.importTime > -1)
                        EditorGUILayout.LabelField(new GUIContent("Last Import Time", "The amount of time the last import took in seconds."),
                                new GUIContent($"{userData.importTime / 1000d:#,##0.000} sec"));
                }
            }

            //Version details.
            EditorGUILayout.GetControlRect();
            rect = EditorGUILayout.GetControlRect();
            if (GUI.Button(new Rect(rect.xMax - 100, rect.yMin, 100, rect.height),
                    $"Version {Constants.currentVersionMajor}.{Constants.currentVersionMinor}.{Constants.currentVersionRelease}", versionStyle)) {
                VersionChanges versionChangesEditorWindow = EditorWindow.GetWindow<VersionChanges>();
                versionChangesEditorWindow.minSize = new Vector2(800, 600);
                versionChangesEditorWindow.titleContent = new GUIContent("Importer for GIMP Image Files - Version Changes");
            }
        }
    }
}