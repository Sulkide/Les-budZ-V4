namespace ImporterForGIMPImageFiles {
    using UnityEngine;

    internal static class Maths {

        //Constants.
        public const float epsilon = 1e-6f;

        //Perform a division safely - that is by avoiding a divide by zero error by capping the result.
        public static float safeDiv(float a, float b) {
            const float minimum = epsilon;
            const float maximum = 1 / minimum;
            if (Mathf.Abs(a) > minimum)
                return Mathf.Clamp(a / b, -maximum, maximum);
            else
                return 0f;
        }

        //Calculate a hypotenuse.
        public static float hypotenuse(float a, float b) => Mathf.Sqrt((a * a) + (b * b));
    }
}