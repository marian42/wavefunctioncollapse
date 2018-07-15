using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModulePrototype))]
public class ModulePrototypeEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		ModulePrototype modulePrototype = (ModulePrototype)target;
		if (GUILayout.Button("Distribute")) {
			int i = 0;
			foreach (Transform transform in modulePrototype.transform.parent) {
				transform.localPosition = Vector3.forward * i * MapGenerator.BlockSize * 2f;
				i++;
			}
		}

		if (GUILayout.Button("Guess connectors")) {
			foreach (var face in modulePrototype.Faces) {
				face.Fingerprint = null;
			}
			modulePrototype.GuessConnectors();
		}

		if (GUILayout.Button("Reset connectors")) {
			foreach (var face in modulePrototype.Faces) {
				face.ResetConnector();
			}
		}
	}
}
