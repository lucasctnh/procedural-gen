using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
	public bool UseFlatShading;
	public bool UseFalloff;
	public float MeshHeightMultiplier;
	public AnimationCurve MeshHeightCurve;
	public float UniformScale = 2f;

	public float MinHeight
	{
		get
		{
			return UniformScale * MeshHeightMultiplier * MeshHeightCurve.Evaluate(0);
		}
	}

	public float MaxHeight
	{
		get
		{
			return UniformScale * MeshHeightMultiplier * MeshHeightCurve.Evaluate(1);
		}
	}
}
