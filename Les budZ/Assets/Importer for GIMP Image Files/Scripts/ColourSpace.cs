namespace ImporterForGIMPImageFiles {
    using System;
    using UnityEngine;

    internal static class ColourSpace {

        //Constants.
        const float LABEpsilon = 216.0f / 24389.0f;
        const float LABKappa = 24389.0f / 27.0f;
        const float D50WhiteRefX = 0.964202880f;
        const float D50WhiteRefY = 1.000000000f;
        const float D50WhiteRefZ = 0.824905400f;

        //Convert colours between colour spaces.
        public static void linearToGamma(ref Color colour) {
            colour.r = linearToGamma(colour.r, colour.r > 1024.0f);
            colour.g = linearToGamma(colour.g, colour.g > 1024.0f);
            colour.b = linearToGamma(colour.b, colour.b > 1024.0f);
        }
        public static void gammaToLinear(ref Color colour) {
            colour.r = gammaToLinear(colour.r, colour.r > 15.218009478672985781990521327014f);
            colour.g = gammaToLinear(colour.g, colour.g > 15.218009478672985781990521327014f);
            colour.b = gammaToLinear(colour.b, colour.b > 15.218009478672985781990521327014f);
        }
        public static void gammaToLAB(ref Color colour) {
            gammaToLinear(ref colour);
            RGB2XYZ(ref colour);
            XYZ2LAB(ref colour);
        }
        public static void LABToGamma(ref Color colour) {
            LAB2XYZ(ref colour);
            XYZ2RGB(ref colour);
            linearToGamma(ref colour);
        }
        public static void linearToLAB(ref Color colour) {
            RGB2XYZ(ref colour);
            XYZ2LAB(ref colour);
        }
        public static void LABToLinear(ref Color colour) {
            LAB2XYZ(ref colour);
            XYZ2RGB(ref colour);
        }

        //Internal conversions.
        static float linearToGamma(float v, bool accurate) {
            if (v <= 0.003130804954f)
                return v * 12.92f;
            else
                return 1.055f * raiseToPower1Over2Point4(v, accurate) - 0.0549998211860657f;
        }
        static float gammaToLinear(float v, bool accurate) {
            if (v <= 0.04045f)
                return v / 12.92f;
            else
                return raiseToPower2Point4((v + 0.055f) / 1.055f, accurate);
        }
        static void RGB2XYZ(ref Color colour) {
            float r = 0.43603516f * colour.r + 0.38511658f * colour.g + 0.14305115f * colour.b;
            float g = 0.22248840f * colour.r + 0.71690369f * colour.g + 0.06060791f * colour.b;
            float b = 0.01391602f * colour.r + 0.09706116f * colour.g + 0.71392822f * colour.b;
            colour.r = r;
            colour.g = g;
            colour.b = b;
        }
        static void XYZ2RGB(ref Color colour) {
            float r = 3.134274799724f * colour.r - 1.617275708956f * colour.g - 0.490724283042f * colour.b;
            float g = -0.978795575994f * colour.r + 1.916161689117f * colour.g + 0.033453331711f * colour.b;
            float b = 0.071976988401f * colour.r - 0.228984974402f * colour.g + 1.405718224383f * colour.b;
            colour.r = r;
            colour.g = g;
            colour.b = b;
        }
        static void XYZ2LAB(ref Color colour) {
            float x = colour.r / D50WhiteRefX;
            float y = colour.g / D50WhiteRefY;
            float z = colour.b / D50WhiteRefZ;
            float x2, y2, z2;
            if (x > LABEpsilon)
                x2 = Mathf.Pow(x, 1.0f / 3.0f);
            else
                x2 = ((LABKappa * x) + 16f) / 116.0f;
            if (y > LABEpsilon)
                y2 = Mathf.Pow(y, 1.0f / 3.0f);
            else
                y2 = ((LABKappa * y) + 16f) / 116.0f;
            if (z > LABEpsilon)
                z2 = Mathf.Pow(z, 1.0f / 3.0f);
            else
                z2 = ((LABKappa * z) + 16f) / 116.0f;
            colour.r = (116.0f * y2) - 16.0f;
            colour.g = 500.0f * (x2 - y2);
            colour.b = 200.0f * (y2 - z2);
        }
        static void LAB2XYZ(ref Color colour) {
            float y = (colour.r + 16.0f) / 116.0f;
            float yCubed = y * y * y;
            float x = y + colour.g / 500.0f;
            float xCubed = x * x * x;
            float z = y - colour.b / 200.0f;
            float zCubed = z * z * z;
            float y2 = colour.r > LABKappa * LABEpsilon ? yCubed : colour.r / LABKappa;
            float x2 = xCubed > LABEpsilon ? xCubed : (x * 116.0f - 16.0f) / LABKappa;
            float z2 = zCubed > LABEpsilon ? zCubed : (z * 116.0f - 16.0f) / LABKappa;
            colour.r = x2 * D50WhiteRefX;
            colour.g = y2 * D50WhiteRefY;
            colour.b = z2 * D50WhiteRefZ;
        }

        //Raise a number to the power of 2.4 or 1 over 2.4 in order to perform gamma correction.
        static float initialiseNewton(float x, float c0, float c1, float c2) {
            long l = BitConverter.DoubleToInt64Bits(x);
            int exponent = (int) ((l & 9218868437227405312) >> 52) - 1024;
            float mantissa = ((float) BitConverter.Int64BitsToDouble((l & 4503599627370495) + 4602678819172646912) * 2) + exponent;
            return c0 + c1 * mantissa + c2 * mantissa * mantissa;
        }
        static float raiseToPower2Point4(float x, bool accurate) {
            if (accurate)
                return Mathf.Exp(Mathf.Log(x) * 2.4f);
            float y = initialiseNewton(x, 0.9953189663f, -0.1330059f, 0.01295872f);
            for (int i = 0; i < 3; i++)
                y = 1.2f * y - 0.2f * x * y * y * y * y * y * y;
            x *= y;
            return x * x * x;
        }
        static float raiseToPower1Over2Point4(float x, bool accurate) {
            if (accurate)
                return Mathf.Exp(Mathf.Log(x) * (1f / 2.4f));
            float y = initialiseNewton(x, 0.9976800269f, -0.05709874f, 0.001971384f);
            x = Mathf.Sqrt(x);
            float z = x * 0.16666667f;
            for (int i = 0; i < 3; i++)
                y = 1.16666667f * y - z * y * y * y * y * y * y * y;
            return x * y;
        }
    }
}