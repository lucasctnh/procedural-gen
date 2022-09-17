using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
	public event System.Action<TerrainChunk, bool> OnVisibilityChanged;

	public const float ColliderGenerationDistanceThreshold = 5f;

	public Vector2 Coord;

	private GameObject meshObject;
	private Vector2 sampleCenter;
	private Bounds bounds;

	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;
	private MeshCollider meshCollider;

	private LODInfo[] detailLevels;
	private LODMesh[] lodMeshes;
	private int colliderLODIndex;
	private bool hasSetCollider;

	private HeightMap heightMap;
	private bool heightMapReceived;
	private int previousLODIndex = -1;
	private float maxViewDst;

	private HeightMapSettings heightMapSettings;
	private MeshSettings meshSettings;
	private Transform viewer;

	private Vector2 ViewerPosition => new Vector2(viewer.position.x, viewer.position.z);

	public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex,
		Transform parent, Material material, Transform viewer)
	{
		this.Coord = coord;
		this.detailLevels = detailLevels;
		this.colliderLODIndex = colliderLODIndex;
		this.heightMapSettings = heightMapSettings;
		this.meshSettings = meshSettings;
		this.viewer = viewer;

		sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.MeshScale;
		Vector2 position = coord * meshSettings.MeshWorldSize;
		bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

		meshObject = new GameObject("Terrain Chunk");
		meshRenderer = meshObject.AddComponent<MeshRenderer>();
		meshFilter = meshObject.AddComponent<MeshFilter>();
		meshCollider = meshObject.AddComponent<MeshCollider>();
		meshRenderer.material = material;

		meshObject.transform.position = new Vector3(position.x, 0, position.y);
		meshObject.transform.parent = parent;
		SetVisible(false);

		lodMeshes = new LODMesh[detailLevels.Length];
		for (int i = 0; i < detailLevels.Length; i++)
		{
			lodMeshes[i] = new LODMesh(detailLevels[i].Lod);
			lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
			if (i == colliderLODIndex)
				lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
		}

		maxViewDst = detailLevels[detailLevels.Length - 1].VisibleDstThreshold;
	}

	public void Load()
	{
		ThreadedDataRequester.RequestData(
			() => HeightMapGenerator.GenerateHeightMap(meshSettings.NumOfVerticesPerLine, meshSettings.NumOfVerticesPerLine,
			heightMapSettings, sampleCenter),
			OnHeightMapReceived
		);
	}

	public void UpdateTerrainChunk()
	{
		if (!heightMapReceived) return;

		float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(ViewerPosition));

		bool wasVisible = IsVisible();
		bool visible = viewerDstFromNearestEdge <= maxViewDst;

		if (visible)
		{
			int lodIndex = 0;

			for (int i = 0; i < detailLevels.Length - 1; i++)
			{
				if (viewerDstFromNearestEdge > detailLevels[i].VisibleDstThreshold)
					lodIndex = i + 1;
				else
					break;
			}

			if (lodIndex != previousLODIndex)
			{
				LODMesh lodMesh = lodMeshes[lodIndex];
				if (lodMesh.HasMesh)
				{
					previousLODIndex = lodIndex;
					meshFilter.mesh = lodMesh.Mesh;
				}
				else if (!lodMesh.HasRequestedMesh)
					lodMesh.RequestMesh(heightMap, meshSettings);
			}
		}

		if (wasVisible != visible)
		{
			SetVisible(visible);
			OnVisibilityChanged?.Invoke(this, visible);
		}
	}

	public void UpdateCollisionMesh()
	{
		if (hasSetCollider) return;

		float sqrDstFromViewerToEdge = bounds.SqrDistance(ViewerPosition);

		if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].SqrVisibleDstThreshold)
		{
			if (!lodMeshes[colliderLODIndex].HasRequestedMesh)
				lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
		}

		if (sqrDstFromViewerToEdge < ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold)
		{
			if (lodMeshes[colliderLODIndex].HasMesh)
			{
				meshCollider.sharedMesh = lodMeshes[colliderLODIndex].Mesh;
				hasSetCollider = true;
			}
		}
	}

	public void SetVisible(bool visible)
	{
		meshObject.SetActive(visible);
	}

	public bool IsVisible()
	{
		return meshObject.activeSelf;
	}

	private void OnHeightMapReceived(object heightMapObject)
	{
		heightMap = (HeightMap)heightMapObject;
		heightMapReceived = true;

		UpdateTerrainChunk();
	}
}

public class LODMesh
{
	public event System.Action UpdateCallback;

	public Mesh Mesh;
	public bool HasRequestedMesh;
	public bool HasMesh;

	private int lod;

	public LODMesh(int lod)
	{
		this.lod = lod;
	}

	private void OnMeshDataReceived(object meshDataObject)
	{
		Mesh = ((MeshData)meshDataObject).CreateMesh();
		HasMesh = true;

		UpdateCallback?.Invoke();
	}

	public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
	{
		HasRequestedMesh = true;
		ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.Values, meshSettings, lod), OnMeshDataReceived);
	}
}

[System.Serializable]
public struct LODInfo
{
	[Range(0, MeshSettings.NumOfSupportedLODs - 1)] public int Lod;
	public float VisibleDstThreshold;

	public float SqrVisibleDstThreshold => VisibleDstThreshold * VisibleDstThreshold;
}