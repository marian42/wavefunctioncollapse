using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		MapGenerator generator = (MapGenerator)target;
		if (GUILayout.Button("Generate")) {
			generator.Generate();
			Debug.Log("Map generation complete.");
			//generator.Build();
		}
	}
}
