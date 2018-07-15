using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class SlotBehaviour : MonoBehaviour {

	public Slot Slot;

	private GUIStyle style;

	[DrawGizmo(GizmoType.InSelectionHierarchy)]
	static void DrawGizmoForMyScript(SlotBehaviour scr, GizmoType gizmoType) {
		Vector3 position = scr.transform.position;

		if (scr.style == null) {
			scr.style = new GUIStyle();
			scr.style.alignment = TextAnchor.MiddleCenter;
			scr.style.normal.textColor = Color.black;
		} 

		//Handles.Label(scr.Slot.GetPosition(), string.Join(", ", scr.Slot.Modules.Select(m => m.ToString()).ToArray()), scr.style);
	}
}
