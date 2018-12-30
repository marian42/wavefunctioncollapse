using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal {

	public readonly Vector3i Position1;
	public readonly Vector3i Position2;
	public readonly int Direction;

	private readonly OcclusionCulling cullingData;

	public readonly Bounds Bounds;

	public Room Room1 {
		get {
			return this.cullingData.GetRoom(this.Position1);
		}
	}
	public Room Room2 {
		get {
			return this.cullingData.GetRoom(this.Position2);
		}
	}

	public bool IsInside {
		get {
			return this.Room1 != null && this.Room2 != null;
		}
	}

	public Room Room {
		get {
			return this.Room1 ?? this.Room2;
		}
	}

	// Direction must be 0, 1 or 2
	public Portal(Vector3i position, int direction, OcclusionCulling cullingData) {
		this.Position1 = position;
		this.Direction = direction;
		this.Position2 = this.Position1 + Orientations.Direction[direction];
		this.cullingData = cullingData;
	}

	public bool IsVisibleFromOutside(Camera camera) {
		var normal = Orientations.Direction[this.Direction + (this.Room1 == null ? 3 : 0)].ToVector3();
		return Vector3.Angle(camera.transform.forward, -normal) < camera.fieldOfView / 2f + 90f;
	}

	public void DrawGizmo(MapBehaviour map, Color color) {
		var pos = 0.5f * (map.GetWorldspacePosition(this.Position1) + map.GetWorldspacePosition(this.Position2));
		var normal = Orientations.Direction[this.Direction + (this.Room1 == null ? 3 : 0)].ToVector3();
		Gizmos.color = color;
		Gizmos.DrawLine(pos, pos + normal.normalized * 0.4f);
	}
}
