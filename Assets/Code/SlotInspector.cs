using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SlotInspector : MonoBehaviour {

	public MapGenerator MapGenerator;

	public Vector3i GetPosition() {
		var pos = this.transform.position / MapGenerator.BlockSize;
		return new Vector3i(Mathf.FloorToInt(pos.x + 0.5f), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z + 0.5f));
	}

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.Selected)]
	static void DrawGizmoForMyScript(SlotInspector target, GizmoType gizmoType) {
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(target.MapGenerator.GetWorldspacePosition(target.GetPosition()), Vector3.one * MapGenerator.BlockSize);
	}
#endif
}
