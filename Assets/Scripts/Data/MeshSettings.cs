using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
	public static readonly int[] SupportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
	//public static readonly int[] SupportedFlatshadedChunkSizes = { 48, 72, 96 };

	public const int NumOfSupportedLODs = 5;
	public const int NumOfSupportedChunkSizes = 9;
	public const int NumOfSupportedFlatshadedChunkSizes = 3;

	public bool UseFlatShading;
	public float MeshScale = 2f;
	[Range(0, NumOfSupportedChunkSizes - 1)] public int ChunkSizeIndex;
	[Range(0, NumOfSupportedFlatshadedChunkSizes - 1)] public int FlatshadedChunkSizeIndex;

	// number of vertices per line of mesh rendered at LOD = 0.
	// Includes the 2 extra vertices that are excluded from final mesh, but used for calculating normals
	public int NumOfVerticesPerLine => SupportedChunkSizes[UseFlatShading ? FlatshadedChunkSizeIndex : ChunkSizeIndex] + 1;
	public float MeshWorldSize => (NumOfVerticesPerLine - 3) * MeshScale;
}
