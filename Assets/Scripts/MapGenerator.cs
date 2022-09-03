using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
	private struct MapThreadInfo<T>
	{
		public readonly Action<T> Callback;
		public readonly T Parameter;

		public MapThreadInfo(Action<T> callback, T parameter)
		{
			Callback = callback;
			Parameter = parameter;
		}

	}

	public enum DrawMode { NoiseMap, Mesh, FalloffMap };

	[SerializeField] private TerrainData terrainData;
	[SerializeField] private NoiseData noiseData;
	[SerializeField] private TextureData textureData;

	[SerializeField] private Material terrainMaterial;

	[SerializeField, Range(0, 6)] int editorPreviewLOD;
	[SerializeField] private bool autoUpdate;
	[SerializeField] private DrawMode drawMode;

	private float[,] falloffMap;
	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	public TerrainData TerrainData => terrainData;
	public NoiseData NoiseData => noiseData;
	public TextureData TextureData => textureData;

	public int MapChunkSize
	{
		get
		{
			if (TerrainData.UseFlatShading)
				return 95;
			else
				return 239; // 241 - 1 // 239 + 2
		}
	}

	private void Update()
	{
		if (mapDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.Callback(threadInfo.Parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.Callback(threadInfo.Parameter);
			}
		}
	}

	private void OnValidate()
	{
		if (terrainData != null)
		{
			terrainData.OnValuesUpdated -= OnValuesUpdated;
			terrainData.OnValuesUpdated += OnValuesUpdated;
		}
		if (noiseData != null)
		{
			noiseData.OnValuesUpdated -= OnValuesUpdated;
			noiseData.OnValuesUpdated += OnValuesUpdated;
		}
		if (textureData != null)
		{
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}
	}

	public void DrawMapInEditor()
	{
		MapData mapData = GenerateMapData(Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap)
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
		else if (drawMode == DrawMode.Mesh)
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, TerrainData.MeshHeightMultiplier, TerrainData.MeshHeightCurve,
				editorPreviewLOD, TerrainData.UseFlatShading));
		else if (drawMode == DrawMode.FalloffMap)
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(MapChunkSize)));
	}

	public void RequestMapData(Vector2 centre, Action<MapData> callback)
	{
		ThreadStart threadStart = delegate
		{
			MapDataThread(centre, callback);
		};

		new Thread(threadStart).Start();
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
	{
		ThreadStart threadStart = delegate
		{
			MeshDataThread(mapData, lod, callback);
		};

		new Thread(threadStart).Start();
	}

	private void MapDataThread(Vector2 centre, Action<MapData> callback)
	{
		MapData mapData = GenerateMapData(centre);
		lock (mapDataThreadInfoQueue)
		{
			mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
		}
	}

	private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
	{
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, TerrainData.MeshHeightMultiplier,
			TerrainData.MeshHeightCurve, lod, TerrainData.UseFlatShading);
		lock (meshDataThreadInfoQueue)
		{
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	private MapData GenerateMapData(Vector2 centre)
	{
		float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSize + 2, MapChunkSize + 2, NoiseData.NoiseScale, NoiseData.Octaves,
			NoiseData.Persistance, NoiseData.Lacunarity, NoiseData.Seed, centre + NoiseData.Offset, NoiseData.NormalizeMode);

		if (terrainData.UseFalloff)
		{
			if (falloffMap == null)
				falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize + 2);

			for (int y = 0; y < MapChunkSize + 2; y++)
			{
				for (int x = 0; x < MapChunkSize + 2; x++)
				{
					if (TerrainData.UseFalloff)
						noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
				}
			}
		}

		textureData.UpdateMeshHeights(terrainMaterial, terrainData.MinHeight, terrainData.MaxHeight);

		return new MapData(noiseMap);
	}

	private void OnValuesUpdated()
	{
		if (!Application.isPlaying)
			DrawMapInEditor();
	}

	private void OnTextureValuesUpdated()
	{
		textureData.ApplyToMaterial(terrainMaterial);
	}
}

public struct MapData
{
	public readonly float[,] HeightMap;

	public MapData(float[,] heightMap)
	{
		HeightMap = heightMap;
	}
}