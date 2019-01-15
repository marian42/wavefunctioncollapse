using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(MapBehaviour))]
public class MapBehaviourEditor : Editor {
	private int collapseAreaSize = 6;

	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		MapBehaviour mapBehaviour = (MapBehaviour)target;
		if (GUILayout.Button("Clear")) {
			mapBehaviour.Clear();
			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		}

		GUILayout.BeginHorizontal();
		int.TryParse(GUILayout.TextField(this.collapseAreaSize.ToString()), out this.collapseAreaSize);

		if (GUILayout.Button("Initialize " + this.collapseAreaSize + "x" + this.collapseAreaSize + " area")) {
			mapBehaviour.Initialize();
			var startTime = System.DateTime.Now;
			mapBehaviour.Map.Collapse(Vector3Int.zero, new Vector3Int(this.collapseAreaSize, mapBehaviour.Map.Height, this.collapseAreaSize), true);
			Debug.Log("Initialized in " + (System.DateTime.Now - startTime).TotalSeconds + " seconds.");
			mapBehaviour.BuildAllSlots();
		}
		GUILayout.EndHorizontal();
	}
}
