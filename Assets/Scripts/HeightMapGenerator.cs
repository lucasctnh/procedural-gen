using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter)
	{
		float[,] values = Noise.GenerateNoiseMap(width, height, settings.NoiseSettings, sampleCenter);

		AnimationCurve threadSafeHeightCurve = new AnimationCurve(settings.HeightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				values[i, j] *= threadSafeHeightCurve.Evaluate(values[i, j]) * settings.HeightMultiplier;

				if (values[i, j] > maxValue)
					maxValue = values[i, j];
				if (values[i, j] < minValue)
					minValue = values[i, j];
			}
		}

		return new HeightMap(values, minValue, maxValue);
	}
}

public struct HeightMap
{
	public readonly float[,] Values;
	public readonly float MinValue;
	public readonly float MaxValue;

	public HeightMap(float[,] values, float minValue, float maxValue)
	{
		Values = values;
		MinValue = minValue;
		MaxValue = maxValue;
	}
}