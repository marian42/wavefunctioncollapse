using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Chunk))]
public class ChunkEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		Chunk chunk = (Chunk)target;
		if (GUILayout.Button("Initialize")) {
			chunk.Initialize();
		}

		for (int i = 0; i < 4; i++) {
			if (chunk.Neighbors[i] == null && GUILayout.Button("Expand (" + i + ")")) {
				chunk.Expand(i);
			}
		}
	}
}