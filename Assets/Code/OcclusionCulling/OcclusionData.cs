using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class OcclusionData {
	public readonly AbstractMap Map;

	private List<Room> rooms;

	private Dictionary<Vector3i, Room> roomsByPosition;

	private Dictionary<Vector3i, Portal[]> portalsByPosition;

	public List<Portal> OutsideFacingPortals;

	private HashSet<Vector3i> outdatedSlots;

	public OcclusionData(AbstractMap map) {
		this.Map = map;
		this.rooms = new List<Room>();
		this.roomsByPosition = new Dictionary<Vector3i, Room>();
		this.portalsByPosition = new Dictionary<Vector3i, Portal[]>();
		this.OutsideFacingPortals = new List<Portal>();
		this.outdatedSlots = new HashSet<Vector3i>();
	}

	public void AddSlot(Slot slot) {
		if (!slot.Module.Prototype.IsInterior) {
			return;
		}

		Room room = null;
		for (int i = 0; i < 6; i++) {
			var face = slot.Module.GetFace(i);
			if (face.Connector == 1 || face.IsOcclusionPortal) {
				continue;
			}
			var neighbor = slot.GetNeighbor(i);
			if (neighbor == null) {
				continue;
			}
			if (neighbor.Collapsed && this.roomsByPosition.ContainsKey(neighbor.Position)) {
				if (room == null) {
					room = this.roomsByPosition[neighbor.Position];
				} else {
					room = this.mergeRooms(this.roomsByPosition[neighbor.Position], room);
				}
			}
		}
		if (room == null) {
			room = new Room();
			this.rooms.Add(room);
		}
		room.Slots.Add(slot);
		this.roomsByPosition[slot.Position] = room;

		for (int i = 0; i < 6; i++) {
			var face = slot.Module.GetFace(i);
			if (!face.IsOcclusionPortal) {
				continue;
			}
			var portal = this.getPortal(slot.Position, i);
			portal.SetRoom(slot.Position, room);
			if (portal.IsInside) {
				this.OutsideFacingPortals.Remove(portal);
			}
		}

		this.clearOutdatedSlots();
	}

	private void clearOutdatedSlots() {
		if (!this.outdatedSlots.Any()) {
			return;
		}
		var items = this.outdatedSlots.ToArray();
		this.outdatedSlots.Clear();
		foreach (var position in items) {
			var slot = this.Map.GetSlot(position);
			if (slot == null || !slot.Collapsed) {
				continue;
			}
			this.AddSlot(slot);
		}
	}

	public void RemoveSlot(Vector3i position) {
		if (this.roomsByPosition.ContainsKey(position)) {
			var room = this.roomsByPosition[position];
			foreach (var slot in room.Slots) {
				this.outdatedSlots.Add(slot.Position);
				this.roomsByPosition.Remove(slot.Position);
			}
			this.rooms.Remove(room);
			foreach (var portal in room.Portals) {
				if (!portal.IsInside) {
					this.portalsByPosition[portal.Position1][portal.Direction] = null;
					this.OutsideFacingPortals.Remove(portal);
				} else {
					portal.RemoveRoom(room);
					this.OutsideFacingPortals.Add(portal);
				}
			}
		}
		this.outdatedSlots.Remove(position);
	}

	private Room mergeRooms(Room room1, Room room2) {
		foreach (var slot in room1.Slots) {
			this.roomsByPosition[slot.Position] = room2;
			room2.Slots.Add(slot);
		}
		this.rooms.Remove(room1);
		return room2;
	}

	private Portal getPortal(Vector3i position, int direction) {
		if (direction >= 3) {
			position = position + Orientations.Direction[direction];
			direction -= 3;
		}
		if (this.portalsByPosition.ContainsKey(position)) {
			var array = this.portalsByPosition[position];
			if (array[direction] == null) {
				var portal = new Portal(position, direction);
				array[direction] = portal;
				this.OutsideFacingPortals.Add(portal);
				return portal;
			} else {
				return array[direction];
			}
		} else {
			var portal = new Portal(position, direction);
			var array = new Portal[3];
			array[direction] = portal;
			this.portalsByPosition[position] = array;
			this.OutsideFacingPortals.Add(portal);
			return portal;
		}
	}
}
