namespace ImporterForGIMPImageFiles {
    using System;
    using UnityEngine;

    internal static class Hierarchy {

        //Enums.
        public enum PrecisionType {
            Integer = 0,
            Float = 1
        }

        //Read hierarchy data.
        public static float[][] readHierarchy(byte[] data, long dataPointer, int channels, int pixelCount, LoadXCF.Compression compression,
                PrecisionType precisionType, int bytesPerFloat) {

            //Initialise the return array.
            float[][] hierarchyData = new float[channels][];
            for (int i = 0; i < channels; i++)
                hierarchyData[i] = new float[pixelCount];

            //Set up a temporary array of conversion bytes for RLE encoding for floating point precisions.
            byte[][] conversionBytes = null;
            if (compression == LoadXCF.Compression.RLEEncoding && precisionType == PrecisionType.Float) {
                if (bytesPerFloat == 2) {
                    conversionBytes = new byte[pixelCount][];
                    for (int k = 0; k < pixelCount; k++)
                        conversionBytes[k] = new byte[1];
                }
                else if (bytesPerFloat == 4) {
                    conversionBytes = new byte[pixelCount][];
                    for (int k = 0; k < pixelCount; k++)
                        conversionBytes[k] = new byte[4];
                }
                else if (bytesPerFloat == 8) {
                    conversionBytes = new byte[pixelCount][];
                    for (int k = 0; k < pixelCount; k++)
                        conversionBytes[k] = new byte[8];
                }
            }

            //No compression.
            if (compression == LoadXCF.Compression.None)
                for (int k = 0; k < pixelCount; k++) {
                    if (precisionType == PrecisionType.Integer && bytesPerFloat == 1)
                        for (int l = 0; l < channels; l++)
                            hierarchyData[l][k] = data[dataPointer++] / 255f;
                    else if (precisionType == PrecisionType.Integer && bytesPerFloat == 2)
                        for (int l = 0; l < channels; l++)
                            hierarchyData[l][k] = ReadBytes.readUInt16(data, ref dataPointer) / 65536f;
                    else if (precisionType == PrecisionType.Integer && bytesPerFloat == 4)
                        for (int l = 0; l < channels; l++)
                            hierarchyData[l][k] = ReadBytes.readUInt32(data, ref dataPointer) / 4294967296f;
                    else if (precisionType == PrecisionType.Float && bytesPerFloat == 2)
                        for (int l = 0; l < channels; l++)
                            hierarchyData[l][k] = Mathf.HalfToFloat((ushort) ((data[dataPointer++] * 256) + data[dataPointer++]));
                    else if (precisionType == PrecisionType.Float && bytesPerFloat == 4)
                        for (int l = 0; l < channels; l++) {
                            hierarchyData[l][k] = BitConverter.ToSingle(new byte[] {
                                data[dataPointer + 3],
                                data[dataPointer + 2],
                                data[dataPointer + 1],
                                data[dataPointer]
                            }, 0);
                            dataPointer += 4;
                        }
                    else if (precisionType == PrecisionType.Float && bytesPerFloat == 8) {
                        for (int l = 0; l < channels; l++) {
                            hierarchyData[l][k] = (float) BitConverter.Int64BitsToDouble(BitConverter.ToInt64(new byte[] {
                                data[dataPointer + 7],
                                data[dataPointer + 6],
                                data[dataPointer + 5],
                                data[dataPointer + 4],
                                data[dataPointer + 3],
                                data[dataPointer + 2],
                                data[dataPointer + 1],
                                data[dataPointer]
                            }, 0));
                            dataPointer += 8;
                        }
                    }
                }

            //RLE encoding compression.
            else if (compression == LoadXCF.Compression.RLEEncoding)
                for (int m = 0; m < channels; m++) {
                    for (int k = 0; k < bytesPerFloat; k++) {
                        byte[] decoded = Decoding.decodeRLEStream(data, ref dataPointer, pixelCount);
                        if (precisionType == PrecisionType.Integer) {
                            if (k == 0 && bytesPerFloat == 1)
                                for (int l = 0; l < pixelCount; l++)
                                    hierarchyData[m][l] = decoded[l] / 255f;
                            else if (k == 0 && bytesPerFloat == 2)
                                for (int l = 0; l < pixelCount; l++)
                                    hierarchyData[m][l] = (decoded[l] * 256) / 65535f;
                            else if (k == 1 && bytesPerFloat == 2)
                                for (int l = 0; l < pixelCount; l++)
                                    hierarchyData[m][l] += decoded[l] / 65535f;
                            else if (k == 0 && bytesPerFloat == 4)
                                for (int l = 0; l < pixelCount; l++)
                                    hierarchyData[m][l] = (uint) (decoded[l] * 16777216) / 4294967296f;
                            else if (k == 1 && bytesPerFloat == 4)
                                for (int l = 0; l < pixelCount; l++)
                                    hierarchyData[m][l] += (decoded[l] * 65536) / 4294967296f;
                            else if (k == 2 && bytesPerFloat == 4)
                                for (int l = 0; l < pixelCount; l++)
                                    hierarchyData[m][l] += (decoded[l] * 256) / 4294967296f;
                            else if (k == 3 && bytesPerFloat == 4)
                                for (int l = 0; l < pixelCount; l++)
                                    hierarchyData[m][l] += decoded[l] / 4294967296f;
                            else
                                throw new ImporterForGIMPImageFilesException(36,
                                        $"RLE encoding for integer precision at {bytesPerFloat} byte {(bytesPerFloat == 1 ? "" : "s")} not supported");
                        }
                        else if (precisionType == PrecisionType.Float) {
                            if (k == 0 && bytesPerFloat == 2)
                                for (int l = 0; l < pixelCount; l++)
                                    conversionBytes[l][0] = decoded[l];
                            else if (k == 1 && bytesPerFloat == 2)
                                for (int l = 0; l < pixelCount; l++)
                                    hierarchyData[m][l] = Mathf.HalfToFloat((ushort) ((conversionBytes[l][0] * 256) + decoded[l]));
                            else if (bytesPerFloat == 4) {
                                for (int l = 0; l < pixelCount; l++)
                                    conversionBytes[l][3 - k] = decoded[l];
                                if (k == 3 && bytesPerFloat == 4) {
                                    for (int l = 0; l < pixelCount; l++)
                                        hierarchyData[m][l] = BitConverter.ToSingle(conversionBytes[l], 0);
                                }
                            }
                            else if (bytesPerFloat == 8) {
                                for (int l = 0; l < pixelCount; l++)
                                    conversionBytes[l][7 - k] = decoded[l];
                                if (k == 7 && bytesPerFloat == 8) {
                                    for (int l = 0; l < pixelCount; l++)
                                        hierarchyData[m][l] = (float) BitConverter.Int64BitsToDouble(BitConverter.ToInt64(conversionBytes[l], 0));
                                }
                            }
                            else
                                throw new ImporterForGIMPImageFilesException(37,
                                        $"RLE encoding for float precision at {bytesPerFloat} byte {(bytesPerFloat == 1 ? "" : "s")} not supported");
                        }
                        else
                            throw new ImporterForGIMPImageFilesException(34, $"RLE encoding for precision type {precisionType} not supported");
                    }
                }

            //ZLib compression.
            else if (compression == LoadXCF.Compression.zLib) {
                dataPointer += 2;
                byte[] decoded = Decoding.decodeZlibStream(data, ref dataPointer, pixelCount * channels * bytesPerFloat);
                int decodedIndex = 0;
                for (int k = 0; k < pixelCount; k++) {
                    if (precisionType == PrecisionType.Integer && bytesPerFloat == 1)
                        for (int l = 0; l < channels; l++)
                            hierarchyData[l][k] = decoded[decodedIndex++] / 255f;
                    else if (precisionType == PrecisionType.Integer && bytesPerFloat == 2)
                        for (int l = 0; l < channels; l++)
                            hierarchyData[l][k] = ReadBytes.readUInt16(decoded, ref decodedIndex) / 65536f;
                    else if (precisionType == PrecisionType.Integer && bytesPerFloat == 4)
                        for (int l = 0; l < channels; l++)
                            hierarchyData[l][k] = ReadBytes.readUInt32(decoded, ref decodedIndex) / 4294967296f;
                    else if (precisionType == PrecisionType.Float && bytesPerFloat == 2)
                        for (int l = 0; l < channels; l++)
                            hierarchyData[l][k] = Mathf.HalfToFloat((ushort) ((decoded[decodedIndex++] * 256) + decoded[decodedIndex++]));
                    else if (precisionType == PrecisionType.Float && bytesPerFloat == 4)
                        for (int l = 0; l < channels; l++) {
                            hierarchyData[l][k] = BitConverter.ToSingle(new byte[] {
                                decoded[decodedIndex + 3],
                                decoded[decodedIndex + 2],
                                decoded[decodedIndex + 1],
                                decoded[decodedIndex]
                            }, 0);
                            decodedIndex += 4;
                        }
                    else if (precisionType == PrecisionType.Float && bytesPerFloat == 8)
                        for (int l = 0; l < channels; l++) {
                            hierarchyData[l][k] = (float) BitConverter.Int64BitsToDouble(BitConverter.ToInt64(new byte[] {
                                decoded[decodedIndex + 7],
                                decoded[decodedIndex + 6],
                                decoded[decodedIndex + 5],
                                decoded[decodedIndex + 4],
                                decoded[decodedIndex + 3],
                                decoded[decodedIndex + 2],
                                decoded[decodedIndex + 1],
                                decoded[decodedIndex]
                            }, 0));
                            decodedIndex += 8;
                        }
                    else
                        throw new ImporterForGIMPImageFilesException(35,
                            $"zLib encoding for precision type {precisionType}, {bytesPerFloat} byte{(bytesPerFloat == 1 ? "" : "s")} per float not supported");
                }
            }

            //Throw an exception if the compression is unsupported.
            else
                throw new ImporterForGIMPImageFilesException(43, $"Compression {compression} unsupported.");

            //Return the hierarchy data.
            return hierarchyData;
        }
    }
}