using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SlotInspector : MonoBehaviour {

	public MapGenerator MapGenerator;

	public Vector3i GetPosition() {
		return new Vector3i(this.transform.position / MapGenerator.BlockSize + new Vector3(-1, 0, 1) * 0.5f);
	}

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.Selected)]
	static void DrawGizmoForMyScript(SlotInspector target, GizmoType gizmoType) {
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(target.MapGenerator.GetWorldspacePosition(target.GetPosition()), Vector3.one * MapGenerator.BlockSize);
	}
#endif
}
