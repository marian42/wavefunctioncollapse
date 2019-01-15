using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal {

	public readonly Vector3Int Position1;
	public readonly Vector3Int Position2;
	public readonly int Direction;

	public readonly Bounds Bounds;

	public Room Room1;
	public Room Room2;

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
	public Portal(Vector3Int position, int direction, CullingData cullingData) {
		this.Position1 = position;
		this.Direction = direction;
		this.Position2 = this.Position1 + Orientations.Direction[direction];

		this.Room1 = cullingData.GetRoom(this.Position1);
		this.Room2 = cullingData.GetRoom(this.Position2);

		var dir = Orientations.Direction[this.Direction].ToVector3();
		this.Bounds = new Bounds(cullingData.MapBehaviour.GetWorldspacePosition(this.Position1) + dir, (Vector3.one - new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z)) * 0.8f) * AbstractMap.BLOCK_SIZE);
	}

	public void ReplaceRoom(Room oldRoom, Room newRoom) {
		if (this.Room1 == oldRoom) {
			this.Room1 = newRoom;
		}
		if (this.Room2 == oldRoom) {
			this.Room2 = newRoom;
		}
	}

	public Room Follow(Room currentRoom) {
		if (this.Room1 == currentRoom) {
			return this.Room2;
		} else {
			return this.Room1;
		}
	}

	public Room Follow(Vector3 cameraPosition) {
		var normal = Orientations.Direction[this.Direction].ToVector3();
		var lookDirection = this.Bounds.center - cameraPosition;
		if (Vector3.Dot(normal, lookDirection) > 0) {
			return this.Room2;
		} else {
			return this.Room1;
		}
	}

	public bool FacesCamera(Vector3 cameraPosition) {
		var normal = Orientations.Direction[this.Direction + (this.Room1 == null ? 3 : 0)].ToVector3();
		var lookDirection = this.Bounds.center - cameraPosition;
		return Vector3.Dot(normal, lookDirection) < 0;
	}

	public bool FacesCamera(Room currentRoom, Vector3 cameraPosition) {
		var normal = Orientations.Direction[this.Direction + (this.Room1 == currentRoom ? 3 : 0)].ToVector3();
		var lookDirection = this.Bounds.center - cameraPosition;
		return Vector3.Dot(normal, lookDirection) < 0;
	}

	private Vector3[] getCorners() {
		Vector3 center = this.Bounds.center;

		switch (this.Direction) {
			case 0:
				return new Vector3[] {
					center + new Vector3(0, +1, +1), 
					center + new Vector3(0, +1, -1),
					center + new Vector3(0, -1, -1),
					center + new Vector3(0, -1, +1)
				};
			case 1:
				return new Vector3[] {
					center + new Vector3(+1, 0, +1),
					center + new Vector3(+1, 0, -1),
					center + new Vector3(-1, 0, -1),
					center + new Vector3(-1, 0, +1)
				};
			case 2:
				return new Vector3[] {
					center + new Vector3(+1, +1, 0),
					center + new Vector3(+1, -1, 0),
					center + new Vector3(-1, -1, 0),
					center + new Vector3(-1, +1, 0),
				};
		}
		throw new System.InvalidOperationException("Portal.Direction must be 0, 1 or 2.");
	}

	public void Draw(Color color) {
		this.Bounds.Draw(color);
	}

	public void DrawFrustum(Vector3 cameraPosition, Color color) {
		var corners = this.getCorners();
		for (int i = 0; i < 4; i++) {
			var dir = (corners[i] - cameraPosition).normalized;
			Debug.DrawLine(corners[i], corners[i] + dir * 10f, color);
		}
	}

	public Plane[] GetFrustumPlanes(Vector3 cameraPosition) {
		var planes = new Plane[5];

		Vector3 center = this.Bounds.center;

		var corners = this.getCorners();

		for (int i = 0; i < 4; i++) {
			var normal = Vector3.Cross((corners[i] - cameraPosition).normalized, (corners[i] - corners[(i + 1) % 4]).normalized);
			if (Vector3.Dot(normal, (center - corners[i]).normalized) < 0) {
				normal *= -1f;
			}

			planes[i] = new Plane(normal, cameraPosition + normal * 0.01f);
		}

		var portalNormal = Orientations.Direction[this.Direction].ToVector3();
		if (Vector3.Dot(portalNormal, (center - cameraPosition).normalized) < 0) {
			portalNormal *= -1f;
		}
		planes[4] = new Plane(portalNormal, center);

		return planes;
	}
}
