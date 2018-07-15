using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ModuleBehaviour : MonoBehaviour {

	public Module Module;

	private GUIStyle style;

	public bool Debug;

	public int Number;

	public void Initialize(Module module, Material material) {
		this.Module = module;

		this.transform.rotation = module.Orientation;

		var meshFilter = this.GetComponent<MeshFilter>();
		if (meshFilter == null) {
			meshFilter = this.gameObject.AddComponent<MeshFilter>();
		}
		meshFilter.sharedMesh = this.Module.mesh;
		var meshRenderer = this.GetComponent<MeshRenderer>();
		if (meshRenderer == null) {
			meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
		}
		meshRenderer.material = material;
	}

	[DrawGizmo(GizmoType.InSelectionHierarchy)]
	static void DrawGizmoForMyScript(ModuleBehaviour scr, GizmoType gizmoType) {
		Vector3 position = scr.transform.position;

		if (scr.style == null) {
			scr.style = new GUIStyle();
			scr.style.alignment = TextAnchor.MiddleCenter;
		}

		if (scr.Debug) {
			scr.style.normal.textColor = Color.black;
			for (int i = 0; i < 6; i++) {
				Handles.Label(position + Orientations.All[i] * Vector3.forward * MapGenerator.BlockSize / 2f, scr.Module.Connectors[i].ToString(), scr.style);
			}

			scr.style.normal.textColor = Color.red;
			Handles.Label(position, scr.Number.ToString(), scr.style);
		}
	}
}
