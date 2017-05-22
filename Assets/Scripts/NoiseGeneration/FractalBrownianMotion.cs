using System;

namespace Assets.Scripts.NoiseGeneration
{
    public static class FractalBrownianMotion
    {
        public static float GenerateNoise(int x, int y, int z, int numOctaves, float gain, float frequency,
            float lacunarity, Func<float, float, float, float> noise)
        {
            var total = 0f;
            var amplitude = gain;

            for (var o = 0; o < numOctaves; o++)
            {
                total += noise(x * frequency, y * frequency, z * frequency) * amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            return total;
        }

        public static float[,,] GenerateTileableNoise(int sizeX, int sizeY, int sizeZ,
            int numOctaves, float gain, float frequency,
            float lacunarity, Func<float, float, float, float> noise, int seed = 0)
        {
            var buffer = new float[sizeX, sizeY, sizeZ];

            for (var k = 0; k < sizeZ; k++)
            {
                for (var j = 0; j < sizeY; j++)
                {
                    for (var i = 0; i < sizeX; i++)
                    {
                        var total = 0f;
                        var amplitude = gain;
                        var freq = frequency;

                        for (var o = 0; o < numOctaves; o++)
                        {
                            total += noise((i + seed) * freq, (j + seed) * freq, (k + seed) * freq) * amplitude;
                            freq *= lacunarity;
                            amplitude *= gain;
                        }

                        buffer[i, j, k] = total;
                    }
                }
            }

            return buffer;
        }
    }
}
