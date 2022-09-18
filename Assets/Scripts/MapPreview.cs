using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
	public enum DrawMode { NoiseMap, Mesh, FalloffMap };

	[SerializeField] private MeshSettings meshSettings;
	[SerializeField] private HeightMapSettings heightMapSettings;
	[SerializeField] private TextureData textureData;

	[SerializeField] private Material terrainMaterial;

	[SerializeField, Range(0, MeshSettings.NumOfSupportedLODs - 1)] private int editorPreviewLOD;
	[SerializeField] private bool autoUpdate;
	[SerializeField] private DrawMode drawMode;

	[SerializeField] private Renderer textureRenderer;
	[SerializeField] private MeshFilter meshFilter;
	[SerializeField] private MeshRenderer meshRenderer;

	public MeshSettings MeshSettings => meshSettings;
	public HeightMapSettings HeightMapSettings => heightMapSettings;
	public TextureData TextureData => textureData;

	private void OnValidate()
	{
		if (meshSettings != null)
		{
			meshSettings.OnValuesUpdated -= OnValuesUpdated;
			meshSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (heightMapSettings != null)
		{
			heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (textureData != null)
		{
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}
	}

	public void DrawTexture(Texture2D texture)
	{
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);

		textureRenderer.gameObject.SetActive(true);
		meshFilter.gameObject.SetActive(false);
	}

	public void DrawMesh(MeshData meshData)
	{
		meshFilter.sharedMesh = meshData.CreateMesh();

		textureRenderer.gameObject.SetActive(false);
		meshFilter.gameObject.SetActive(true);
	}

	public void DrawMapInEditor()
	{
		textureData.ApplyToMaterial(terrainMaterial);
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.NumOfVerticesPerLine, meshSettings.NumOfVerticesPerLine, heightMapSettings, Vector2.zero);

		if (drawMode == DrawMode.NoiseMap)
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		else if (drawMode == DrawMode.Mesh)
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.Values, meshSettings, editorPreviewLOD));
		else if (drawMode == DrawMode.FalloffMap)
			DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.NumOfVerticesPerLine), 0, 1)));
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
