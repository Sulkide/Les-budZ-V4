namespace ImporterForGIMPImageFilesEditor {
    using ImporterForGIMPImageFiles;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEditor;
#if UNITY_2D_SPRITE_PACKAGE
    using UnityEditor.U2D.Sprites;
#endif
    using UnityEngine;
    using UnityEngine.U2D;

    internal class UserData {

        //Constants.
        public const int numberOfRandomNumericCharacters = 16;
        static string[] _platforms = null;
        static string[] platforms {
            get {
                if (_platforms == null)
                    _platforms = new string[] {
                        "Standalone",
                        "Web",
                        "iPhone",
                        "Android",
                        "WebGL",
                        "Windows Store Apps",
                        "PS4",
                        "XboxOne",
                        "Nintendo Switch",
                        "tvOS"
                    };
                return _platforms;
            }
        }

        //User data.
        public int versionMajor;
        public int versionMinor;
        public int versionRelease;
        public int fileVersionNumber;
        public uint width, height;
        public LoadXCF.ColorMode colourMode;
        public LoadXCF.Precision precision;
        public LoadXCF.Compression compression;
        public string error;
        public string randomNumericCharacters;
        public bool firstImport;
        public TextureImporterSettings textureImporterSettings;
#if UNITY_2D_SPRITE_PACKAGE
        [NonSerialized]
        public SpriteRect[] spriteRects;
#endif
        [NonSerialized]
        public TextureImporterPlatformSettings defaultTextureImporterPlatformSettings;
        [NonSerialized]
        public TextureImporterPlatformSettings[] textureImporterPlatformSettings;
        public long importTime;
        [NonSerialized]
        public SecondarySpriteTexture[] secondarySpriteTextures;
        [NonSerialized]
        public List<Vector2[]>[] spriteOutlines;
        [NonSerialized]
        public List<Vector2[]>[] spritePhysicsShapes;
        [NonSerialized]
        public List<SpriteBone>[] spriteBones;
#if UNITY_2D_SPRITE_PACKAGE
        [NonSerialized]
        public Vertex2DMetaData[][] spriteMeshVertices;
#endif
        [NonSerialized]
        public Vector2Int[][] spriteMeshEdges;
        [NonSerialized]
        public int[][] spriteMeshIndices;

        //Constructor.
        public UserData() {
            versionMajor = Constants.currentVersionMajor;
            versionMinor = Constants.currentVersionMinor;
            versionRelease = Constants.currentVersionRelease;
            fileVersionNumber = -1;
            width = 0;
            height = 0;
            colourMode = LoadXCF.ColorMode.RGB;
            precision = LoadXCF.Precision._8BitLinearInteger;
            compression = LoadXCF.Compression.None;
            error = "";
            firstImport = true;
            textureImporterSettings = null;
#if UNITY_2D_SPRITE_PACKAGE
            spriteRects = null;
#endif
            textureImporterPlatformSettings = null;
            defaultTextureImporterPlatformSettings = null;
            importTime = -1;
            secondarySpriteTextures = null;
            spriteOutlines = null;
            spritePhysicsShapes = null;
            spriteBones = null;
#if UNITY_2D_SPRITE_PACKAGE
            spriteMeshVertices = null;
#endif
            spriteMeshEdges = null;
            spriteMeshIndices = null;

            //Generate random numeric characters for the unique filename.
            randomNumericCharacters = "";
            while (randomNumericCharacters.Length < numberOfRandomNumericCharacters) {
                string thisRandomAlphaCharacters = Guid.NewGuid().ToString("n");
                foreach (char c in thisRandomAlphaCharacters) {
                    if (c >= 48 && c <= 57)
                        randomNumericCharacters += c.ToString().ToLower();
                    if (randomNumericCharacters.Length >= numberOfRandomNumericCharacters)
                        break;
                }
            }
        }

        //Convert from Json.
        public static UserData getUserDataInstance(string userDataJson) {
            if (string.IsNullOrWhiteSpace(userDataJson))
                return new UserData();
            try {

                //Split the user data text into fields.
                string separator = ",".PadLeft(Convert.ToInt32(userDataJson.Substring(0, userDataJson.IndexOf(","))), ',');
                string[] fields = userDataJson.Split(new string[] { separator }, StringSplitOptions.None);
                int index = 1;

                //Create a user data instance from the first field.
                UserData userData = JsonUtility.FromJson<UserData>(fields[index++]);

                //Create sprite sheet data.
#if UNITY_2D_SPRITE_PACKAGE
                userData.spriteRects = new SpriteRect[Convert.ToInt32(fields[index++])];
                for (int i = 0; i < userData.spriteRects.Length; i++)
                    userData.spriteRects[i] = JsonUtility.FromJson<SpriteRect>(fields[index++]);
#else
                index += Convert.ToInt32(fields[index]) + 1;
#endif

                //Create texture importer data.
                userData.defaultTextureImporterPlatformSettings = JsonUtility.FromJson<TextureImporterPlatformSettings>(fields[index++]);
                userData.textureImporterPlatformSettings = new TextureImporterPlatformSettings[Convert.ToInt32(fields[index++])];
                for (int i = 0; i < userData.textureImporterPlatformSettings.Length; i++)
                    userData.textureImporterPlatformSettings[i] = JsonUtility.FromJson<TextureImporterPlatformSettings>(fields[index++]);

                //Secondary sprites.
                userData.secondarySpriteTextures = new SecondarySpriteTexture[Convert.ToInt32(fields[index++])];
                for (int i = 0; i < userData.secondarySpriteTextures.Length; i++)
                    userData.secondarySpriteTextures[i] = JsonUtility.FromJson<SecondarySpriteTexture>(fields[index++]);

                //Outlines.
#if UNITY_2D_SPRITE_PACKAGE
                userData.spriteOutlines = new List<Vector2[]>[userData.spriteRects.Length];
                for (int k = 0; k < userData.spriteOutlines.Length; k++) {
                    userData.spriteOutlines[k] = new List<Vector2[]>();
                    int outlineCount = Convert.ToInt32(fields[index++]);
                    for (int i = 0; i < outlineCount; i++) {
                        int vertexCount = Convert.ToInt32(fields[index++]);
                        Vector2[] vertices = new Vector2[vertexCount];
                        for (int j = 0; j < vertexCount; j++)
                            vertices[j] = new Vector2(Convert.ToSingle(fields[index++]), Convert.ToSingle(fields[index++]));
                        userData.spriteOutlines[k].Add(vertices);
                    }
                }
#else
                userData.spriteOutlines = new List<Vector2[]>[0];
#endif

                //Physics shapes.
#if UNITY_2D_SPRITE_PACKAGE
                userData.spritePhysicsShapes = new List<Vector2[]>[userData.spriteRects.Length];
                for (int k = 0; k < userData.spritePhysicsShapes.Length; k++) {
                    userData.spritePhysicsShapes[k] = new List<Vector2[]>();
                    int outlineCount = Convert.ToInt32(fields[index++]);
                    for (int i = 0; i < outlineCount; i++) {
                        int vertexCount = Convert.ToInt32(fields[index++]);
                        Vector2[] vertices = new Vector2[vertexCount];
                        for (int j = 0; j < vertexCount; j++)
                            vertices[j] = new Vector2(Convert.ToSingle(fields[index++]), Convert.ToSingle(fields[index++]));
                        userData.spritePhysicsShapes[k].Add(vertices);
                    }
                }
#else
                userData.spritePhysicsShapes = new List<Vector2[]>[0];
#endif

                //Bones.
#if UNITY_2D_SPRITE_PACKAGE
                userData.spriteBones = new List<SpriteBone>[userData.spriteRects.Length];
                for (int i = 0; i < userData.spriteBones.Length; i++) {
                    userData.spriteBones[i] = new List<SpriteBone>();
                    int spriteBoneCount = Convert.ToInt32(fields[index++]);
                    for (int j = 0; j < spriteBoneCount; j++)
                        userData.spriteBones[i].Add(JsonUtility.FromJson<SpriteBone>(fields[index++]));
                }
#else
                userData.spriteBones = new List<SpriteBone>[0];
#endif

                //Mesh.
#if UNITY_2D_SPRITE_PACKAGE
                userData.spriteMeshVertices = new Vertex2DMetaData[userData.spriteRects.Length][];
                userData.spriteMeshEdges = new Vector2Int[userData.spriteRects.Length][];
                userData.spriteMeshIndices = new int[userData.spriteRects.Length][];
                for (int i = 0; i < userData.spriteRects.Length; i++) {
                    int vertexCount = Convert.ToInt32(fields[index++]);
                    userData.spriteMeshVertices[i] = new Vertex2DMetaData[vertexCount];
                    for (int j = 0; j < vertexCount; j++)
                        userData.spriteMeshVertices[i][j] = JsonUtility.FromJson<Vertex2DMetaData>(fields[index++]);
                    int edgeCount = Convert.ToInt32(fields[index++]);
                    userData.spriteMeshEdges[i] = new Vector2Int[edgeCount];
                    for (int j = 0; j < edgeCount; j++)
                        userData.spriteMeshEdges[i][j] = JsonUtility.FromJson<Vector2Int>(fields[index++]);
                    int indexCount = Convert.ToInt32(fields[index++]);
                    userData.spriteMeshIndices[i] = new int[indexCount];
                    for (int j = 0; j < indexCount; j++)
                        userData.spriteMeshIndices[i][j] = Convert.ToInt32(fields[index++]);
                }
#else
                userData.spriteMeshEdges = new Vector2Int[0][];
                userData.spriteMeshIndices = new int[0][];
#endif

                //Perform any version updates.
                //if (userData.versionMajor == 1 && userData.versionMinor == 0 && userData.versionRelease == 0) { }

                //Return the user data instance.
                return userData;
            }
            catch {
                return new UserData();
            }
        }

        //Apply the user data settings to a PNG texture importer.
        public void applyToAssetImporter(TextureImporter textureImporter) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
            UnityEngine.Debug.Log($"User Data ({textureImporter.assetPath}) - applying user data to asset importer (sprites in sprite sheet: " +
                    $"{(spriteRects == null ? 0 : spriteRects.Length)}).");
#endif
            textureImporter.SetTextureSettings(textureImporterSettings);
            textureImporter.SetPlatformTextureSettings(defaultTextureImporterPlatformSettings);
            for (int i = 0; i < textureImporterPlatformSettings.Length; i++)
                textureImporter.SetPlatformTextureSettings(textureImporterPlatformSettings[i]);
            textureImporter.secondarySpriteTextures = secondarySpriteTextures;
#if UNITY_2D_SPRITE_PACKAGE
            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider spriteRectDataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
            spriteRectDataProvider.InitSpriteEditorDataProvider();
            spriteRectDataProvider.SetSpriteRects(spriteRects);
            ISpriteOutlineDataProvider outlineDataProvider = spriteRectDataProvider.GetDataProvider<ISpriteOutlineDataProvider>();
            for (int i = 0; i < spriteRects.Length; i++)
                outlineDataProvider.SetOutlines(spriteRects[i].spriteID, spriteOutlines[i]);
            ISpritePhysicsOutlineDataProvider physicsOutlineDataProvider = spriteRectDataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            for (int i = 0; i < spriteRects.Length; i++)
                physicsOutlineDataProvider.SetOutlines(spriteRects[i].spriteID, spritePhysicsShapes[i]);
            ISpriteBoneDataProvider boneDataProvider = spriteRectDataProvider.GetDataProvider<ISpriteBoneDataProvider>();
            for (int i = 0; i < spriteRects.Length; i++)
                boneDataProvider.SetBones(spriteRects[i].spriteID, spriteBones[i]);
            ISpriteMeshDataProvider meshDataProvider = spriteRectDataProvider.GetDataProvider<ISpriteMeshDataProvider>();
            for (int i = 0; i < spriteRects.Length; i++) {
                meshDataProvider.SetVertices(spriteRects[i].spriteID, spriteMeshVertices[i]);
                meshDataProvider.SetEdges(spriteRects[i].spriteID, spriteMeshEdges[i]);
                meshDataProvider.SetIndices(spriteRects[i].spriteID, spriteMeshIndices[i]);
            }
            spriteRectDataProvider.Apply();
#endif
        }

        //Convert to Json.
        public string getJSONStringFromUserData(TextureImporter textureImporter) {

            //Store everything from the texture importer in this user data instance.
            if (textureImporter != null) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                UnityEngine.Debug.Log($"User Data ({textureImporter.assetPath}) - getting JSON string from user data.");
#endif
                textureImporter.ReadTextureSettings(textureImporterSettings);
                defaultTextureImporterPlatformSettings = textureImporter.GetDefaultPlatformTextureSettings();
                List<TextureImporterPlatformSettings> textureImporterPlatformSettingsList = new List<TextureImporterPlatformSettings>();
                for (int i = 0; i < platforms.Length; i++) {
                    TextureImporterPlatformSettings thisTextureImporterPlatformSettings = textureImporter.GetPlatformTextureSettings(platforms[i]);
                    if (thisTextureImporterPlatformSettings.overridden)
                        textureImporterPlatformSettingsList.Add(thisTextureImporterPlatformSettings);
                }
                textureImporterPlatformSettings = textureImporterPlatformSettingsList.ToArray();
                secondarySpriteTextures = textureImporter.secondarySpriteTextures;

                //Create sprite sheet data.
#if UNITY_2D_SPRITE_PACKAGE
                SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
                factory.Init();
                ISpriteEditorDataProvider spriteRectDataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
                spriteRectDataProvider.InitSpriteEditorDataProvider();
                spriteRects = spriteRectDataProvider.GetSpriteRects();
                ISpriteOutlineDataProvider outlineDataProvider = spriteRectDataProvider.GetDataProvider<ISpriteOutlineDataProvider>();
                spriteOutlines = new List<Vector2[]>[spriteRects.Length];
                for (int i = 0; i < spriteRects.Length; i++)
                    spriteOutlines[i] = outlineDataProvider.GetOutlines(spriteRects[i].spriteID);
                ISpritePhysicsOutlineDataProvider spritePhysicsOutlineDataProvider =
                        spriteRectDataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
                spritePhysicsShapes = new List<Vector2[]>[spriteRects.Length];
                for (int i = 0; i < spriteRects.Length; i++)
                    spritePhysicsShapes[i] = spritePhysicsOutlineDataProvider.GetOutlines(spriteRects[i].spriteID);
                ISpriteBoneDataProvider spriteBoneDataProvider = spriteRectDataProvider.GetDataProvider<ISpriteBoneDataProvider>();
                spriteBones = new List<SpriteBone>[spriteRects.Length];
                for (int i = 0; i < spriteRects.Length; i++)
                    spriteBones[i] = spriteBoneDataProvider.GetBones(spriteRects[i].spriteID);
                ISpriteMeshDataProvider spriteMeshDataProvider = spriteRectDataProvider.GetDataProvider<ISpriteMeshDataProvider>();
                spriteMeshVertices = new Vertex2DMetaData[spriteRects.Length][];
                spriteMeshEdges = new Vector2Int[spriteRects.Length][];
                spriteMeshIndices = new int[spriteRects.Length][];
                for (int i = 0; i < spriteRects.Length; i++) {
                    spriteMeshVertices[i] = spriteMeshDataProvider.GetVertices(spriteRects[i].spriteID);
                    spriteMeshEdges[i] = spriteMeshDataProvider.GetEdges(spriteRects[i].spriteID);
                    spriteMeshIndices[i] = spriteMeshDataProvider.GetIndices(spriteRects[i].spriteID);
                }
#else
                spriteOutlines = new List<Vector2[]>[0];
                spritePhysicsShapes = new List<Vector2[]>[0];
                spriteBones = new List<SpriteBone>[0];
                spriteMeshEdges = new Vector2Int[0][];
                spriteMeshIndices = new int[0][];
#endif
            }
            else if (firstImport) {
#if GIMP_IMPORTER_ASSET_IMPORT_DEBUG
                UnityEngine.Debug.Log($"User Data - getting JSON string for first import (resetting).");
#endif
#if UNITY_2D_SPRITE_PACKAGE
                spriteRects = new SpriteRect[0];
#endif
                defaultTextureImporterPlatformSettings = new TextureImporterPlatformSettings();
                textureImporterPlatformSettings = new TextureImporterPlatformSettings[0];
                secondarySpriteTextures = new SecondarySpriteTexture[0];
                spriteOutlines = new List<Vector2[]>[0];
                spritePhysicsShapes = new List<Vector2[]>[0];
                spriteBones = new List<SpriteBone>[0];
#if UNITY_2D_SPRITE_PACKAGE
                spriteMeshVertices = new Vertex2DMetaData[0][];
#endif
                spriteMeshEdges = new Vector2Int[0][];
                spriteMeshIndices = new int[0][];
            }

            //Initialise the fields array and add a blank field that will contain the separator length followed by the serialization of this class.
            List<string> fields = new List<string>();
            fields.Add("");
            fields.Add(JsonUtility.ToJson(this));

            //Add sprite sheet serialization.
#if UNITY_2D_SPRITE_PACKAGE
            if (spriteRects == null || spriteRects.Length == 0)
                fields.Add("0");
            else {
                fields.Add(spriteRects.Length.ToString());
                for (int i = 0; i < spriteRects.Length; i++)
                    fields.Add(JsonUtility.ToJson(spriteRects[i]));
            }
#else
            fields.Add("0");
#endif

            //Add texture importer platform settings.
            fields.Add(JsonUtility.ToJson(defaultTextureImporterPlatformSettings));
            fields.Add(textureImporterPlatformSettings.Length.ToString());
            for (int i = 0; i < textureImporterPlatformSettings.Length; i++)
                fields.Add(JsonUtility.ToJson(textureImporterPlatformSettings[i]));

            //Add secondary sprite settings.
            fields.Add(secondarySpriteTextures.Length.ToString());
            for (int i = 0; i < secondarySpriteTextures.Length; i++)
                fields.Add(JsonUtility.ToJson(secondarySpriteTextures[i]));

            //Add sprite outlines.
            for (int i = 0; i < spriteOutlines.Length; i++) {
                fields.Add(spriteOutlines[i].Count.ToString());
                for (int j = 0; j < spriteOutlines[i].Count; j++) {
                    fields.Add(spriteOutlines[i][j].Length.ToString());
                    for (int k = 0; k < spriteOutlines[i][j].Length; k++) {
                        fields.Add(spriteOutlines[i][j][k].x.ToString());
                        fields.Add(spriteOutlines[i][j][k].y.ToString());
                    }
                }
            }

            //Add sprite physics shapes.
            for (int i = 0; i < spritePhysicsShapes.Length; i++) {
                fields.Add(spritePhysicsShapes[i].Count.ToString());
                for (int j = 0; j < spritePhysicsShapes[i].Count; j++) {
                    fields.Add(spritePhysicsShapes[i][j].Length.ToString());
                    for (int k = 0; k < spritePhysicsShapes[i][j].Length; k++) {
                        fields.Add(spritePhysicsShapes[i][j][k].x.ToString());
                        fields.Add(spritePhysicsShapes[i][j][k].y.ToString());
                    }
                }
            }

            //Add sprite bones.
            for (int i = 0; i < spriteBones.Length; i++) {
                fields.Add(spriteBones[i].Count.ToString());
                for (int j = 0; j < spriteBones[i].Count; j++)
                    fields.Add(JsonUtility.ToJson(spriteBones[i][j]));
            }

            //Add sprite mesh data.
#if UNITY_2D_SPRITE_PACKAGE
            for (int i = 0; i < spriteMeshVertices.Length; i++) {
                fields.Add(spriteMeshVertices[i].Length.ToString());
                for (int j = 0; j < spriteMeshVertices[i].Length; j++)
                    fields.Add(JsonUtility.ToJson(spriteMeshVertices[i][j]));
                fields.Add(spriteMeshEdges[i].Length.ToString());
                for (int j = 0; j < spriteMeshEdges[i].Length; j++)
                    fields.Add(JsonUtility.ToJson(spriteMeshEdges[i][j]));
                fields.Add(spriteMeshIndices[i].Length.ToString());
                for (int j = 0; j < spriteMeshIndices[i].Length; j++)
                    fields.Add(spriteMeshIndices[i][j].ToString());
            }
#endif

            //Work out how many commas are needed for the separator (the shortest string length that isn't present in any of the fields).
            string separator = ",";
            bool separatorOK = false;
            while (!separatorOK) {
                separatorOK = true;
                for (int i = 0; i < fields.Count; i++)
                    if (fields[i].Contains(separator)) {
                        separatorOK = false;
                        separator += ",";
                        break;
                    }
            }
            fields[0] = separator.Length.ToString();

            //Set the user data string and return it.
            StringBuilder jsonStringBuilder = new StringBuilder();
            for (int i = 0; i < fields.Count; i++) {
                if (i > 0)
                    jsonStringBuilder.Append(separator);
                jsonStringBuilder.Append(fields[i]);
            }
            return jsonStringBuilder.ToString();
        }
    }
}