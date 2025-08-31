namespace ImporterForGIMPImageFiles {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    internal static class LoadXCF {

        //Constants.
        const string fileHeader = "gimp xcf ";

        //Enums.
        enum ImagePropertyTypes {
            End = 0,
            ColourMap = 1,
            Compression = 17,
            Resolution = 19,
            Tattoo = 20,
            Parasites = 21,
            Unit = 22
        }
        enum LayerTypes {
            RGBColourWithoutAlpha = 0,
            RGBColourWithAlpha = 1,
            GreyscaleWithoutAlpha = 2,
            GreyscaleWithAlpha = 3,
            IndexedWithoutAlpha = 4,
            IndexedWithAlpha = 5
        }
        enum LayerPropertyTypes {
            End = 0,
            ColourMap = 1,
            ActiveLayer = 2,
            ActiveChannel = 3,
            Selection = 4,
            FloatingSelection = 5,
            Opacity = 6,
            Mode = 7,
            Visible = 8,
            Linked = 9,
            LockAlpha = 10,
            ApplyLayerMask = 11,
            EditMask = 12,
            ShowMask = 13,
            ShowMasked = 14,
            Offsets = 15,
            Colour = 16,
            Compression = 17,
            Guides = 18,
            Resolution = 19,
            Tattoo = 20,
            Parasites = 21,
            Unit = 22,
            Paths = 23,
            UserUnit = 24,
            Vectors = 25,
            TextLayerFlags = 26,
            OldSamplePoints = 27,
            LockContent = 28,
            GroupItem = 29,
            ItemPath = 30,
            GroupItemFlags = 31,
            LockPosition = 32,
            FloatOpacity = 33,
            ColourTag = 34,
            CompositeMode = 35,
            CompositeSpace = 36,
            BlendSpace = 37,
            FloatColour = 38,
            SamplePoints = 39
        }
        public enum Precision {
            _8BitLinearInteger = 100,
            _8BitGammaInteger = 150,
            _16BitLinearInteger = 200,
            _16BitGammaInteger = 250,
            _32BitLinearInteger = 300,
            _32BitGammaInteger = 350,
            _16BitLinearFloatingPoint = 500,
            _16BitGammaFloatingPoint = 550,
            _32BitLinearFloatingPoint = 600,
            _32BitGammaFloatingPoint = 650,
            _64BitLinearFloatingPoint = 700,
            _64BitGammaFloatingPoint = 750
        }
        public enum Compression {
            None = 0,
            RLEEncoding = 1,
            zLib = 2
        }
        public enum ColorMode {
            RGB = 0,
            Greyscale = 1,
            Indexed = 2
        }

        //Classes.
        public class LoadedTexture {
            public Texture2D texture;
            public int fileVersionNumber;
            public Precision precision;
            public Compression compression;
            public uint width, height;
            public ColorMode colourMode;
        }
        class Layer {
            public uint width, height;
            public LayerTypes type;
            public float opacity = 1;
            public bool visible = true;
            public bool applyLayerMask = false;
            public int offsetX = 0, offsetY = 0;
            public Blend.LayerMode mode = Blend.LayerMode.Normal;
            public Blend.LayerSpace blendSpace = Blend.LayerSpace.Auto;
            public Blend.LayerSpace compositeSpace = Blend.LayerSpace.Auto;
            public CompositeBlend.LayerCompositeMode compositeMode = CompositeBlend.LayerCompositeMode.Union;
            public bool isGroup = false;
            public int groupDepth = 0;
            public long hierarchyStructurePointer = 0;
            public long maskPointer = 0;
            public string name = "";
            public bool floatingSelection = false;
            public bool editLayerMask = false;
            public bool storeLayer = false;
            public bool mergeLayer = false;
        }

        //Load an XCF file and return a texture.
        public static LoadedTexture load(byte[] XCFData, bool widthAndHeightOnly) {
            LoadedTexture loadedTexture = new LoadedTexture();
            int filePosition = 0;
            bool compressionSet = false;
            Color[] colourMap = null;

            //Check for the header.
            if (!charactersMatch(XCFData, filePosition, fileHeader))
                throw new ImporterForGIMPImageFilesException(6, "XCF file header is invalid.");
            filePosition += fileHeader.Length;

            //Get the file version number.
            loadedTexture.fileVersionNumber = -1;
            if (charactersMatch(XCFData, filePosition, "file"))
                loadedTexture.fileVersionNumber = 0;
            else if (charactersMatch(XCFData, filePosition, "v")) {
                string versionString = ((char) XCFData[filePosition + 1]).ToString() + ((char) XCFData[filePosition + 2]).ToString() +
                        ((char) XCFData[filePosition + 3]).ToString();
                int.TryParse(versionString, out loadedTexture.fileVersionNumber);
            }
            if (loadedTexture.fileVersionNumber < 0)
                throw new ImporterForGIMPImageFilesException(7, "Could not read XCF file version number.");
            filePosition += 4;

            //Check the version tag ends with a zero byte.
            if (XCFData[filePosition++] != 0)
                throw new ImporterForGIMPImageFilesException(8, "XCF file version tag doesn't end with a zero byte.");

            //Get the image width and height.
            loadedTexture.width = ReadBytes.readUInt32(XCFData, ref filePosition);
            if (loadedTexture.width <= 0)
                throw new ImporterForGIMPImageFilesException(9, $"XCF file image has an invalid width ({loadedTexture.width}).");
            loadedTexture.height = ReadBytes.readUInt32(XCFData, ref filePosition);
            if (loadedTexture.height <= 0)
                throw new ImporterForGIMPImageFilesException(10, $"XCF file image has an invalid height ({loadedTexture.height}).");

            //Return now if only the width and height are required.
            if (widthAndHeightOnly)
                return loadedTexture;

            //Get the colour mode.
            uint colourMode = ReadBytes.readUInt32(XCFData, ref filePosition);
            if (!enumContainsValue(typeof(ColorMode), colourMode))
                throw new ImporterForGIMPImageFilesException(11, $"XCF file has an invalid colour mode ({colourMode}).");
            loadedTexture.colourMode = (ColorMode) colourMode;

            //Get the precision.
            if (loadedTexture.fileVersionNumber >= 7) {
                uint precision = ReadBytes.readUInt32(XCFData, ref filePosition);
                if (!enumContainsValue(typeof(Precision), precision))
                    throw new ImporterForGIMPImageFilesException(12, $"XCF file (version {loadedTexture.fileVersionNumber}) has an invalid precision ({precision}).");
                loadedTexture.precision = (Precision) precision;
            }
            else if (loadedTexture.fileVersionNumber >= 5) {
                uint precision = ReadBytes.readUInt32(XCFData, ref filePosition);
                if (precision == 100)
                    loadedTexture.precision = Precision._8BitLinearInteger;
                else if (precision == 150)
                    loadedTexture.precision = Precision._8BitGammaInteger;
                else if (precision == 200)
                    loadedTexture.precision = Precision._16BitLinearInteger;
                else if (precision == 250)
                    loadedTexture.precision = Precision._16BitGammaInteger;
                else if (precision == 300)
                    loadedTexture.precision = Precision._32BitLinearInteger;
                else if (precision == 350)
                    loadedTexture.precision = Precision._32BitGammaInteger;
                else if (precision == 400)
                    loadedTexture.precision = Precision._16BitLinearFloatingPoint;
                else if (precision == 450)
                    loadedTexture.precision = Precision._16BitGammaFloatingPoint;
                else if (precision == 500)
                    loadedTexture.precision = Precision._32BitLinearFloatingPoint;
                else if (precision == 550)
                    loadedTexture.precision = Precision._32BitGammaFloatingPoint;
                else
                    throw new ImporterForGIMPImageFilesException(13, $"XCF file (version {loadedTexture.fileVersionNumber}) has an invalid precision ({precision}).");
            }
            else if (loadedTexture.fileVersionNumber >= 4) {
                uint precision = ReadBytes.readUInt32(XCFData, ref filePosition);
                if (precision == 0)
                    loadedTexture.precision = Precision._8BitGammaInteger;
                else if (precision == 1)
                    loadedTexture.precision = Precision._16BitGammaInteger;
                else if (precision == 2)
                    loadedTexture.precision = Precision._32BitLinearInteger;
                else if (precision == 3)
                    loadedTexture.precision = Precision._16BitLinearFloatingPoint;
                else if (precision == 4)
                    loadedTexture.precision = Precision._32BitLinearFloatingPoint;
                else
                    throw new ImporterForGIMPImageFilesException(14, $"XCF file (version {loadedTexture.fileVersionNumber}) has an invalid precision ({precision}).");
            }
            else
                loadedTexture.precision = Precision._8BitGammaInteger;

            //Get the image properties.
            while (true) {
                uint propertyTypeCode = ReadBytes.readUInt32(XCFData, ref filePosition);

                //If the image property is known, get its type and skip the payload integer. This should be ignored if the length is known.
                ImagePropertyTypes propertyType = (ImagePropertyTypes) propertyTypeCode;
                uint payload = ReadBytes.readUInt32(XCFData, ref filePosition);

                //Compression.
                if (propertyType == ImagePropertyTypes.Compression) {
                    byte compression = XCFData[filePosition++];
                    if (!enumContainsValue(typeof(Compression), compression))
                        throw new ImporterForGIMPImageFilesException(16, $"XCF file compression not found ({compression}).");
                    loadedTexture.compression = (Compression) compression;
                    compressionSet = true;
                }

                //Colour map.
                else if (propertyType == ImagePropertyTypes.ColourMap) {
                    uint colourCount = ReadBytes.readUInt32(XCFData, ref filePosition);
                    if (colourCount > 256)
                        throw new ImporterForGIMPImageFilesException(54, $"XCF file colour map contains {colourCount} colours but only a maximum of 256 are supported.");
                    colourMap = new Color[colourCount];
                    for (int i = 0; i < colourCount; i++)
                        colourMap[i] = new Color(
                            XCFData[filePosition++] / 255f,
                            XCFData[filePosition++] / 255f,
                            XCFData[filePosition++] / 255f,
                            1f
                        );
                }

                //End.
                else if (propertyType == ImagePropertyTypes.End)
                    break;

                //For all unknown image property types, just skip past the payload.
                else
                    filePosition += (int) payload;
            }

            //Throw an exception if the tile compression has not been set. We need to know this in order to read layer data.
            if (!compressionSet)
                throw new ImporterForGIMPImageFilesException(17, "XCF file compression not set in image properties.");

            //Create the array of colours for the texture.
            int imagePixelCount = (int) loadedTexture.width * (int) loadedTexture.height;
            Color[] pixels = new Color[imagePixelCount];
            Color[] storedPixels = new Color[imagePixelCount];

            //Read the layers and add them to a list. Layers keep going while there is a non-zero pointer.
            List<Layer> layers = new List<Layer>();
            long layerStructurePointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref filePosition);
            int doNotAddLayersWithADepthGreaterThan = int.MaxValue;
            while (layerStructurePointer > 0) {
                uint layerWidth = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);
                uint layerHeight = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);
                uint layerTypeValue = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);
                if (!enumContainsValue(typeof(LayerTypes), layerTypeValue))
                    throw new ImporterForGIMPImageFilesException(18, $"XCF file invalid layer type ({layerTypeValue}).");
                string name = ReadBytes.readString(XCFData, ref layerStructurePointer);
                Layer layer = new Layer {
                    width = layerWidth,
                    height = layerHeight,
                    type = (LayerTypes) layerTypeValue,
                    name = name
                };

                //Get the layer properties.
                while (true) {

                    //Get the property type code.
                    uint propertyTypeCode = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);

                    //If the image property is known, get its type and skip the payload integer. This should be ignored if the length is known.
                    LayerPropertyTypes propertyType = (LayerPropertyTypes) propertyTypeCode;
                    uint payload = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);

                    //Opacity.
                    if (propertyType == LayerPropertyTypes.Opacity) {
                        uint opacity = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);
                        if (opacity > 255)
                            throw new ImporterForGIMPImageFilesException(20, $"XCF file invalid integer opacity value ({opacity}).");
                        layer.opacity = (float) opacity / 255;
                    }

                    //Float opacity.
                    else if (propertyType == LayerPropertyTypes.FloatOpacity) {
                        float opacity = ReadBytes.readFloat(XCFData, ref layerStructurePointer);
                        if (opacity < -Maths.epsilon || opacity > Maths.epsilon + 1f)
                            throw new ImporterForGIMPImageFilesException(21, $"XCF file invalid float opacity value ({opacity}).");
                        layer.opacity = Mathf.Clamp01(opacity);
                    }

                    //Visible.
                    else if (propertyType == LayerPropertyTypes.Visible) {
                        uint layerVisibleValue = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);
                        if (layerVisibleValue != 0 && layerVisibleValue != 1)
                            throw new ImporterForGIMPImageFilesException(22, $"Invalid layer visible flag ({layerVisibleValue}).");
                        layer.visible = layerVisibleValue == 1;
                    }

                    //Apply layer mask.
                    else if (propertyType == LayerPropertyTypes.ApplyLayerMask) {
                        uint applyLayerMaskValue = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);
                        if (applyLayerMaskValue != 0 && applyLayerMaskValue != 1)
                            throw new ImporterForGIMPImageFilesException(23, $"XCF file invalid apply layer mask flag ({applyLayerMaskValue}).");
                        layer.applyLayerMask = applyLayerMaskValue == 1;
                    }

                    //Offsets.
                    else if (propertyType == LayerPropertyTypes.Offsets) {
                        layer.offsetX = ReadBytes.readInt32(XCFData, ref layerStructurePointer);
                        layer.offsetY = ReadBytes.readInt32(XCFData, ref layerStructurePointer);
                    }

                    //Mode.
                    else if (propertyType == LayerPropertyTypes.Mode) {
                        uint modeValue = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);
                        if (!enumContainsValue(typeof(Blend.LayerMode), modeValue))
                            throw new ImporterForGIMPImageFilesException(24, $"XCF file invalid layer mode ({modeValue}).");
                        layer.mode = (Blend.LayerMode) modeValue;
                    }

                    //Blend space.
                    else if (propertyType == LayerPropertyTypes.BlendSpace) {
                        uint blendSpaceValue = (uint) Math.Abs(ReadBytes.readInt32(XCFData, ref layerStructurePointer));
                        if (!enumContainsValue(typeof(Blend.LayerSpace), blendSpaceValue))
                            throw new ImporterForGIMPImageFilesException(25, $"XCF file invalid blend space ({blendSpaceValue}).");
                        layer.blendSpace = (Blend.LayerSpace) blendSpaceValue;
                    }

                    //Composite space.
                    else if (propertyType == LayerPropertyTypes.CompositeSpace) {
                        uint compositeSpaceValue = (uint) Math.Abs(ReadBytes.readInt32(XCFData, ref layerStructurePointer));
                        if (!enumContainsValue(typeof(Blend.LayerSpace), compositeSpaceValue))
                            throw new ImporterForGIMPImageFilesException(26, $"XCF file invalid composite space ({compositeSpaceValue}).");
                        layer.compositeSpace = (Blend.LayerSpace) compositeSpaceValue;
                    }

                    //Composite mode.
                    else if (propertyType == LayerPropertyTypes.CompositeMode) {
                        uint compositeModeValue = (uint) Math.Abs(ReadBytes.readInt32(XCFData, ref layerStructurePointer));
                        if (!enumContainsValue(typeof(CompositeBlend.LayerCompositeMode), compositeModeValue))
                            throw new ImporterForGIMPImageFilesException(27, $"XCF file invalid composite mode ({compositeModeValue}),");
                        layer.compositeMode = (CompositeBlend.LayerCompositeMode) compositeModeValue;
                    }

                    //Group item.
                    else if (propertyType == LayerPropertyTypes.GroupItem)
                        layer.isGroup = true;

                    //Item path, specifying the layer is in a group.
                    else if (propertyType == LayerPropertyTypes.ItemPath) {
                        layerStructurePointer += payload;
                        layer.groupDepth = ((int) payload / 4) - 1;
                    }

                    //Floating selection.
                    else if (propertyType == LayerPropertyTypes.FloatingSelection) {
                        layerStructurePointer += payload;
                        layer.floatingSelection = true;
                    }

                    //Edit layer mask.
                    else if (propertyType == LayerPropertyTypes.EditMask) {
                        uint editLayerMaskValue = ReadBytes.readUInt32(XCFData, ref layerStructurePointer);
                        if (editLayerMaskValue != 0 && editLayerMaskValue != 1)
                            throw new ImporterForGIMPImageFilesException(51, $"XCF file invalid edit layer mask flag ({editLayerMaskValue}).");
                        layer.editLayerMask = editLayerMaskValue == 1;
                    }

                    //End.
                    else if (propertyType == LayerPropertyTypes.End)
                        break;

                    //For all unknown layer property types, just skip past the payload.
                    else
                        layerStructurePointer += payload;
                }

                //Get the layer's hierarchy structure pointer.
                layer.hierarchyStructurePointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref layerStructurePointer);

                //Get the layer's mask pointer.
                layer.maskPointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref layerStructurePointer);

                //Add the layer to the start of the layers list unless it is invisible or a parent layer group is invisible.                
                if (doNotAddLayersWithADepthGreaterThan < int.MaxValue && layer.groupDepth <= doNotAddLayersWithADepthGreaterThan)
                    doNotAddLayersWithADepthGreaterThan = int.MaxValue;
                bool addLayer = layer.visible && layer.groupDepth < doNotAddLayersWithADepthGreaterThan;
                if (doNotAddLayersWithADepthGreaterThan == int.MaxValue && !layer.visible && layer.isGroup)
                    doNotAddLayersWithADepthGreaterThan = layer.groupDepth;
                if (addLayer)
                    layers.Insert(0, layer);
                else if (layers.Count > 0 && layers[0].floatingSelection)
                    layers.RemoveAt(0);

                //Move onto the next layer.
                layerStructurePointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref filePosition);
            }

            //Get rid of groups that are covered by layer groups, and layer groups that are set to "pass through" so are covered by groups.
            for (int i = layers.Count - 1; i >= 0; i--)
                if (layers[i].isGroup && layers[i].mode == Blend.LayerMode.PassThrough) {
                    layers[i].mergeLayer = true;
                    int j = i - 1;
                    while (j >= 0 && layers[j].groupDepth > layers[i].groupDepth)
                        j--;
                    if (j >= 0)
                        layers[j].storeLayer = true;
                }
            int deleteLayerWhileDepthAbove = -1;
            for (int i = layers.Count - 1; i >= 0; i--) {
                if (deleteLayerWhileDepthAbove > -1) {
                    if (layers[i].groupDepth > deleteLayerWhileDepthAbove)
                        layers.RemoveAt(i);
                    else
                        deleteLayerWhileDepthAbove = -1;
                }
                if (deleteLayerWhileDepthAbove == -1 && layers[i].isGroup && layers[i].mode != Blend.LayerMode.PassThrough)
                    deleteLayerWhileDepthAbove = layers[i].groupDepth;
            }

            //Determine whether we are in linear or gamma colour space.
            bool isLinear;
            if (loadedTexture.precision == Precision._8BitLinearInteger ||
                    loadedTexture.precision == Precision._16BitLinearInteger ||
                    loadedTexture.precision == Precision._32BitLinearInteger ||
                    loadedTexture.precision == Precision._16BitLinearFloatingPoint ||
                    loadedTexture.precision == Precision._32BitLinearFloatingPoint ||
                    loadedTexture.precision == Precision._64BitLinearFloatingPoint)
                isLinear = true;
            else if (loadedTexture.precision == Precision._8BitGammaInteger ||
                    loadedTexture.precision == Precision._16BitGammaInteger ||
                    loadedTexture.precision == Precision._32BitGammaInteger ||
                    loadedTexture.precision == Precision._16BitGammaFloatingPoint ||
                    loadedTexture.precision == Precision._32BitGammaFloatingPoint ||
                    loadedTexture.precision == Precision._64BitGammaFloatingPoint)
                isLinear = false;
            else
                throw new ImporterForGIMPImageFilesException(33, $"Not known whether precision \"{loadedTexture.precision}\" is linear or gamma.");

            //Get the precision type and number of bytes per float.
            Hierarchy.PrecisionType precisionType;
            int bytesPerFloat;
            if (loadedTexture.precision == Precision._8BitGammaInteger || loadedTexture.precision == Precision._8BitLinearInteger) {
                precisionType = Hierarchy.PrecisionType.Integer;
                bytesPerFloat = 1;
            }
            else if (loadedTexture.precision == Precision._16BitGammaInteger || loadedTexture.precision == Precision._16BitLinearInteger) {
                precisionType = Hierarchy.PrecisionType.Integer;
                bytesPerFloat = 2;
            }
            else if (loadedTexture.precision == Precision._32BitGammaInteger || loadedTexture.precision == Precision._32BitLinearInteger) {
                precisionType = Hierarchy.PrecisionType.Integer;
                bytesPerFloat = 4;
            }
            else if (loadedTexture.precision == Precision._16BitGammaFloatingPoint ||
                    loadedTexture.precision == Precision._16BitLinearFloatingPoint) {
                precisionType = Hierarchy.PrecisionType.Float;
                bytesPerFloat = 2;
            }
            else if (loadedTexture.precision == Precision._32BitGammaFloatingPoint ||
                    loadedTexture.precision == Precision._32BitLinearFloatingPoint) {
                precisionType = Hierarchy.PrecisionType.Float;
                bytesPerFloat = 4;
            }
            else if (loadedTexture.precision == Precision._64BitGammaFloatingPoint ||
                    loadedTexture.precision == Precision._64BitLinearFloatingPoint) {
                precisionType = Hierarchy.PrecisionType.Float;
                bytesPerFloat = 8;
            }
            else
                throw new ImporterForGIMPImageFilesException(32, $"Precision \"{loadedTexture.precision}\" not supported for reading tile data.");

            //If there are any floating layers, we need arrays to store their colour, or in the case of masks, alpha values.
            float[] layerMaskAlphaValues = null;
            for (int m = 0; m < layers.Count; m++)
                if (layers[m].maskPointer != 0) {
                    layerMaskAlphaValues = new float[loadedTexture.width * loadedTexture.height];
                    break;
                }
            Color[] layerFloatingColours = null;
            float[] layerFloatingMaskAlphaValues = null;

            //Loop over the layers.
            Layer floatingLayer = null;
            int tileCountX, tileCountY;
            for (int m = -2; m < layers.Count; m++) {
                bool applyFloatingLayer = false;
                bool applyFloatingLayerMask = false;
                Layer layer = null;
                Layer layerToApplyFloatingSelectionTo = null;

                //-2 is a special case - this is the floating layer, if there is one.
                if (m == -2) {
                    for (int i = 0; i < layers.Count; i++)
                        if (layers[i].floatingSelection) {
                            floatingLayer = layers[i];
                            if (i == 0)
                                throw new ImporterForGIMPImageFilesException(52, "Bottom layer cannot be a floating layer.");
                            else if (!layers[i - 1].editLayerMask) {
                                layerToApplyFloatingSelectionTo = layers[i - 1];
                                layerFloatingColours = new Color[loadedTexture.width * loadedTexture.height];
                                for (int j = 0; j < layerFloatingColours.Length; j++)
                                    layerFloatingColours[j] = Color.clear;
                                layer = layers[i];
                                break;
                            }
                        }
                    if (layer == null)
                        continue;
                }

                //-1 is another special case - this is the floating layer mask, if there is one.
                else if (m == -1) {
                    for (int i = 1; i < layers.Count; i++)
                        if (layers[i].floatingSelection) {
                            floatingLayer = layers[i];
                            if (layers[i - 1].editLayerMask) {
                                layerToApplyFloatingSelectionTo = layers[i - 1];
                                layerFloatingMaskAlphaValues = new float[loadedTexture.width * loadedTexture.height];
                                for (int j = 0; j < layerFloatingMaskAlphaValues.Length; j++)
                                    layerFloatingMaskAlphaValues[j] = -1f;
                                layer = layers[i];
                                break;
                            }
                        }
                    if (layer == null)
                        continue;
                }

                //Because the floating layer mask has been processed above, skip it in the list of layers.
                else if (m > 0 && layers[m].floatingSelection)
                    continue;

                //Otherwise just look at the current layer.
                else {
                    layer = layers[m];
                    applyFloatingLayerMask = m < layers.Count - 1 && layer.maskPointer != 0 && layer.editLayerMask && layers[m + 1].floatingSelection;
                    applyFloatingLayer = !applyFloatingLayerMask && m < layers.Count - 1 && layers[m + 1].floatingSelection;
                }

                //The bottom layer should always be normal blend mode and union composite mode.
                if (m == 0) {
                    layer.mode = Blend.LayerMode.Normal;
                    layer.compositeMode = CompositeBlend.LayerCompositeMode.Union;
                }

                //Convert layer spaces of auto into the default for the blend mode.
                ConvertAutoBlendSpace.convertAutoBlendSpace(ref layer.blendSpace, layer.mode);
                ConvertAutoCompositeSpace.convertAutoCompositeSpace(ref layer.compositeSpace, layer.mode);

                //Calculate colour mode, whether alpha is present and hence the number of channels.
                ColorMode layerColourMode;
                int channels;
                if (layer.type == LayerTypes.RGBColourWithAlpha || layer.type == LayerTypes.RGBColourWithoutAlpha) {
                    layerColourMode = ColorMode.RGB;
                    channels = 3;
                }
                else if (layer.type == LayerTypes.GreyscaleWithAlpha || layer.type == LayerTypes.GreyscaleWithoutAlpha) {
                    layerColourMode = ColorMode.Greyscale;
                    channels = 1;
                }
                else if (layer.type == LayerTypes.IndexedWithAlpha || layer.type == LayerTypes.IndexedWithoutAlpha) {
                    layerColourMode = ColorMode.Indexed;
                    channels = 1;
                }
                else
                    throw new ImporterForGIMPImageFilesException(48, $"Colour mode cannot be determined from layer type {layer.type}.");
                bool hasAlpha;
                if (layer.type == LayerTypes.RGBColourWithAlpha || layer.type == LayerTypes.GreyscaleWithAlpha ||
                        layer.type == LayerTypes.IndexedWithAlpha)
                    hasAlpha = true;
                else if (layer.type == LayerTypes.RGBColourWithoutAlpha || layer.type == LayerTypes.GreyscaleWithoutAlpha ||
                        layer.type == LayerTypes.IndexedWithoutAlpha)
                    hasAlpha = false;
                else
                    throw new ImporterForGIMPImageFilesException(49, $"Whether layer has alpha cannot be determined from layer type {layer.type}.");
                if (hasAlpha)
                    channels++;

                //Apply the layer mask if present.
                if (layer.maskPointer != 0 && layer.applyLayerMask) {
                    for (int i = 0; i < layerMaskAlphaValues.Length; i++)
                        layerMaskAlphaValues[i] = 1;
                    long maskPointer = layer.maskPointer;
                    uint channelWidth = ReadBytes.readUInt32(XCFData, ref maskPointer);
                    if (channelWidth != layer.width)
                        throw new ImporterForGIMPImageFilesException(38, $"Mask channel width ({channelWidth}) does not match layer width ({layer.width})");
                    uint channelHeight = ReadBytes.readUInt32(XCFData, ref maskPointer);
                    if (channelHeight != layer.height)
                        throw new ImporterForGIMPImageFilesException(39, $"Mask channel height ({channelHeight}) does not match layer height ({layer.height})");
                    ReadBytes.readString(XCFData, ref maskPointer);
                    while (true) {
                        uint propertyTypeCode = ReadBytes.readUInt32(XCFData, ref maskPointer);

                        //If the image property is known, get its type and skip the payload integer. This should be ignored if the length is known.
                        LayerPropertyTypes propertyType = (LayerPropertyTypes) propertyTypeCode;
                        uint payload = ReadBytes.readUInt32(XCFData, ref maskPointer);

                        //End.
                        if (propertyType == LayerPropertyTypes.End)
                            break;

                        //For all unknown mask property types, just skip past the payload.
                        else
                            maskPointer += (int) payload;
                    }

                    //Get the layer's hierarchy structure pointer.
                    long maskHierarchyStructurePointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref maskPointer);

                    //Get the mask hierarchy width and height and check it matches the image width and height.
                    uint maskHierarchyWidth = ReadBytes.readUInt32(XCFData, ref maskHierarchyStructurePointer);
                    if (maskHierarchyWidth != layer.width)
                        throw new ImporterForGIMPImageFilesException(44, $"Mask hierarchy width ({maskHierarchyWidth}) does not match layer width ({layer.width})");
                    uint maskHierarchyHeight = ReadBytes.readUInt32(XCFData, ref maskHierarchyStructurePointer);
                    if (maskHierarchyHeight != layer.height)
                        throw new ImporterForGIMPImageFilesException(45, $"Mask hierarchy height ({maskHierarchyHeight}) does not match layer height ({layer.height})");

                    //Get the bytes per pixel and validate that it is always 1.
                    uint bytesPerMaskPixel = ReadBytes.readUInt32(XCFData, ref maskHierarchyStructurePointer);
                    if (bytesPerMaskPixel != bytesPerFloat)
                        throw new ImporterForGIMPImageFilesException(15, $"Bytes per mask pixel ({bytesPerMaskPixel}) is not the expected value ({bytesPerFloat}).");

                    //Get the pointer to the hierarchy data.
                    long maskLevelStructurePointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData,
                            ref maskHierarchyStructurePointer);

                    //Ensure the width and height match the image.
                    uint maskWidth = ReadBytes.readUInt32(XCFData, ref maskLevelStructurePointer);
                    if (maskWidth != layer.width)
                        throw new ImporterForGIMPImageFilesException(46, $"Mask width ({maskWidth}) does not match layer width ({layer.width})");
                    uint maskHeight = ReadBytes.readUInt32(XCFData, ref maskLevelStructurePointer);
                    if (maskHeight != layer.height)
                        throw new ImporterForGIMPImageFilesException(47, $"Mask height ({maskHeight}) does not match layer height ({layer.height})");

                    //Work out the number of tiles, loop over them and get their offsets.
                    tileCountX = Mathf.CeilToInt((float) maskWidth / 64);
                    tileCountY = Mathf.CeilToInt((float) maskHeight / 64);
                    for (int j = 0; j < tileCountY; j++) {
                        int tileOffsetY = (int) (loadedTexture.height - 1) - (layer.offsetY + (j * 64));
                        int tileHeight = j < tileCountY - 1 || maskHeight % 64 == 0 ? 64 : ((int) maskHeight % 64);
                        for (int i = 0; i < tileCountX; i++) {

                            //Get the tile offsets, size and pixel count.
                            int tileOffsetX = layer.offsetX + (i * 64);
                            int tileWidth = i < tileCountX - 1 || maskWidth % 64 == 0 ? 64 : ((int) maskWidth % 64);
                            int pixelCount = tileWidth * tileHeight;

                            //If the tile is completely off the image, skip it.
                            if (tileOffsetX + tileWidth < 0 || tileOffsetX >= loadedTexture.width || tileOffsetY < 0 ||
                                    tileOffsetY - tileHeight >= loadedTexture.height) {
                                maskLevelStructurePointer += loadedTexture.fileVersionNumber >= 11 ? 8 : 4;
                                continue;
                            }

                            //Get a pointer to the tile data.
                            long maskTilePointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref maskLevelStructurePointer);

                            //Read pixel data.
                            float[][] maskHierarchyData = Hierarchy.readHierarchy(XCFData, maskTilePointer, 1, pixelCount, loadedTexture.compression,
                                    precisionType, bytesPerFloat);

                            //Apply the mask.
                            int index = -1;
                            for (int l = 0; l < tileHeight; l++)
                                for (int k = 0; k < tileWidth; k++) {
                                    index++;
                                    int X = tileOffsetX + k, Y = tileOffsetY - l;
                                    if (X >= 0 && X < loadedTexture.width && Y >= 0 && Y < loadedTexture.height)
                                        layerMaskAlphaValues[(Y * (int) loadedTexture.width) + X] *= maskHierarchyData[0][index];
                                }
                        }
                    }

                    //Check there is a zero pointer at the end of the mask level structure.
                    if (ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref maskLevelStructurePointer) != 0)
                        throw new ImporterForGIMPImageFilesException(40, "XCF file expected zero pointer at end of mask level structure.");
                }

                //Ready the hierarchy size and check it matches the layer size.
                uint hierarchyWidth = ReadBytes.readUInt32(XCFData, ref layer.hierarchyStructurePointer);
                uint hierarchyHeight = ReadBytes.readUInt32(XCFData, ref layer.hierarchyStructurePointer);
                if (hierarchyWidth != layer.width || hierarchyHeight != layer.height)
                    throw new ImporterForGIMPImageFilesException(28,
                            $"Layer and hierarchy sizes different (layer={layer.width}x{layer.height}, hierarchy={hierarchyWidth}x{hierarchyHeight})");

                //Get the number of bytes per pixel.
                uint bytesPerPixel = ReadBytes.readUInt32(XCFData, ref layer.hierarchyStructurePointer);

                //Get the pointer to the level structure and loop over the tiles.
                long levelStructurePointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref layer.hierarchyStructurePointer);
                uint levelWidth = ReadBytes.readUInt32(XCFData, ref levelStructurePointer);
                uint levelHeight = ReadBytes.readUInt32(XCFData, ref levelStructurePointer);

                //Hierarchy and level sizes must be identical.
                if (hierarchyWidth != levelWidth || hierarchyHeight != levelHeight)
                    throw new ImporterForGIMPImageFilesException(29, $"Hierarchy and level sizes are different (hierarchy: {hierarchyWidth} x {hierarchyHeight}; " +
                            $"level: {levelWidth} x {levelHeight})");

                //Clear the image outside of the layer if clipping to the layer.
                if (layer.compositeMode == CompositeBlend.LayerCompositeMode.ClipToLayer)
                    for (int j = 0; j < loadedTexture.height; j++) {
                        int Y = (int) loadedTexture.height - j - 1;
                        for (int i = 0; i < loadedTexture.width; i++)
                            if (i < layer.offsetX || i >= layer.offsetX + layer.width || Y < layer.offsetY || Y >= layer.offsetY + layer.height)
                                pixels[(j * (int) loadedTexture.width) + i] = Color.clear;
                    }

                //If applying the layer mask with a composite mode of union when clipping to a layer, apply the areas outside of the layer.
                if (applyFloatingLayer && layer.compositeMode == CompositeBlend.LayerCompositeMode.ClipToLayer &&
                        (floatingLayer.compositeMode == CompositeBlend.LayerCompositeMode.Union ||
                        floatingLayer.compositeMode == CompositeBlend.LayerCompositeMode.ClipToLayer))
                    for (int j = 0; j < loadedTexture.height; j++) {
                        int Y = (int) loadedTexture.height - j - 1;
                        for (int i = 0; i < loadedTexture.width; i++) {
                            int pixelArrayIndex = (j * (int) loadedTexture.width) + i;
                            if (i < layer.offsetX || i >= layer.offsetX + layer.width || Y < layer.offsetY || Y >= layer.offsetY + layer.height)
                                pixels[pixelArrayIndex] = CompositeBlend.compositeBlend(
                                    layerFloatingColours[pixelArrayIndex],
                                    pixels[pixelArrayIndex],
                                    Blend.blend(
                                        layerFloatingColours[pixelArrayIndex],
                                        pixels[pixelArrayIndex],
                                        floatingLayer.mode,
                                        floatingLayer.blendSpace,
                                        floatingLayer.compositeSpace,
                                        isLinear,
                                        true
                                    ),
                                    layer.opacity,
                                    floatingLayer.mode,
                                    floatingLayer.compositeMode,
                                    floatingLayer.compositeSpace,
                                    floatingLayer.blendSpace,
                                    isLinear,
                                    false,
                                    0,
                                    i,
                                    (int) loadedTexture.height - j - 1,
                                    true
                                );
                        }
                    }

                //Work out the number of tiles, loop over them and get their offsets.
                tileCountX = Mathf.CeilToInt((float) levelWidth / 64);
                tileCountY = Mathf.CeilToInt((float) levelHeight / 64);
                for (int j = 0; j < tileCountY; j++) {
                    int tileOffsetY = (int) (loadedTexture.height - 1) - (layer.offsetY + (j * 64));
                    int tileHeight = j < tileCountY - 1 || levelHeight % 64 == 0 ? 64 : ((int) levelHeight % 64);
                    for (int i = 0; i < tileCountX; i++) {

                        //Get the tile offsets, size and pixel count.
                        int tileOffsetX = layer.offsetX + (i * 64);
                        int tileWidth = i < tileCountX - 1 || levelWidth % 64 == 0 ? 64 : ((int) levelWidth % 64);
                        int pixelCount = tileWidth * tileHeight;

                        //If the tile is completely off the image, skip it.
                        if (tileOffsetX + tileWidth < 0 || tileOffsetX >= loadedTexture.width || tileOffsetY < 0 ||
                                tileOffsetY - tileHeight >= loadedTexture.height) {
                            levelStructurePointer += loadedTexture.fileVersionNumber >= 11 ? 8 : 4;
                            continue;
                        }

                        //Get a pointer to the tile data.
                        long tilePointer = ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref levelStructurePointer);

                        //Validate the number of bytes per pixel is correct.
                        int expectedBytesPerPixel = bytesPerFloat * channels;
                        if (expectedBytesPerPixel != bytesPerPixel)
                            throw new ImporterForGIMPImageFilesException(19, $"Bytes per pixel ({bytesPerPixel}) is not the expected value ({expectedBytesPerPixel}).");

                        //Read pixel data.
                        float[][] hierarchyData = Hierarchy.readHierarchy(XCFData, tilePointer, channels, pixelCount, loadedTexture.compression,
                                precisionType, bytesPerFloat);

                        //Apply the layer.
                        int index = -1;
                        for (int l = 0; l < tileHeight; l++)
                            for (int k = 0; k < tileWidth; k++) {
                                index++;
                                int X = tileOffsetX + k, Y = tileOffsetY - l;
                                if (X >= 0 && X < loadedTexture.width && Y >= 0 && Y < loadedTexture.height) {
                                    float alphaLevel = hasAlpha ? hierarchyData[channels - 1][index] : 1;
                                    int pixelArrayIndex = (Y * (int) loadedTexture.width) + X;

                                    //Handle the floating layer.
                                    if (m == -2) {
                                        if (layerColourMode == ColorMode.RGB)
                                            layerFloatingColours[pixelArrayIndex] = new Color(hierarchyData[0][index], hierarchyData[1][index],
                                                    hierarchyData[2][index], alphaLevel);
                                        else if (layerColourMode == ColorMode.Greyscale)
                                            layerFloatingColours[pixelArrayIndex] = new Color(hierarchyData[0][index], hierarchyData[0][index],
                                                    hierarchyData[0][index], alphaLevel);
                                        else if (layerColourMode == ColorMode.Indexed) {
                                            layerFloatingColours[pixelArrayIndex] = colourMap[Mathf.RoundToInt(hierarchyData[0][index] * 255)];
                                            layerFloatingColours[pixelArrayIndex].a = alphaLevel;
                                        }
                                        else
                                            throw new ImporterForGIMPImageFilesException(55,
                                                    $"Cannot calculate source pixel for colour mode {colourMode} in floating selection.");
                                    }

                                    //Handle the floating layer mask.
                                    else if (m == -1)
                                        layerFloatingMaskAlphaValues[pixelArrayIndex] = hierarchyData[0][index];

                                    //Handle all other layers.
                                    else {

                                        //Get the source pixel.
                                        Color source;
                                        if (layer.mergeLayer)
                                            source = pixels[pixelArrayIndex];
                                        else if (layerColourMode == ColorMode.RGB)
                                            source = new Color(hierarchyData[0][index], hierarchyData[1][index], hierarchyData[2][index], alphaLevel);
                                        else if (layerColourMode == ColorMode.Greyscale)
                                            source = new Color(hierarchyData[0][index], hierarchyData[0][index], hierarchyData[0][index], alphaLevel);
                                        else if (layerColourMode == ColorMode.Indexed) {
                                            source = colourMap[Mathf.RoundToInt(hierarchyData[0][index] * 255)];
                                            source.a = alphaLevel;
                                        }
                                        else
                                            throw new ImporterForGIMPImageFilesException(50, $"Cannot calculate source pixel for colour mode {colourMode}.");

                                        //If the floating layer is to be applied to this layer, do that first.
                                        if (applyFloatingLayer)
                                            source = CompositeBlend.compositeBlend(
                                                layerFloatingColours[pixelArrayIndex],
                                                source,
                                                Blend.blend(
                                                    layerFloatingColours[pixelArrayIndex],
                                                    source,
                                                    floatingLayer.mode,
                                                    floatingLayer.blendSpace,
                                                    floatingLayer.compositeSpace,
                                                    isLinear,
                                                    true
                                                ),
                                                1,
                                                floatingLayer.mode,
                                                floatingLayer.compositeMode,
                                                floatingLayer.compositeSpace,
                                                floatingLayer.blendSpace,
                                                isLinear,
                                                false,
                                                0,
                                                X,
                                                (int) loadedTexture.height - Y - 1,
                                                true
                                            );

                                        //Get the destination pixel.
                                        Color destination = layer.mergeLayer ? storedPixels[pixelArrayIndex] : pixels[pixelArrayIndex];

                                        //Convert the source and destination pixels colour spaces now if they are the same for both blend and composite blend.
                                        bool convertSourceAndDestinationColourSpaces = true;
                                        if (layer.blendSpace == layer.compositeSpace) {
                                            if (isLinear) {
                                                if (layer.blendSpace == Blend.LayerSpace.RGBPerceptual) {
                                                    ColourSpace.linearToGamma(ref source);
                                                    ColourSpace.linearToGamma(ref destination);
                                                }
                                                else if (layer.blendSpace == Blend.LayerSpace.LAB) {
                                                    ColourSpace.linearToLAB(ref source);
                                                    ColourSpace.linearToLAB(ref destination);
                                                }
                                            }
                                            else {
                                                if (layer.blendSpace == Blend.LayerSpace.RGBLinear) {
                                                    ColourSpace.gammaToLinear(ref source);
                                                    ColourSpace.gammaToLinear(ref destination);
                                                }
                                                else if (layer.blendSpace == Blend.LayerSpace.LAB) {
                                                    ColourSpace.gammaToLAB(ref source);
                                                    ColourSpace.gammaToLAB(ref destination);
                                                }
                                            }
                                            convertSourceAndDestinationColourSpaces = false;
                                        }

                                        //Blend.
                                        pixels[pixelArrayIndex] = CompositeBlend.compositeBlend(
                                            source,
                                            destination,
                                            Blend.blend(
                                                source,
                                                destination,
                                                layer.mode,
                                                layer.blendSpace,
                                                layer.compositeSpace,
                                                isLinear,
                                                convertSourceAndDestinationColourSpaces
                                            ),
                                            layer.opacity,
                                            layer.mode,
                                            layer.compositeMode,
                                            layer.compositeSpace, 
                                            layer.blendSpace,
                                            isLinear,
                                            layer.maskPointer != 0 && layer.applyLayerMask,
                                            layer.maskPointer != 0 && layer.applyLayerMask ?
                                                (applyFloatingLayerMask && layerFloatingMaskAlphaValues[pixelArrayIndex] > -0.5f ?
                                                layerFloatingMaskAlphaValues[pixelArrayIndex] : layerMaskAlphaValues[pixelArrayIndex]) : 0,
                                            X,
                                            (int) loadedTexture.height - Y - 1,
                                            convertSourceAndDestinationColourSpaces
                                        );

                                        //Apply the layer mask to the floating selection.
                                        if (layer.floatingSelection && layerToApplyFloatingSelectionTo.applyLayerMask &&
                                                layerToApplyFloatingSelectionTo.maskPointer != 0)
                                            pixels[pixelArrayIndex].a *= layerMaskAlphaValues[pixelArrayIndex];

                                        //Red, green and blue values that are greater than 32767 or flagged as not a number should be zero.
                                        if (float.IsNaN(pixels[pixelArrayIndex].r))
                                            pixels[pixelArrayIndex].r = 0;
                                        else if (pixels[pixelArrayIndex].r > 32767)
                                            pixels[pixelArrayIndex].r = 1;
                                        if (float.IsNaN(pixels[pixelArrayIndex].g))
                                            pixels[pixelArrayIndex].g = 0;
                                        else if (pixels[pixelArrayIndex].g > 32767)
                                            pixels[pixelArrayIndex].g = 1;
                                        if (float.IsNaN(pixels[pixelArrayIndex].b))
                                            pixels[pixelArrayIndex].b = 0;
                                        else if (pixels[pixelArrayIndex].b > 32767)
                                            pixels[pixelArrayIndex].b = 1;
                                        if (float.IsNaN(pixels[pixelArrayIndex].a))
                                            pixels[pixelArrayIndex].a = 0;
                                        else if (pixels[pixelArrayIndex].a > 32767)
                                            pixels[pixelArrayIndex].a = 1;
                                        pixels[pixelArrayIndex].a = Mathf.Clamp01(pixels[pixelArrayIndex].a);
                                    }
                                }
                            }
                    }
                }

                //Check there is a zero pointer at the end of the level structure.
                if (ReadBytes.readPointer(loadedTexture.fileVersionNumber, XCFData, ref levelStructurePointer) != 0)
                    throw new ImporterForGIMPImageFilesException(30, "XCF file expected zero pointer at end of level structure.");

                //Store the pixels if this layer requires it.
                if (m >= 0 && layer.storeLayer)
                    Array.Copy(pixels, storedPixels, pixels.Length);
            }

            //For indexed colour mode, convert the resulting pixels to the nearest colour in the colour map
            if (loadedTexture.colourMode == ColorMode.Indexed) {
                for (int i = 0; i < pixels.Length; i++) {

                    //Round the RGB values.
                    float roundedPixelR = Mathf.RoundToInt(pixels[i].r * 255) / 255f;
                    float roundedPixelG = Mathf.RoundToInt(pixels[i].g * 255) / 255f;
                    float roundedPixelB = Mathf.RoundToInt(pixels[i].b * 255) / 255f;

                    //For indexed images, pixels are either fully transparent or fully opaque.
                    if (pixels[i].a < 0.5f)
                        pixels[i].a = 0;
                    else
                        pixels[i].a = 1;

                    //Calculate the nearest colour in the colour map and assign it to the pixel.
                    float shortestDistance = float.MaxValue;
                    int shortestIndex = -1;
                    for (int j = 0; j < colourMap.Length; j++) {
                        float redDifference = Mathf.Abs(colourMap[j].r - roundedPixelR);
                        float greenDifference = Mathf.Abs(colourMap[j].g - roundedPixelG);
                        float blueDifference = Mathf.Abs(colourMap[j].b - roundedPixelB);
                        float distance = (redDifference * redDifference) + (greenDifference * greenDifference) + (blueDifference * blueDifference);
                        if (distance <= shortestDistance) {
                            shortestDistance = distance;
                            shortestIndex = j;
                        }
                    }
                    pixels[i].r = colourMap[shortestIndex].r;
                    pixels[i].g = colourMap[shortestIndex].g;
                    pixels[i].b = colourMap[shortestIndex].b;
                }
            }

            //For linear images, convert to gamma before creating the final texture.
            if (isLinear)
                for (int i = 0; i < pixels.Length; i++)
                    ColourSpace.linearToGamma(ref pixels[i]);

            //Create the texture and assign the pixels.
            loadedTexture.texture = new Texture2D((int) loadedTexture.width, (int) loadedTexture.height);
            loadedTexture.texture.SetPixels(pixels);
            loadedTexture.texture.Apply();

            //Return the loaded texture.
            return loadedTexture;
        }

        //Return whether characters in a byte array match a given string.
        static bool charactersMatch(byte[] bytes, int offset, string s) {
            foreach (char c in s)
                if (bytes[offset++] != c)
                    return false;
            return true;
        }

        //Return whether an enumerated type contains a specific value.
        static bool enumContainsValue(Type enumType, uint value) => Enum.GetValues(enumType).Cast<int>().ToList().Contains((int) value);
    }
}