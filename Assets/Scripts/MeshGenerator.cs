using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour {
	[HideInInspector] public Mesh mesh;

	public bool autoUpdate;

	[SerializeField] private int _xSize = 20;
	[SerializeField] private int _zSize = 20;
	[SerializeField] private float _xNoise = .3f;
	[SerializeField] private float _zNoise = .3f;
	[SerializeField] private float _noiseStrength = 2f;

	private Vector3[] _vertices;
	private int[] _triangles;

	private void Start() {
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		CreateShape();
	}

	public void CreateShape() {
		_vertices = new Vector3[(_xSize + 1) * (_zSize + 1)];

		for (int i = 0, z = 0; z <= _zSize; z++) {
			for (int x = 0; x <= _xSize; x++) {
				float y = Mathf.PerlinNoise(x * _xNoise, z * _zNoise) * _noiseStrength;
				_vertices[i] = new Vector3(x, y, z);
				i++;
			}
		}

		_triangles = new int[_xSize * _zSize * 6];

		for (int vert = 0, tris = 0, z = 0; z < _zSize; z++) {
			for (int x = 0; x < _xSize; x++) {
				_triangles[tris + 0] = vert + 0;
				_triangles[tris + 1] = vert + _xSize + 1;
				_triangles[tris + 2] = vert + 1;
				_triangles[tris + 3] = vert + 1;
				_triangles[tris + 4] = vert + _xSize + 1;
				_triangles[tris + 5] = vert + _xSize + 2;

				tris += 6;
				vert++;
			}

			vert++;
		}

		UpdateMesh();
	}

	private void UpdateMesh() {
		mesh.Clear();
		mesh.vertices = _vertices;
		mesh.triangles = _triangles;
		mesh.RecalculateNormals();
	}
}