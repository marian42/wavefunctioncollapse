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
	}
}