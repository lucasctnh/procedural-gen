using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
	[System.Serializable]
	public class Layer
	{
		public Texture2D Texture;
		public float TextureScale;
		public Color Tint;
		[Range(0, 1)] public float TintStrength;
		[Range(0, 1)] public float StartHeight;
		[Range(0, 1)] public float BlendStrength;
	}

	public Layer[] layers;

	private const int textureSize = 512;
	private const TextureFormat textureFormat = TextureFormat.RGB565;

	private float savedMinHeight;
	private float savedMaxHeight;

    public void ApplyToMaterial(Material material)
	{
		material.SetInt("layerCount", layers.Length);
		material.SetColorArray("baseColours", layers.Select(x => x.Tint).ToArray());
		material.SetFloatArray("baseColoursStrength", layers.Select(x => x.TintStrength).ToArray());
		material.SetFloatArray("baseStartHeights", layers.Select(x => x.StartHeight).ToArray());
		material.SetFloatArray("baseBlends", layers.Select(x => x.BlendStrength).ToArray());
		material.SetFloatArray("baseTextureScales", layers.Select(x => x.TextureScale).ToArray());
		Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.Texture).ToArray());
		material.SetTexture("baseTextures", texturesArray);

		UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
	}

	public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
	{
		savedMinHeight = minHeight;
		savedMaxHeight = maxHeight;

		material.SetFloat("minHeight", minHeight);
		material.SetFloat("maxHeight", maxHeight);
	}

	private Texture2DArray GenerateTextureArray(Texture2D[] textures)
	{
		Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
		for (int i = 0; i < textures.Length; i++)
			textureArray.SetPixels(textures[i].GetPixels(), i);
		textureArray.Apply();
		return textureArray;
	}
}
