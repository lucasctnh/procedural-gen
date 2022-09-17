using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
	public const float ViewerMoveThresholdForChunkUpdate = 25f;
	public const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

	[SerializeField] private MeshSettings meshSettings;
	[SerializeField] private HeightMapSettings heightMapSettings;
	[SerializeField] private TextureData textureSettings;

	[SerializeField] private int colliderLODIndex;
	[SerializeField] private LODInfo[] detailLevels;
	[SerializeField] private Transform viewer;
	[SerializeField] private Material terrainMaterial;

	private Vector2 viewerPosition;
	private Vector2 viewerPositionOld;
	private float meshWorldSize;
	private int chunksVisibleInViewDst;
	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	private void Start()
	{
		textureSettings.ApplyToMaterial(terrainMaterial);
		textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

		float maxViewDst = detailLevels[detailLevels.Length - 1].VisibleDstThreshold;
		meshWorldSize = meshSettings.MeshWorldSize;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

		UpdateVisibleChunks();
	}

	private void Update()
	{
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

		if (viewerPosition != viewerPositionOld)
		{
			foreach (TerrainChunk chunk in visibleTerrainChunks)
				chunk.UpdateCollisionMesh();
		}

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate)
		{
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	private void UpdateVisibleChunks()
	{
		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
		for (int i = visibleTerrainChunks.Count -1; i >= 0; i--)
		{
			alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].Coord);
			visibleTerrainChunks[i].UpdateTerrainChunk();
		}

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
		{
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
			{
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
				{
					if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
						terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
					else
					{
						TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex,
							transform, terrainMaterial, viewer);
						terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
						newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load();
					}
				}
			}
		}
	}

	private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
	{
		if (isVisible)
			visibleTerrainChunks.Add(chunk);
		else
			visibleTerrainChunks.Remove(chunk);
	}
}
