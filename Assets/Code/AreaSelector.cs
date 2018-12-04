using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AreaSelector : MonoBehaviour {
	public MapBehaviour MapBehaviour;

	public Vector3i StartPosition {
		get {
			var start = new Vector3i((this.transform.position) / InfiniteMap.BLOCK_SIZE);
			if (start.Y >= this.MapBehaviour.Map.Height) {
				start.Y = this.MapBehaviour.Map.Height - 1;
			}
			if (start.Y < 0) {
				start.Y = 0;
			}
			return start;
		}
	}

	public Vector3i Size {
		get {
			var start = this.StartPosition;
			var size = new Vector3i(this.transform.localScale / InfiniteMap.BLOCK_SIZE);
			if (size.Y + start.Y >= this.MapBehaviour.Map.Height) {
				size.Y = System.Math.Max(0, this.MapBehaviour.Map.Height - start.Y);
			}
			return size;
		}
	}

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.Selected)]
	static void DrawGizmoForMyScript(AreaSelector areaSelector, GizmoType gizmoType) {
		if (areaSelector.MapBehaviour == null || !areaSelector.MapBehaviour.Initialized) {
			return;
		}
		var size = areaSelector.Size.ToVector3() * InfiniteMap.BLOCK_SIZE;
		var start = areaSelector.StartPosition.ToVector3() * InfiniteMap.BLOCK_SIZE - new Vector3(1f, 0, 1f) * InfiniteMap.BLOCK_SIZE * 0.5f;
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(areaSelector.MapBehaviour.transform.position + start + size * 0.5f, size);
	}
#endif
}
