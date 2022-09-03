using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
	public float NoiseScale;
	public int Octaves;
	[Range(0, 1)] public float Persistance;
	public float Lacunarity;
	public int Seed;
	public Vector2 Offset;
	public Noise.NormalizeMode NormalizeMode;

	protected override void OnValidate()
	{
		if (Lacunarity < 1)
			Lacunarity = 1;
		if (Octaves < 0)
			Octaves = 0;

		base.OnValidate();
	}
}
