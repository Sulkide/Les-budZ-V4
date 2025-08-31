namespace ImporterForGIMPImageFiles {
    internal static class ConvertAutoBlendSpace {

        //Convert the blend space if it is set to auto.
        public static void convertAutoBlendSpace(ref Blend.LayerSpace blendSpace, Blend.LayerMode layerMode) {
            if (blendSpace == Blend.LayerSpace.Auto)
                switch (layerMode) {
                    case Blend.LayerMode.Multiply_Legacy:
                    case Blend.LayerMode.Screen_Legacy:
                    case Blend.LayerMode.Difference_Legacy:
                    case Blend.LayerMode.Addition_Legacy:
                    case Blend.LayerMode.Subtract_Legacy:
                    case Blend.LayerMode.DarkenOnly_Legacy:
                    case Blend.LayerMode.LightenOnly_Legacy:
                    case Blend.LayerMode.HueHSV_Legacy:
                    case Blend.LayerMode.SaturationHSV_Legacy:
                    case Blend.LayerMode.ColorHSL_Legacy:
                    case Blend.LayerMode.ValueHSV_Legacy:
                    case Blend.LayerMode.Divide_Legacy:
                    case Blend.LayerMode.Dodge_Legacy:
                    case Blend.LayerMode.Burn_Legacy:
                    case Blend.LayerMode.HardLight_Legacy:
                    case Blend.LayerMode.SoftLight_Legacy:
                    case Blend.LayerMode.GrainExtract_Legacy:
                    case Blend.LayerMode.GrainMerge_Legacy:
                    case Blend.LayerMode.ColorErase_Legacy:
                    case Blend.LayerMode.Overlay:
                    case Blend.LayerMode.Screen:
                    case Blend.LayerMode.Difference:
                    case Blend.LayerMode.HueHSV:
                    case Blend.LayerMode.SaturationHSV:
                    case Blend.LayerMode.ColorHSL:
                    case Blend.LayerMode.ValueHSV:
                    case Blend.LayerMode.Dodge:
                    case Blend.LayerMode.HardLight:
                    case Blend.LayerMode.SoftLight:
                    case Blend.LayerMode.GrainExtract:
                    case Blend.LayerMode.GrainMerge:
                    case Blend.LayerMode.VividLight:
                    case Blend.LayerMode.PinLight:
                    case Blend.LayerMode.LinearLight:
                    case Blend.LayerMode.HardMix:
                    case Blend.LayerMode.Exclusion:
                    case Blend.LayerMode.LinearBurn:
                    case Blend.LayerMode.LumaLuminanceDarkenOnly:
                    case Blend.LayerMode.LumaLuminanceLightenOnly:
                        blendSpace = Blend.LayerSpace.RGBPerceptual;
                        break;
                    case Blend.LayerMode.HueLCH:
                    case Blend.LayerMode.ChromaLCH:
                    case Blend.LayerMode.ColourLCH:
                    case Blend.LayerMode.LightnessLCH:
                        blendSpace = Blend.LayerSpace.LAB;
                        break;
                    default:
                        blendSpace = Blend.LayerSpace.RGBLinear;
                        break;
                }
        }
        
    }
}