namespace ImporterForGIMPImageFiles {
    using UnityEngine;

    internal static class CompositeBlend {

        //Enums.
        public enum LayerCompositeMode {
            Union = 1,
            ClipToBackdrop = 2,
            ClipToLayer = 3,
            Intersection = 4
        }

        //Determine whether the layer mode is subtractive.
        static bool blendModeIsSubtractive(Blend.LayerMode layerMode) =>
            layerMode == Blend.LayerMode.ColorErase ||
            layerMode == Blend.LayerMode.ColorErase_Legacy ||
            layerMode == Blend.LayerMode.Erase ||
            layerMode == Blend.LayerMode.Split;

        //Apply the composite blend.
        public static Color compositeBlend(Color source, Color destination, Color blended, float opacity, Blend.LayerMode layerMode,
                LayerCompositeMode layerCompositeMode, Blend.LayerSpace layerCompositeSpace, Blend.LayerSpace layerBlendSpace, bool isLinear, bool hasMask,
                float maskAlpha, int x, int y, bool convertSourceAndDestinationColourSpaces) {

            //Apply gamma correction depending on the composite layer space.
            if (isLinear) {
                if (layerCompositeSpace == Blend.LayerSpace.RGBPerceptual) {
                    if (convertSourceAndDestinationColourSpaces) {
                        ColourSpace.linearToGamma(ref source);
                        ColourSpace.linearToGamma(ref destination);
                    }
                    if (layerBlendSpace != Blend.LayerSpace.RGBPerceptual)
                        ColourSpace.linearToGamma(ref blended);
                }
                else if (layerCompositeSpace == Blend.LayerSpace.LAB) {
                    if (convertSourceAndDestinationColourSpaces) {
                        ColourSpace.linearToLAB(ref source);
                        ColourSpace.linearToLAB(ref destination);
                    }
                    if (layerBlendSpace != Blend.LayerSpace.LAB)
                        ColourSpace.linearToLAB(ref blended);
                }
            }
            else {
                if (layerCompositeSpace == Blend.LayerSpace.RGBLinear) {
                    if (convertSourceAndDestinationColourSpaces) {
                        ColourSpace.gammaToLinear(ref source);
                        ColourSpace.gammaToLinear(ref destination);
                    }
                    if (layerBlendSpace != Blend.LayerSpace.RGBLinear)
                        ColourSpace.gammaToLinear(ref blended);
                }
                else if (layerCompositeSpace == Blend.LayerSpace.LAB) {
                    if (convertSourceAndDestinationColourSpaces) {
                        ColourSpace.gammaToLAB(ref source);
                        ColourSpace.gammaToLAB(ref destination);
                    }
                    if (layerBlendSpace != Blend.LayerSpace.LAB)
                        ColourSpace.gammaToLAB(ref blended);
                }
            }

            //Call the relevant blend function depending on the layer mode.
            if (layerMode == Blend.LayerMode.Normal || layerMode == Blend.LayerMode.Normal_Legacy)
                blended = normalCompositeBlend(source, destination, opacity, layerCompositeMode, hasMask, maskAlpha);
            else if (layerMode == Blend.LayerMode.PassThrough)
                blended = passThroughCompositeBlend(source, destination, opacity, hasMask, maskAlpha);
            else if (layerMode == Blend.LayerMode.Split)
                blended = splitCompositeBlend(source, destination, opacity, layerCompositeMode, hasMask, maskAlpha);
            else if (layerMode == Blend.LayerMode.Erase)
                blended = eraseCompositeBlend(source, destination, opacity, layerCompositeMode, hasMask, maskAlpha);
            else if (layerMode == Blend.LayerMode.Merge)
                blended = mergeCompositeBlend(source, destination, opacity, layerCompositeMode, hasMask, maskAlpha);
            else if (layerMode == Blend.LayerMode.Dissolve)
                blended = dissolveCompositeBlend(source, destination, opacity, layerCompositeMode, hasMask, maskAlpha, x, y);
            else
                blended = defaultCompositeBlend(source, destination, blended, opacity, blendModeIsSubtractive(layerMode), layerCompositeMode, hasMask,
                        maskAlpha);          

            //Apply gamma correction depending on the composite layer space.
            if (isLinear) {
                if (layerCompositeSpace == Blend.LayerSpace.RGBPerceptual)
                    ColourSpace.gammaToLinear(ref blended);
                else if (layerCompositeSpace == Blend.LayerSpace.LAB)
                    ColourSpace.LABToLinear(ref blended);
            }
            else {
                if (layerCompositeSpace == Blend.LayerSpace.RGBLinear)
                    ColourSpace.linearToGamma(ref blended);
                else if (layerCompositeSpace == Blend.LayerSpace.LAB)
                    ColourSpace.LABToGamma(ref blended);
            }

            //Return the pixel colour.
            return blended;
        }

        //Default composite blending.
        static Color defaultCompositeBlend(Color source, Color destination, Color blended, float opacity, bool isSubtractive,
                LayerCompositeMode layerCompositeMode, bool hasMask, float maskAlpha) {

            //Union.
            if (layerCompositeMode == LayerCompositeMode.Union) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                if (isSubtractive) {
                    float newAlpha = destination.a + layerAlpha - (2.0f - blended.a) * destination.a * layerAlpha;
                    if (layerAlpha < Maths.epsilon || newAlpha < Maths.epsilon) {
                        blended.r = destination.r;
                        blended.g = destination.g;
                        blended.b = destination.b;
                    }
                    else if (destination.a < Maths.epsilon) {
                        blended.r = source.r;
                        blended.g = source.g;
                        blended.b = source.b;
                    }
                    else {
                        float ratio = destination.a / newAlpha;
                        float layerCoefficient = 1.0f / destination.a - 1.0f;
                        blended.r = ratio * (layerAlpha * (blended.a * blended.r + layerCoefficient * source.r - destination.r) + destination.r);
                        blended.g = ratio * (layerAlpha * (blended.a * blended.g + layerCoefficient * source.g - destination.g) + destination.g);
                        blended.b = ratio * (layerAlpha * (blended.a * blended.b + layerCoefficient * source.b - destination.b) + destination.b);
                    }
                    blended.a = newAlpha;
                }
                else {
                    float newAlpha = layerAlpha + (1.0f - layerAlpha) * destination.a;
                    if (layerAlpha < Maths.epsilon || newAlpha < Maths.epsilon) {
                        blended.r = destination.r;
                        blended.g = destination.g;
                        blended.b = destination.b;
                    }
                    else if (destination.a < Maths.epsilon) {
                        blended.r = source.r;
                        blended.g = source.g;
                        blended.b = source.b;
                    }
                    else {
                        float ratio = layerAlpha / newAlpha;
                        blended.r = ratio * (destination.a * (blended.r - source.r) + source.r - destination.r) + destination.r;
                        blended.g = ratio * (destination.a * (blended.g - source.g) + source.g - destination.g) + destination.g;
                        blended.b = ratio * (destination.a * (blended.b - source.b) + source.b - destination.b) + destination.b;
                    }
                    blended.a = newAlpha;
                }
            }

            //Clip to backdrop.
            else if (layerCompositeMode == LayerCompositeMode.ClipToBackdrop) {
                if (isSubtractive) {
                    float layerAlpha = source.a * opacity;
                    if (hasMask)
                        layerAlpha *= maskAlpha;
                    blended.a *= layerAlpha;
                    float newAlpha = 1.0f - layerAlpha + blended.a;
                    if (destination.a < Maths.epsilon || blended.a < Maths.epsilon) {
                        blended.r = destination.r;
                        blended.g = destination.g;
                        blended.b = destination.b;
                    }
                    else {
                        float ratio = blended.a / newAlpha;
                        blended.r = blended.r * ratio + destination.r * (1.0f - ratio);
                        blended.g = blended.g * ratio + destination.g * (1.0f - ratio);
                        blended.b = blended.b * ratio + destination.b * (1.0f - ratio);
                    }
                    blended.a = newAlpha * destination.a;
                }
                else {
                    float layerAlpha = blended.a * opacity;
                    if (hasMask)
                        layerAlpha *= maskAlpha;
                    if (destination.a < Maths.epsilon || layerAlpha < Maths.epsilon) {
                        blended.r = destination.r;
                        blended.g = destination.g;
                        blended.b = destination.b;
                    }
                    else {
                        blended.r = blended.r * layerAlpha + destination.r * (1f - layerAlpha);
                        blended.g = blended.g * layerAlpha + destination.g * (1f - layerAlpha);
                        blended.b = blended.b * layerAlpha + destination.b * (1f - layerAlpha);
                    }
                    blended.a = destination.a;
                }
            }

            //Clip to layer.
            else if (layerCompositeMode == LayerCompositeMode.ClipToLayer) {
                if (isSubtractive) {
                    float layerAlpha = source.a * opacity;
                    if (hasMask)
                        layerAlpha *= maskAlpha;
                    blended.a *= destination.a;
                    float newAlpha = 1.0f - destination.a + blended.a;
                    if (layerAlpha < Maths.epsilon) {
                        blended.r = destination.r;
                        blended.g = destination.g;
                        blended.b = destination.b;
                    }
                    else if (destination.a < Maths.epsilon) {
                        blended.r = source.r;
                        blended.g = source.g;
                        blended.b = source.b;
                    }
                    else {
                        float ratio = blended.a / newAlpha;
                        blended.r = blended.r * ratio + source.r * (1.0f - ratio);
                        blended.g = blended.g * ratio + source.g * (1.0f - ratio);
                        blended.b = blended.b * ratio + source.b * (1.0f - ratio);
                    }
                    blended.a = newAlpha * layerAlpha;
                }
                else {
                    float layerAlpha = source.a * opacity;
                    if (hasMask)
                        layerAlpha *= maskAlpha;
                    if (layerAlpha < Maths.epsilon) {
                        blended.r = destination.r;
                        blended.g = destination.g;
                        blended.b = destination.b;
                    }
                    else if (destination.a < Maths.epsilon) {
                        blended.r = source.r;
                        blended.g = source.g;
                        blended.b = source.b;
                    }
                    else {
                        blended.r = blended.r * destination.a + source.r * (1f - destination.a);
                        blended.g = blended.g * destination.a + source.g * (1f - destination.a);
                        blended.b = blended.b * destination.a + source.b * (1f - destination.a);
                    }
                    blended.a = layerAlpha;
                }
            }

            //Intersection.
            else if (layerCompositeMode == LayerCompositeMode.Intersection) {
                if (isSubtractive) {
                    float newAlpha = destination.a * source.a * blended.a * opacity;
                    if (hasMask)
                        newAlpha *= maskAlpha;
                    if (newAlpha < Maths.epsilon) {
                        blended.r = destination.r;
                        blended.g = destination.g;
                        blended.b = destination.b;
                    }
                    blended.a = newAlpha;
                }
                else {
                    float newAlpha = destination.a * blended.a * opacity;
                    if (hasMask)
                        newAlpha *= maskAlpha;
                    if (newAlpha < Maths.epsilon) {
                        blended.r = destination.r;
                        blended.g = destination.g;
                        blended.b = destination.b;
                    }
                    blended.a = newAlpha;
                }
            }

            //Throw an exception if the layer composite mode is not supported at the current time.
            else
                throw new ImporterForGIMPImageFilesException(4, $"Layer composite mode {layerCompositeMode} not supported for default composite blending.");

            //Return the pixel colour.
            return blended;
        }

        //Composite blending for the normal layer mode.
        static Color normalCompositeBlend(Color source, Color destination, float opacity, LayerCompositeMode layerCompositeMode, bool hasMask,
                float maskAlpha) {
            Color blended;

            //Union.
            if (layerCompositeMode == LayerCompositeMode.Union) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                blended.a = layerAlpha + destination.a - layerAlpha * destination.a;
                if (blended.a > Maths.epsilon) {
                    float layerWeight = layerAlpha / blended.a;
                    float inWeight = 1.0f - layerWeight;
                    blended.r = source.r * layerWeight + destination.r * inWeight;
                    blended.g = source.g * layerWeight + destination.g * inWeight;
                    blended.b = source.b * layerWeight + destination.b * inWeight;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
            }

            //Clip to backdrop.
            else if (layerCompositeMode == LayerCompositeMode.ClipToBackdrop) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                blended.a = source.a;
                if (blended.a > Maths.epsilon) {
                    blended.r = destination.r + (source.r - destination.r) * layerAlpha;
                    blended.g = destination.g + (source.g - destination.g) * layerAlpha;
                    blended.b = destination.b + (source.b - destination.b) * layerAlpha;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
            }

            //Clip to layer.
            else if (layerCompositeMode == LayerCompositeMode.ClipToLayer) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                blended.a = layerAlpha;
                if (blended.a > Maths.epsilon) {
                    blended.r = source.r;
                    blended.g = source.g;
                    blended.b = source.b;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
            }

            //Intersection.
            else if (layerCompositeMode == LayerCompositeMode.Intersection) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                blended.a = destination.a * layerAlpha;
                if (blended.a > Maths.epsilon) {
                    blended.r = source.r;
                    blended.g = source.g;
                    blended.b = source.b;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
            }

            //Throw an exception if the layer composite mode is not supported at the current time.
            else
                throw new ImporterForGIMPImageFilesException(53, $"Layer composite mode {layerCompositeMode} not supported for split composite blending.");

            //Return the pixel colour.
            return blended;
        }

        //Composite blending for the pass through layer mode.
        static Color passThroughCompositeBlend(Color source, Color destination, float opacity, bool hasMask, float maskAlpha) {
            Color blended = Color.clear;
            if (hasMask)
                opacity *= maskAlpha;
            blended.a = (source.a - destination.a) * opacity + destination.a;
            float ratio = opacity;
            if (ratio > Maths.epsilon)
                ratio *= source.a / blended.a;
            blended.r = (source.r - destination.r) * ratio + destination.r;
            blended.g = (source.g - destination.g) * ratio + destination.g;
            blended.b = (source.b - destination.b) * ratio + destination.b;
         
            //Return the pixel colour.
            return blended;
        }

        //Composite blending for the split layer mode.
        static Color splitCompositeBlend(Color source, Color destination, float opacity, LayerCompositeMode layerCompositeMode, bool hasMask, float maskAlpha) {
            Color blended;

            //Union.
            if (layerCompositeMode == LayerCompositeMode.Union) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                if (layerAlpha <= destination.a) {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                    blended.a = destination.a - layerAlpha;
                }
                else {
                    blended.r = source.r;
                    blended.g = source.g;
                    blended.b = source.b;
                    blended.a = layerAlpha - destination.a;
                }
            }

            //Clip to backdrop.
            else if (layerCompositeMode == LayerCompositeMode.ClipToBackdrop) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                blended.r = destination.r;
                blended.g = destination.g;
                blended.b = destination.b;
                blended.a = Mathf.Max(destination.a - layerAlpha, 0f);
            }

            //Clip to layer.
            else if (layerCompositeMode == LayerCompositeMode.ClipToLayer) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                blended.a = Mathf.Max(layerAlpha - destination.a, 0f);
                if (blended.a > Maths.epsilon) {
                    blended.r = source.r;
                    blended.g = source.g;
                    blended.b = source.b;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
            }

            //Intersection.
            else if (layerCompositeMode == LayerCompositeMode.Intersection) {
                blended.r = destination.r;
                blended.g = destination.g;
                blended.b = destination.b;
                blended.a = 0f;
            }

            //Throw an exception if the layer composite mode is not supported at the current time.
            else
                throw new ImporterForGIMPImageFilesException(56, $"Layer composite mode {layerCompositeMode} not supported for split composite blending.");

            //Return the pixel colour.
            return blended;
        }

        //Composite blending for the erase layer mode.
        static Color eraseCompositeBlend(Color source, Color destination, float opacity, LayerCompositeMode layerCompositeMode, bool hasMask, float maskAlpha) {
            Color blended;

            //Union.
            if (layerCompositeMode == LayerCompositeMode.Union) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                float newAlpha = destination.a + layerAlpha - 2.0f * destination.a * layerAlpha;
                if (newAlpha > Maths.epsilon) {
                    float ratio = (1 - destination.a) * layerAlpha / newAlpha;
                    blended.r = ratio * source.r + (1 - ratio) * destination.r;
                    blended.g = ratio * source.g + (1 - ratio) * destination.g;
                    blended.b = ratio * source.b + (1 - ratio) * destination.b;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
                blended.a = newAlpha;
            }

            //Clip to backdrop.
            else if (layerCompositeMode == LayerCompositeMode.ClipToBackdrop) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                blended.r = destination.r;
                blended.g = destination.g;
                blended.b = destination.b;
                blended.a = (1.0f - layerAlpha) * destination.a;
            }

            //Clip to layer.
            else if (layerCompositeMode == LayerCompositeMode.ClipToLayer) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                float newAlpha = (1.0f - destination.a) * layerAlpha;
                if (newAlpha < Maths.epsilon) {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
                else {
                    blended.r = source.r;
                    blended.g = source.g;
                    blended.b = source.b;
                }
                blended.a = newAlpha;
            }

            //Intersection.
            else if (layerCompositeMode == LayerCompositeMode.Intersection) {
                blended.r = destination.r;
                blended.g = destination.g;
                blended.b = destination.b;
                blended.a = 0f;
            }

            //Throw an exception if the layer composite mode is not supported at the current time.
            else
                throw new ImporterForGIMPImageFilesException(57, $"Layer composite mode {layerCompositeMode} not supported for erase composite blending.");

            //Return the pixel colour.
            return blended;
        }

        //Composite blending for the merge layer mode.
        static Color mergeCompositeBlend(Color source, Color destination, float opacity, LayerCompositeMode layerCompositeMode, bool hasMask, float maskAlpha) {
            Color blended;

            //Union.
            if (layerCompositeMode == LayerCompositeMode.Union) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                float inAlpha = Mathf.Min(destination.a, 1.0f - layerAlpha);
                blended.a = inAlpha + layerAlpha;
                if (blended.a > Maths.epsilon) {
                    float ratio = layerAlpha / blended.a;
                    blended.r = destination.r + (source.r - destination.r) * ratio;
                    blended.g = destination.g + (source.g - destination.g) * ratio;
                    blended.b = destination.b + (source.b - destination.b) * ratio;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
            }

            //Clip to backdrop.
            else if (layerCompositeMode == LayerCompositeMode.ClipToBackdrop) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                layerAlpha -= 1.0f - destination.a;
                if (layerAlpha > Maths.epsilon) {
                    float ratio = layerAlpha / destination.a;
                    blended.r = destination.r + (source.r - destination.r) * ratio;
                    blended.g = destination.g + (source.g - destination.g) * ratio;
                    blended.b = destination.b + (source.b - destination.b) * ratio;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
                blended.a = destination.a;
            }

            //Clip to layer.
            else if (layerCompositeMode == LayerCompositeMode.ClipToLayer) {
                blended.a = source.a * opacity;
                if (hasMask)
                    blended.a *= maskAlpha;
                if (blended.a > Maths.epsilon) {
                    blended.r = source.r;
                    blended.g = source.g;
                    blended.b = source.b;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
            }

            //Intersection.
            else if (layerCompositeMode == LayerCompositeMode.Intersection) {
                float layerAlpha = source.a * opacity;
                if (hasMask)
                    layerAlpha *= maskAlpha;
                layerAlpha -= 1.0f - destination.a;
                layerAlpha = Mathf.Max(layerAlpha, 0.0f);
                if (layerAlpha > Maths.epsilon) {
                    blended.r = source.r;
                    blended.g = source.g;
                    blended.b = source.b;
                }
                else {
                    blended.r = destination.r;
                    blended.g = destination.g;
                    blended.b = destination.b;
                }
                blended.a = layerAlpha;
            }

            //Throw an exception if the layer composite mode is not supported at the current time.
            else
                throw new ImporterForGIMPImageFilesException(58, $"Layer composite mode {layerCompositeMode} not supported for merge composite blending.");

            //Return the pixel colour.
            return blended;
        }

        //Composite blending for the dissolve layer mode.
        static Color dissolveCompositeBlend(Color source, Color destination, float opacity, LayerCompositeMode layerCompositeMode, bool hasMask,
                float maskAlpha, int x, int y) {
            Color blended;
            RandomFunctions.newRandomWithSeed((uint) ((y % RandomFunctions.randomTableSize) + RandomFunctions.randomTableSize));
            for (int i = 0; i < x; i++)
                RandomFunctions.getInt();
            float value = source.a * opacity * 255;
            if (hasMask)
                value *= maskAlpha;
            if (RandomFunctions.getInt(0, 255) >= value) {
                blended.r = destination.r;
                blended.g = destination.g;
                blended.b = destination.b;
                blended.a = layerCompositeMode == LayerCompositeMode.Union || layerCompositeMode == LayerCompositeMode.ClipToBackdrop ? destination.a : 0.0f;
            }
            else {
                blended.r = source.r;
                blended.g = source.g;
                blended.b = source.b;
                blended.a = layerCompositeMode == LayerCompositeMode.Union || layerCompositeMode == LayerCompositeMode.ClipToLayer ? 1.0f : destination.a;
            }

            //Return the pixel colour.
            return blended;
        }

    }
}