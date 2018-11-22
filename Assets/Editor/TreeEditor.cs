using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Tree))]
public class TreeEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		Tree tree = (Tree)target;

		if (GUILayout.Button("Build")) {
			tree.Build();
		}
	}
}
