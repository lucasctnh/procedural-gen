using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
	public static MapGenerator Instance;

	public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };

	[SerializeField, Range(0, 6)] int editorPreviewLOD;
	[SerializeField] private float noiseScale;
	[SerializeField] private int octaves;
	[SerializeField, Range(0, 1)] private float persistance;
	[SerializeField] private float lacunarity;
	[SerializeField] private int seed;
	[SerializeField] private Vector2 offset;
	[SerializeField] private bool useFalloff;
	[SerializeField] private float meshHeightMultiplier;
	[SerializeField] private AnimationCurve meshHeightCurve;
	[SerializeField] private TerrainType[] regions;
	[SerializeField] private bool useFlatShading;
	[SerializeField] private bool autoUpdate;
	[SerializeField] private DrawMode drawMode;
	[SerializeField] private Noise.NormalizeMode normalizeMode;

	private float[,] falloffMap;
	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	public static int MapChunkSize
	{
		get
		{
			if (Instance == null)
				Instance = FindObjectOfType<MapGenerator>();
			if (Instance.useFlatShading)
				return 95;
			else
				return 239; // 241 - 1 // 239 + 2
		}
	}

	private void Awake()
	{
		falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
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
		if (lacunarity < 1)
			lacunarity = 1;
		if (octaves < 0)
			octaves = 0;

		falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
	}

	public void DrawMapInEditor()
	{
		MapData mapData = GenerateMapData(Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap)
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
		else if (drawMode == DrawMode.ColourMap)
			display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.ColourMap, MapChunkSize, MapChunkSize));
		else if (drawMode == DrawMode.Mesh)
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, useFlatShading), TextureGenerator.TextureFromColourMap(mapData.ColourMap, MapChunkSize, MapChunkSize));
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
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, meshHeightMultiplier, meshHeightCurve, lod, useFlatShading);
		lock (meshDataThreadInfoQueue)
		{
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	private MapData GenerateMapData(Vector2 centre)
	{
		float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSize + 2, MapChunkSize + 2, noiseScale, octaves, persistance, lacunarity, seed, centre + offset, normalizeMode);

		Color[] colourMap = new Color[MapChunkSize * MapChunkSize];
		for (int y = 0; y < MapChunkSize; y++)
		{
			for (int x = 0; x < MapChunkSize; x++)
			{
				if (useFalloff)
					noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);

				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < regions.Length; i++)
				{
					if (currentHeight >= regions[i].height)
						colourMap[y * MapChunkSize + x] = regions[i].colour;
					else
						break;
				}
			}
		}


		return new MapData(noiseMap, colourMap);
	}

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

}

[System.Serializable]
public struct TerrainType
{
	public string name;
	public float height;
	public Color colour;
}

public struct MapData
{
	public readonly float[,] HeightMap;
	public readonly Color[] ColourMap;

	public MapData(float[,] heightMap, Color[] colourMap)
	{
		HeightMap = heightMap;
		ColourMap = colourMap;
	}
}