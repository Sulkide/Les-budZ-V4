namespace ImporterForGIMPImageFiles {
    internal static class ConvertAutoCompositeSpace {

        //Convert the blend space if it is set to auto.
        public static void convertAutoCompositeSpace(ref Blend.LayerSpace compositeSpace, Blend.LayerMode layerMode) {
            if (compositeSpace == Blend.LayerSpace.Auto)
                switch (layerMode) {
                    case Blend.LayerMode.Normal_Legacy:
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
                        compositeSpace = Blend.LayerSpace.RGBPerceptual;
                        break;
                    default:
                        compositeSpace = Blend.LayerSpace.RGBLinear;
                        break;
                }
        }
    }
}