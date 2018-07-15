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
			if (Application.isPlaying) {
				generator.StartCoroutine("Generate");
			} else {
				var innerRoutine = generator.Generate();
				while (innerRoutine.MoveNext());
			}
		}

		if (GUILayout.Button("Show Modules")) {
			generator.ShowModules();
		}
	}
}
