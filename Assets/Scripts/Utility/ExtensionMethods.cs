using UnityEngine;

namespace Assets.Scripts.Utility
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns the percent increase from b to a.
        /// Positive if a is greater than b,
        /// negative if a is less than b.
        /// </summary>
        public static float PercentDifference(this float a, float b)
        {
            return Mathf.Abs(a - b) / Mathf.Abs(a);
        }

        /// <summary>
        /// Returns 'a' plus or minus 'p' percentage of 'a'
        /// </summary>
        public static float Randomize(this float a, float p)
        {
            return Random.Range(a - (a * p), a + (a * p));
        }
    }
}