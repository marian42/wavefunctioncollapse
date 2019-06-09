using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SlotInspector : MonoBehaviour {

	public MapBehaviour MapBehaviour;

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.Selected)]
	static void DrawGizmo(SlotInspector target, GizmoType gizmoType) {
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(target.MapBehaviour.GetWorldspacePosition(target.MapBehaviour.GetMapPosition(target.transform.position)), Vector3.one * InfiniteMap.BLOCK_SIZE);
	}
#endif
}
