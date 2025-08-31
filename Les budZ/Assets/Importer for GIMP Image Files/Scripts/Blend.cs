namespace ImporterForGIMPImageFiles {
    using UnityEngine;

    internal static class Blend {

        //Enums.
        public enum LayerMode {
            Normal_Legacy = 0,
            Dissolve = 1,
            Multiply_Legacy = 3,
            Screen_Legacy = 4,
            Difference_Legacy = 6,
            Addition_Legacy = 7,
            Subtract_Legacy = 8,
            DarkenOnly_Legacy = 9,
            LightenOnly_Legacy = 10,
            HueHSV_Legacy = 11,
            SaturationHSV_Legacy = 12,
            ColorHSL_Legacy = 13,
            ValueHSV_Legacy = 14,
            Divide_Legacy = 15,
            Dodge_Legacy = 16,
            Burn_Legacy = 17,
            HardLight_Legacy = 18,
            SoftLight_Legacy = 19,
            GrainExtract_Legacy = 20,
            GrainMerge_Legacy = 21,
            ColorErase_Legacy = 22,
            Overlay = 23,
            HueLCH = 24,
            ChromaLCH = 25,
            ColourLCH = 26,
            LightnessLCH = 27,
            Normal = 28,
            Multiply = 30,
            Screen = 31,
            Difference = 32,
            Addition = 33,
            Subtract = 34,
            DarkenOnly = 35,
            LightenOnly = 36,
            HueHSV = 37,
            SaturationHSV = 38,
            ColorHSL = 39,
            ValueHSV = 40,
            Divide = 41,
            Dodge = 42,
            Burn = 43,
            HardLight = 44,
            SoftLight = 45,
            GrainExtract = 46,
            GrainMerge = 47,
            VividLight = 48,
            PinLight = 49,
            LinearLight = 50,
            HardMix = 51,
            Exclusion = 52,
            LinearBurn = 53,
            LumaLuminanceDarkenOnly = 54,
            LumaLuminanceLightenOnly = 55,
            Luminance = 56,
            ColorErase = 57,
            Erase = 58,
            Merge = 59,
            Split = 60,
            PassThrough = 61
        }
        public enum LayerSpace {
            Auto = 0,
            RGBLinear = 1,
            RGBPerceptual = 2,
            LAB = 3
        }

        //Blend two pixels. Note the "source" pixel is the pixel being applied to the image, and the "destination" pixel is the pixel already in the image.
        public static Color blend(Color source, Color destination, LayerMode layerMode, LayerSpace layerBlendSpace, LayerSpace layerCompositeSpace,
                bool isLinear, bool convertSourceAndDestinationColourSpaces) {
            Color blended;

            //Don't blend layer modes that have their own separate blending algorithms.
            if (layerMode == LayerMode.Normal || layerMode == LayerMode.Normal_Legacy || layerMode == LayerMode.PassThrough || layerMode == LayerMode.Split ||
                    layerMode == LayerMode.Erase || layerMode == LayerMode.Merge || layerMode == LayerMode.Dissolve)
                return new Color(0, 0, 0, 0);

            //Put the colour in the correct colour space.
            if (convertSourceAndDestinationColourSpaces) {
                if (isLinear) {
                    if (layerBlendSpace == LayerSpace.RGBPerceptual) {
                        ColourSpace.linearToGamma(ref source);
                        ColourSpace.linearToGamma(ref destination);
                    }
                    else if (layerBlendSpace == LayerSpace.LAB) {
                        ColourSpace.linearToLAB(ref source);
                        ColourSpace.linearToLAB(ref destination);
                    }
                }
                else {
                    if (layerBlendSpace == LayerSpace.RGBLinear) {
                        ColourSpace.gammaToLinear(ref source);
                        ColourSpace.gammaToLinear(ref destination);
                    }
                    else if (layerBlendSpace == LayerSpace.LAB) {
                        ColourSpace.gammaToLAB(ref source);
                        ColourSpace.gammaToLAB(ref destination);
                    }
                }
            }

            //Overlay.
            if (layerMode == LayerMode.Overlay) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        destination.r < 0.5f ? 2f * destination.r * source.r : (1f - (2f * (1f - source.r) * (1f - destination.r))),
                        destination.g < 0.5f ? 2f * destination.g * source.g : (1f - (2f * (1f - source.g) * (1f - destination.g))),
                        destination.b < 0.5f ? 2f * destination.b * source.b : (1f - (2f * (1f - source.b) * (1f - destination.b))),
                        source.a
                    );
            }

            //Multiply.
            else if (layerMode == LayerMode.Multiply || layerMode == LayerMode.Multiply_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        source.r * destination.r,
                        source.g * destination.g,
                        source.b * destination.b,
                        source.a
                    );
            }

            //Subtract.
            else if (layerMode == LayerMode.Subtract || layerMode == LayerMode.Subtract_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        destination.r - source.r,
                        destination.g - source.g,
                        destination.b - source.b,
                        source.a
                    );
            }

            //Colour erase.
            else if (layerMode == LayerMode.ColorErase || layerMode == LayerMode.ColorErase_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float alpha = 0;
                    for (int i = 0; i < 3; i++) {
                        float sourceClamped = Mathf.Clamp01(i == 0 ? source.r : (i == 1 ? source.g : source.b));
                        float destinationClamped = Mathf.Clamp01(i == 0 ? destination.r : (i == 1 ? destination.g : destination.b));
                        if (Mathf.Abs(destinationClamped - sourceClamped) > Maths.epsilon) {
                            float thisAlpha;
                            if (destinationClamped > sourceClamped)
                                thisAlpha = (destinationClamped - sourceClamped) / (1.0f - sourceClamped);
                            else
                                thisAlpha = (sourceClamped - destinationClamped) / sourceClamped;
                            alpha = Mathf.Max(alpha, thisAlpha);
                        }
                    }
                    if (alpha > Maths.epsilon) {
                        float inverseAlpha = 1.0f / alpha;
                        blended = new Color(
                            (destination.r - source.r) * inverseAlpha + source.r,
                            (destination.g - source.g) * inverseAlpha + source.g,
                            (destination.b - source.b) * inverseAlpha + source.b,

                            alpha
                        );
                    }
                    else
                        blended = new Color(0, 0, 0, 0);
                }
            }

            //Lighten only.
            else if (layerMode == LayerMode.LightenOnly || layerMode == LayerMode.LightenOnly_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        Mathf.Max(source.r, destination.r),
                        Mathf.Max(source.g, destination.g),
                        Mathf.Max(source.b, destination.b),
                        source.a
                    );
            }

            //Luma lighten only.
            else if (layerMode == LayerMode.LumaLuminanceLightenOnly) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float sourceLuminance = source.getLuminance();
                    float destinationLuminance = destination.getLuminance();
                    if (destinationLuminance >= sourceLuminance)
                        blended = new Color(destination.r, destination.g, destination.b, source.a);
                    else
                        blended = source;
                }
            }

            //Screen.
            else if (layerMode == LayerMode.Screen || layerMode == LayerMode.Screen_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        1.0f - (1.0f - destination.r) * (1.0f - source.r),
                        1.0f - (1.0f - destination.g) * (1.0f - source.g),
                        1.0f - (1.0f - destination.b) * (1.0f - source.b),
                        source.a
                    );
            }

            //Dodge.
            else if (layerMode == LayerMode.Dodge || layerMode == LayerMode.Dodge_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        Maths.safeDiv(destination.r, 1.0f - source.r),
                        Maths.safeDiv(destination.g, 1.0f - source.g),
                        Maths.safeDiv(destination.b, 1.0f - source.b),
                        source.a
                    );
            }

            //Addition.
            else if (layerMode == LayerMode.Addition || layerMode == LayerMode.Addition_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        destination.r + source.r,
                        destination.g + source.g,
                        destination.b + source.b,
                        source.a
                    );
            }

            //Darken only.
            else if (layerMode == LayerMode.DarkenOnly || layerMode == LayerMode.DarkenOnly_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        Mathf.Min(destination.r, source.r),
                        Mathf.Min(destination.g, source.g),
                        Mathf.Min(destination.b, source.b),
                        source.a
                    );
            }

            //Luma darken only.
            else if (layerMode == LayerMode.LumaLuminanceDarkenOnly) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float sourceLuminance = source.getLuminance();
                    float destinationLuminance = destination.getLuminance();
                    if (destinationLuminance <= sourceLuminance)
                        blended = new Color(destination.r, destination.g, destination.b, source.a);
                    else
                        blended = source;
                }
            }

            //Burn.
            else if (layerMode == LayerMode.Burn || layerMode == LayerMode.Burn_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        1.0f - Maths.safeDiv(1.0f - destination.r, source.r),
                        1.0f - Maths.safeDiv(1.0f - destination.g, source.g),
                        1.0f - Maths.safeDiv(1.0f - destination.b, source.b),
                        source.a
                    );
            }

            //Linear burn.
            else if (layerMode == LayerMode.LinearBurn) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        destination.r + source.r - 1.0f,
                        destination.g + source.g - 1.0f,
                        destination.b + source.b - 1.0f,
                        source.a
                    );
            }

            //Soft light.
            else if (layerMode == LayerMode.SoftLight || layerMode == LayerMode.SoftLight_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        (1.0f - destination.r) * destination.r * source.r + destination.r * (1.0f - (1.0f - destination.r) * (1.0f - source.r)),
                        (1.0f - destination.g) * destination.g * source.g + destination.g * (1.0f - (1.0f - destination.g) * (1.0f - source.g)),
                        (1.0f - destination.b) * destination.b * source.b + destination.b * (1.0f - (1.0f - destination.b) * (1.0f - source.b)),
                        source.a
                    );
            }

            //Hard light.
            else if (layerMode == LayerMode.HardLight || layerMode == LayerMode.HardLight_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        source.r > 0.5f ?
                            Mathf.Min(1.0f - (1.0f - destination.r) * (1.0f - (source.r - 0.5f) * 2.0f), 1.0f) :
                            Mathf.Min(destination.r * (source.r * 2.0f), 1.0f),
                        source.g > 0.5f ?
                            Mathf.Min(1.0f - (1.0f - destination.g) * (1.0f - (source.g - 0.5f) * 2.0f), 1.0f) :
                            Mathf.Min(destination.g * (source.g * 2.0f), 1.0f),
                        source.b > 0.5f ?
                            Mathf.Min(1.0f - (1.0f - destination.b) * (1.0f - (source.b - 0.5f) * 2.0f), 1.0f) :
                            Mathf.Min(destination.b * (source.b * 2.0f), 1.0f),
                        source.a
                    );
            }

            //Vivid light.
            else if (layerMode == LayerMode.VividLight) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        source.r <= 0.5f ?
                            Mathf.Max(1.0f - Maths.safeDiv(1.0f - destination.r, 2.0f * source.r), 0.0f) :
                            Mathf.Min(Maths.safeDiv(destination.r, 2.0f * (1.0f - source.r)), 1.0f),
                        source.g <= 0.5f ?
                            Mathf.Max(1.0f - Maths.safeDiv(1.0f - destination.g, 2.0f * source.g), 0.0f) :
                            Mathf.Min(Maths.safeDiv(destination.g, 2.0f * (1.0f - source.g)), 1.0f),
                        source.b <= 0.5f ?
                            Mathf.Max(1.0f - Maths.safeDiv(1.0f - destination.b, 2.0f * source.b), 0.0f) :
                            Mathf.Min(Maths.safeDiv(destination.b, 2.0f * (1.0f - source.b)), 1.0f),
                        source.a
                    );
            }

            //Pin light.
            else if (layerMode == LayerMode.PinLight) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        source.r > 0.5f ?
                            Mathf.Max(destination.r, 2.0f * (source.r - 0.5f)) :
                            Mathf.Min(destination.r, 2.0f * source.r),
                        source.g > 0.5f ?
                            Mathf.Max(destination.g, 2.0f * (source.g - 0.5f)) :
                            Mathf.Min(destination.g, 2.0f * source.g),
                        source.b > 0.5f ?
                            Mathf.Max(destination.b, 2.0f * (source.b - 0.5f)) :
                            Mathf.Min(destination.b, 2.0f * source.b),
                        source.a
                    );
            }

            //Linear light.
            else if (layerMode == LayerMode.LinearLight) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        source.r <= 0.5f ?
                            destination.r + 2.0f * source.r - 1.0f :
                            destination.r + 2.0f * (source.r - 0.5f),
                        source.g <= 0.5f ?
                            destination.g + 2.0f * source.g - 1.0f :
                            destination.g + 2.0f * (source.g - 0.5f),
                        source.b <= 0.5f ?
                            destination.b + 2.0f * source.b - 1.0f :
                            destination.b + 2.0f * (source.b - 0.5f),
                        source.a
                    );
            }

            //Hard mix.
            else if (layerMode == LayerMode.HardMix) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        destination.r + source.r < 1.0f ? 0.0f : 1.0f,
                        destination.g + source.g < 1.0f ? 0.0f : 1.0f,
                        destination.b + source.b < 1.0f ? 0.0f : 1.0f,
                        source.a
                    );
            }

            //Difference.
            else if (layerMode == LayerMode.Difference || layerMode == LayerMode.Difference_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        Mathf.Abs(destination.r - source.r),
                        Mathf.Abs(destination.g - source.g),
                        Mathf.Abs(destination.b - source.b),
                        source.a
                    );
            }

            //Difference.
            else if (layerMode == LayerMode.Exclusion) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        0.5f - 2.0f * (destination.r - 0.5f) * (source.r - 0.5f),
                        0.5f - 2.0f * (destination.g - 0.5f) * (source.g - 0.5f),
                        0.5f - 2.0f * (destination.b - 0.5f) * (source.b - 0.5f),
                        source.a
                    );
            }

            //Grain extract.
            else if (layerMode == LayerMode.GrainExtract || layerMode == LayerMode.GrainExtract_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        destination.r - source.r + 0.5f,
                        destination.g - source.g + 0.5f,
                        destination.b - source.b + 0.5f,
                        source.a
                    );
            }

            //Grain merge.
            else if (layerMode == LayerMode.GrainMerge || layerMode == LayerMode.GrainMerge_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        destination.r + source.r - 0.5f,
                        destination.g + source.g - 0.5f,
                        destination.b + source.b - 0.5f,
                        source.a
                    );
            }

            //Divide.
            else if (layerMode == LayerMode.Divide || layerMode == LayerMode.Divide_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        Maths.safeDiv(destination.r, source.r),
                        Maths.safeDiv(destination.g, source.g),
                        Maths.safeDiv(destination.b, source.b),
                        source.a
                    );
            }

            //HSV hue.
            else if (layerMode == LayerMode.HueHSV || layerMode == LayerMode.HueHSV_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float sourceMin = Mathf.Min(source.r, source.g, source.b);
                    float sourceMax = Mathf.Max(source.r, source.g, source.b);
                    float sourceDelta = sourceMax - sourceMin;
                    if (sourceDelta > 0.0001f) {
                        float destinationMin = Mathf.Min(destination.r, destination.g, destination.b);
                        float destinationMax = Mathf.Max(destination.r, destination.g, destination.b);
                        float destinationDelta = destinationMax - destinationMin;
                        float destinationDeltaOverMax = destinationMax > 0.0001f ? destinationDelta / destinationMax : 0f;
                        float ratio = destinationDeltaOverMax * destinationMax / sourceDelta;
                        float offset = destinationMax - sourceMax * ratio;
                        blended = new Color(
                            source.r * ratio + offset,
                            source.g * ratio + offset,
                            source.b * ratio + offset,
                            source.a
                        );
                    }
                    else
                        blended = new Color(
                            destination.r,
                            destination.g,
                            destination.b,
                            source.a
                        );
                }
            }

            //HSV saturation.
            else if (layerMode == LayerMode.SaturationHSV || layerMode == LayerMode.SaturationHSV_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float destinationMin = Mathf.Min(destination.r, destination.g, destination.b);
                    float destinationMax = Mathf.Max(destination.r, destination.g, destination.b);
                    float destinationDelta = destinationMax - destinationMin;
                    if (destinationDelta > 0.0001f) {
                        float sourceMin = Mathf.Min(source.r, source.g, source.b);
                        float sourceMax = Mathf.Max(source.r, source.g, source.b);
                        float sourceDelta = sourceMax - sourceMin;
                        float sourceDeltaOverMax = sourceMax > 0.0001f ? sourceDelta / sourceMax : 0f;
                        float ratio = sourceDeltaOverMax * destinationMax / destinationDelta;
                        float offset = (1.0f - ratio) * destinationMax;
                        blended = new Color(
                            destination.r * ratio + offset,
                            destination.g * ratio + offset,
                            destination.b * ratio + offset,
                            source.a
                        );
                    }
                    else
                        blended = new Color(
                            destinationMax,
                            destinationMax,
                            destinationMax,
                            source.a
                        );
                }
            }

            //HSL colour.
            else if (layerMode == LayerMode.ColorHSL || layerMode == LayerMode.ColorHSL_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float sourceMin = Mathf.Min(source.r, source.g, source.b);
                    float sourceMax = Mathf.Max(source.r, source.g, source.b);
                    float sourceAverage = (sourceMin + sourceMax) / 2f;
                    float destinationMin = Mathf.Min(destination.r, destination.g, destination.b);
                    float destinationMax = Mathf.Max(destination.r, destination.g, destination.b);
                    float destinationAverage = (destinationMin + destinationMax) / 2f;
                    if (Mathf.Abs(sourceAverage) > 0.0001f && Mathf.Abs(1.0f - sourceAverage) > 0.0001f) {
                        bool destinationHigh = destinationAverage > 0.5f;
                        bool sourceHigh = sourceAverage > 0.5f;
                        destinationAverage = Mathf.Min(destinationAverage, 1.0f - destinationAverage);
                        sourceAverage = Mathf.Min(sourceAverage, 1.0f - sourceAverage);
                        float ratio = destinationAverage / sourceAverage;
                        float offset = 0f;
                        if (destinationHigh)
                            offset += 1.0f - 2.0f * destinationAverage;
                        if (sourceHigh)
                            offset += 2.0f * destinationAverage - ratio;
                        blended = new Color(
                            source.r * ratio + offset,
                            source.g * ratio + offset,
                            source.b * ratio + offset,
                            source.a
                        );
                    }
                    else
                        blended = new Color(
                            destinationAverage,
                            destinationAverage,
                            destinationAverage,
                            source.a
                        );
                }
            }

            //HSV value.
            else if (layerMode == LayerMode.ValueHSV || layerMode == LayerMode.ValueHSV_Legacy) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float sourceMax = Mathf.Max(source.r, source.g, source.b);
                    float destinationMax = Mathf.Max(destination.r, destination.g, destination.b);
                    if (Mathf.Abs(destinationMax) > 0.0001f) {
                        float ratio = sourceMax / destinationMax;
                        blended = new Color(
                            destination.r * ratio,
                            destination.g * ratio,
                            destination.b * ratio,
                            source.a
                        );
                    }
                    else
                        blended = new Color(
                            sourceMax,
                            sourceMax,
                            sourceMax,
                            source.a
                        );
                }
            }

            //LCH hue.
            else if (layerMode == LayerMode.HueLCH) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float sourceGBHypotenuse = Maths.hypotenuse(source.g, source.b);
                    if (sourceGBHypotenuse > 0.0001f) {
                        float destinationGBHypotenuse = Maths.hypotenuse(destination.g, destination.b);
                        blended = new Color(
                            destination.r,
                            destinationGBHypotenuse * source.g / sourceGBHypotenuse,
                            destinationGBHypotenuse * source.b / sourceGBHypotenuse,
                            source.a
                        );
                    }
                    else
                        blended = new Color(
                            destination.r,
                            destination.g,
                            destination.b,
                            source.a
                        );
                }
            }

            //LCH chroma.
            else if (layerMode == LayerMode.ChromaLCH) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float destinationGBHypotenuse = Maths.hypotenuse(destination.g, destination.b);
                    if (destinationGBHypotenuse > 0.0001f) {
                        float sourceGBHypotenuse = Maths.hypotenuse(source.g, source.b);
                        blended = new Color(
                            destination.r,
                            sourceGBHypotenuse * destination.g / destinationGBHypotenuse,
                            sourceGBHypotenuse * destination.b / destinationGBHypotenuse,
                            source.a
                        );
                    }
                    else
                        blended = new Color(
                            destination.r,
                            destination.g,
                            destination.b,
                            source.a
                        );
                }
            }

            //LCH colour.
            else if (layerMode == LayerMode.ColourLCH) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        destination.r,
                        source.g,
                        source.b,
                        source.a
                    );
            }

            //LCH lightness.
            else if (layerMode == LayerMode.LightnessLCH) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else
                    blended = new Color(
                        source.r,
                        destination.g,
                        destination.b,
                        source.a
                    );
            }

            //Luminance.
            else if (layerMode == LayerMode.Luminance) {
                if (source.a < Maths.epsilon || destination.a < Maths.epsilon)
                    blended = new Color(0, 0, 0, 0);
                else {
                    float sourceOverDestinationLuminance = Maths.safeDiv(Luminance.getLuminance(source), Luminance.getLuminance(destination));
                    blended = new Color(
                        destination.r * sourceOverDestinationLuminance,
                        destination.g * sourceOverDestinationLuminance,
                        destination.b * sourceOverDestinationLuminance,
                        source.a
                    );
                }
            }

            //Throw an exception for layer modes that are not implemented.
            else
                throw new ImporterForGIMPImageFilesException(3, $"Layer mode {layerMode} not implemented.");

            //Put the blended colour in the correct colour space.
            if (isLinear) {
                if (layerBlendSpace == LayerSpace.RGBPerceptual && layerCompositeSpace != LayerSpace.RGBPerceptual)
                    ColourSpace.gammaToLinear(ref blended);
                else if (layerBlendSpace == LayerSpace.LAB && layerCompositeSpace != LayerSpace.LAB)
                    ColourSpace.LABToLinear(ref blended);
            }
            else {
                if (layerBlendSpace == LayerSpace.RGBLinear && layerCompositeSpace != LayerSpace.RGBLinear)
                    ColourSpace.linearToGamma(ref blended);
                else if (layerBlendSpace == LayerSpace.LAB && layerCompositeSpace != LayerSpace.LAB)
                    ColourSpace.LABToGamma(ref blended);
            }

            //Return the blended pixel.
            return blended;
        }
    }
}