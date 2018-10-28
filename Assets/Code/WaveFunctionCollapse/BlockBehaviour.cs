using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BlockBehaviour : MonoBehaviour {

	public Slot Slot;

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.Selected)]
	static void DrawGizmoForMyScript(BlockBehaviour target, GizmoType gizmoType) {
		Gizmos.color = Color.black;
		Gizmos.DrawWireCube(target.transform.position, Vector3.one * 2f);
	}
#endif

}
