using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
	public enum NormalizeMode { Local, Global }

	public const int RandomRange = 100000;

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)
	{
		float[,] noiseMap = new float[mapWidth, mapHeight];

		System.Random prng = new System.Random(settings.Seed);
		Vector2[] octaveOffsets = new Vector2[settings.Octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < settings.Octaves; i++)
		{
			float offsetX = prng.Next(-RandomRange, RandomRange) + settings.Offset.x + sampleCenter.x;
			float offsetY = prng.Next(-RandomRange, RandomRange) - settings.Offset.y - sampleCenter.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= settings.Persistance;
		}

		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < settings.Octaves; i++)
				{
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.Scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.Scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // change range to make negative possible
					noiseHeight += perlinValue * amplitude;

					amplitude *= settings.Persistance;
					frequency *= settings.Lacunarity;
				}

				if (noiseHeight > maxLocalNoiseHeight)
					maxLocalNoiseHeight = noiseHeight;
				if (noiseHeight < minLocalNoiseHeight)
					minLocalNoiseHeight = noiseHeight;

				noiseMap[x, y] = noiseHeight;

				if (settings.NormalizeMode == NormalizeMode.Global) // for seamless chunks
				{
					float normalizeHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 2f); // estimate
					noiseMap[x, y] = Mathf.Clamp(normalizeHeight, 0, int.MaxValue);
				}
			}
		}

		if (settings.NormalizeMode == NormalizeMode.Local)
		{
			for (int y = 0; y < mapHeight; y++)
				for (int x = 0; x < mapWidth; x++)
					noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]); // normalize values into 0 and 1
		}

		return noiseMap;
	}
}

[System.Serializable]
public class NoiseSettings
{
	public float Scale = 50f;
	public int Octaves = 6;
	[Range(0, 1)] public float Persistance = .6f;
	public float Lacunarity = 2f;

	public int Seed;
	public Vector2 Offset;
	public Noise.NormalizeMode NormalizeMode;

	public void ValidateValues()
	{
		Scale = Mathf.Max(Scale, 0.01f);
		Octaves = Mathf.Max(Octaves, 1);
		Lacunarity = Mathf.Max(Lacunarity, 1);
		Persistance = Mathf.Clamp01(Persistance);
	}
}