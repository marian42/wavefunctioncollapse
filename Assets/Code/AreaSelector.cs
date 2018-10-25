using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AreaSelector : MonoBehaviour {
	public MapGenerator MapGenerator;

	public Vector3i StartPosition {
		get {
			var start = new Vector3i((this.transform.position) / MapGenerator.BlockSize);
			if (start.Y > this.MapGenerator.HeightLimit) {
				start.Y = this.MapGenerator.HeightLimit;
			}
			if (start.Y < -this.MapGenerator.HeightLimit) {
				start.Y = -this.MapGenerator.HeightLimit;
			}
			return start;
		}
	}

	public Vector3i Size {
		get {
			var start = this.StartPosition;
			var size = new Vector3i(this.transform.localScale / MapGenerator.BlockSize);
			if (size.Y + start.Y > this.MapGenerator.HeightLimit) {
				size.Y = System.Math.Max(0, this.MapGenerator.HeightLimit - start.Y);
			}
			return size;
		}
	}

//#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmoForMyScript(AreaSelector areaSelector, GizmoType gizmoType) {
		var size = areaSelector.Size.ToVector3() * MapGenerator.BlockSize;
		var start = areaSelector.StartPosition.ToVector3() * MapGenerator.BlockSize - new Vector3(1f, 0, 1f) * MapGenerator.BlockSize * 0.5f;
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(areaSelector.MapGenerator.transform.position + start + size * 0.5f, size);
	}
//#endif
}
