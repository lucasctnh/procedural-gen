using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
	public const float ViewerMoveThresholdForChunkUpdate = 25f;
	public const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

	public static Vector2 viewerPosition;
	public static MapGenerator mapGenerator;
	public static float MaxViewDst = 450;

	[SerializeField] private LODInfo[] detailLevels;
	[SerializeField] private Transform viewer;
	[SerializeField] private Material mapMaterial;

	private static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	private Vector2 viewerPositionOld;
	private int chunkSize;
	private int chunksVisibleInViewDst;
	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

	private void Start()
	{
		mapGenerator = FindObjectOfType<MapGenerator>();

		MaxViewDst = detailLevels[detailLevels.Length - 1].VisibleDstThreshold;
		chunkSize = mapGenerator.MapChunkSize - 1;
		chunksVisibleInViewDst = Mathf.RoundToInt(MaxViewDst / chunkSize);

		UpdateVisibleChunks();
	}

	private void Update()
	{
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.TerrainData.UniformScale;

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate)
		{
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	private void UpdateVisibleChunks()
	{
		for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
			terrainChunksVisibleLastUpdate[i].SetVisible(false);

		terrainChunksVisibleLastUpdate.Clear();

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
		{
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
			{
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
					terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
				else
					terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
			}
		}
	}

	public class TerrainChunk
	{
		private GameObject meshObject;
		private Vector2 position;
		private Bounds bounds;

		private MeshRenderer meshRenderer;
		private MeshFilter meshFilter;
		private MeshCollider meshCollider;

		private LODInfo[] detailLevels;
		private LODMesh[] lodMeshes;
		private LODMesh collisionLODLevel;

		private MapData mapData;
		private bool mapDataReceived;
		private int previousLODIndex = -1;

		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
		{
			this.detailLevels = detailLevels;

			position = coord * size;
			bounds = new Bounds(position, Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x, 0, position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshRenderer.material = material;

			meshObject.transform.position = positionV3 * mapGenerator.TerrainData.UniformScale;
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * mapGenerator.TerrainData.UniformScale;
			SetVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++)
			{
				lodMeshes[i] = new LODMesh(detailLevels[i].Lod, UpdateTerrainChunk);
				if (detailLevels[i].UseForCollider)
					collisionLODLevel = lodMeshes[i];
			}

			mapGenerator.RequestMapData(position, OnMapDataReceived);
		}

		public void UpdateTerrainChunk()
		{
			if (!mapDataReceived) return;

			float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
			bool visible = viewerDstFromNearestEdge <= MaxViewDst;

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
						lodMesh.RequestMesh(mapData);
				}

				if (lodIndex == 0)
				{
					if (collisionLODLevel.HasMesh)
						meshCollider.sharedMesh = collisionLODLevel.Mesh;
					else if (!collisionLODLevel.HasRequestedMesh)
						collisionLODLevel.RequestMesh(mapData);
				}

				terrainChunksVisibleLastUpdate.Add(this);
			}

			SetVisible(visible);
		}

		public void SetVisible(bool visible)
		{
			meshObject.SetActive(visible);
		}

		public bool IsVisible()
		{
			return meshObject.activeSelf;
		}

		private void OnMapDataReceived(MapData mapData)
		{
			this.mapData = mapData;
			mapDataReceived = true;

			UpdateTerrainChunk();
		}
	}

	public class LODMesh
	{
		public Mesh Mesh;
		public bool HasRequestedMesh;
		public bool HasMesh;

		private int lod;
		private System.Action updateCallback;

		public LODMesh(int lod, System.Action updateCallback)
		{
			this.lod = lod;
			this.updateCallback = updateCallback;
		}

		private void OnMeshDataReceived(MeshData meshData)
		{
			Mesh = meshData.CreateMesh();
			HasMesh = true;

			updateCallback();
		}

		public void RequestMesh(MapData mapData)
		{
			HasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
		}
	}

	[System.Serializable]
	public struct LODInfo
	{
		public int Lod;
		public float VisibleDstThreshold;
		public bool UseForCollider;
	}
}
