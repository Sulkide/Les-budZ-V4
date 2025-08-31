namespace ImporterForGIMPImageFiles {
    using UnityEngine;

    internal static class Luminance {

        //Get the luminance for a colour.
        public static float getLuminance(this Color colour) => (colour.r * 0.22248840f) + (colour.g * 0.71690369f) + (colour.b * 0.06060791f);
    }
}