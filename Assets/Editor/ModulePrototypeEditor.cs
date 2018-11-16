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
				transform.localPosition = Vector3.forward * i * InfiniteMap.BLOCK_SIZE * 2f;
				i++;
			}
		}

		if (GUILayout.Button("Distribute (Overview)")) {
			int w = Mathf.FloorToInt(Mathf.Sqrt(modulePrototype.transform.parent.childCount));
			int i = 0;
			foreach (Transform transform in modulePrototype.transform.parent) {
				transform.localPosition = Vector3.forward * (i / w) * AbstractMap.BLOCK_SIZE * 1.4f + Vector3.right * (i % w) * AbstractMap.BLOCK_SIZE * 1.4f;
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

		if (GUILayout.Button("Reset exlusion rules in all prototypes")) {
			foreach (var prototype in modulePrototype.transform.parent.GetComponentsInChildren<ModulePrototype>()) {
				foreach (var face in prototype.Faces) {
					face.ExcludedNeighbours = new ModulePrototype[0];
				}
			}
		}
	}
}
