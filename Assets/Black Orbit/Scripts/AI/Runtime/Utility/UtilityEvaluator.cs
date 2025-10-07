using System;
using System.Collections.Generic;

namespace Black_Orbit.Scripts.AI.Runtime.Utility
{
    public static class UtilityEvaluator
    {
        // Мультипликативная с компенсацией (avoid zero)
        public static float CombineMultiplyCompensate(IReadOnlyList<float> considerations)
        {
            if (considerations == null || considerations.Count == 0) return 0f;
            float product = 1f;
            float modFactor = 1f;
            float count = considerations.Count;

            for (int i = 0; i < considerations.Count; i++)
            {
                float c = Clamp01(considerations[i]);
                float modification = (1f - (1f / count));
                float makeUp = (1f - c) * modification;
                float compensated = c + (makeUp * c);
                product *= compensated;
            }
            return Clamp01(product);
        }

        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
