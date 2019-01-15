using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AreaSelector : MonoBehaviour {
	public MapBehaviour MapBehaviour;

	public Vector3Int StartPosition {
		get {
			var start = Vector3Int.RoundToInt((this.transform.position) / InfiniteMap.BLOCK_SIZE);
			if (start.y >= this.MapBehaviour.Map.Height) {
				start.y = this.MapBehaviour.Map.Height - 1;
			}
			if (start.y < 0) {
				start.y = 0;
			}
			return start;
		}
	}

	public Vector3Int Size {
		get {
			var start = this.StartPosition;
			var size = Vector3Int.RoundToInt(this.transform.localScale / InfiniteMap.BLOCK_SIZE);
			if (size.y + start.y >= this.MapBehaviour.Map.Height) {
				size.y = System.Math.Max(0, this.MapBehaviour.Map.Height - start.y);
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
