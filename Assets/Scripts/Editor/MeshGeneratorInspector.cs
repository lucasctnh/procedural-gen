using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshGenerator))]
public class MeshGeneratorInspector : Editor {
	public override void OnInspectorGUI() {
		MeshGenerator meshGen = (MeshGenerator)target;

		if (DrawDefaultInspector()) {
			if (meshGen.autoUpdate && meshGen.mesh != null)
				meshGen.CreateShape();
		}

		if (GUILayout.Button("Regenerate Mesh"))
			meshGen.CreateShape();
	}
}
