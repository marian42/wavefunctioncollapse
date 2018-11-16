using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AreaSelector))]
public class AreaSelectorEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		AreaSelector selector = (AreaSelector)target;
		if (GUILayout.Button("Generate")) {
			if (!selector.MapBehaviour.Initialized) {
				selector.MapBehaviour.Initialize();
			}
			selector.MapBehaviour.Map.Collapse(selector.StartPosition, selector.Size, true);
			selector.MapBehaviour.BuildAllSlots();
		}
	}
}
