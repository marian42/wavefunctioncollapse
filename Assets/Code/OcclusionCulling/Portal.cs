using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal {

	public readonly Vector3i Position1;
	public readonly Vector3i Position2;
	public int Direction;

	public Room Room1;
	public Room Room2;

	public bool IsInside {
		get {
			return this.Room1 != null && this.Room2 != null;
		}
	}

	// Direction must be 0, 1 or 2
	public Portal(Vector3i position, int direction) {
		this.Position1 = position;
		this.Direction = direction;
		this.Position2 = this.Position1 + Orientations.Direction[direction];
	}

	public void SetRoom(Vector3i position, Room room) {
		if (position == this.Position1) {
			if (this.Room1 != null) {
				this.Room1.Portals.Remove(this);
			}
			this.Room1 = room;
		} else if (position == this.Position2) {
			if (this.Room2 != null) {
				this.Room2.Portals.Remove(this);
			}
			this.Room2 = room;
		} else {
			throw new System.InvalidOperationException("Tried to assign a room to a portal for a slot position that the portal doesn't touch.");
		}
		room.Portals.Add(this);
	}

	public void RemoveRoom(Room room) {
		if (this.Room1 == room) {
			this.Room1 = null;
		}
		if (this.Room2 == room) {
			this.Room2 = null;
		}
	}
}
