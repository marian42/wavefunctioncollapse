using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(ModuleData))]
public class ModuleDataEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		ModuleData moduleData = (ModuleData)target;

		int count = moduleData.Modules != null ? moduleData.Modules.Length : 0;
		GUILayout.Label(count + " Modules");

		EditorGUILayout.HelpBox("Create a transform that contains one child for each module prototype and save it as a prefab. Drag it into the Prototypes property above and click \"Create module data\".", MessageType.Info);

		if (GUILayout.Button("Create module data")) {
			moduleData.CreateModules();
		}

		EditorGUILayout.HelpBox("This removes neighbors that are implicitly exluded. It's optional and will make map generation ~20% faster.", MessageType.Info);

		if (GUILayout.Button("Simplify module data")) {
			moduleData.SimplifyNeighborData();
		}
	}
}
