using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class CullingData : MonoBehaviour {
	[HideInInspector]
	public MapBehaviour MapBehaviour;

	public Dictionary<Vector3Int, Room> RoomsByPosition;

	private Dictionary<Vector3Int, Portal[]> portalsByPosition;

	private HashSet<Vector3Int> outdatedSlots;

	public Dictionary<Vector3Int, Chunk> Chunks;
	public List<Chunk> ChunksInRange;

	public int ChunkSize = 3;

	public bool DrawGizmo = false;

	public void Initialize() {
		this.MapBehaviour = this.GetComponent<MapBehaviour>();
		this.RoomsByPosition = new Dictionary<Vector3Int, Room>();
		this.portalsByPosition = new Dictionary<Vector3Int, Portal[]>();
		this.outdatedSlots = new HashSet<Vector3Int>();
		this.Chunks = new Dictionary<Vector3Int, Chunk>();
		this.ChunksInRange = new List<Chunk>();
	}

	public Vector3Int GetChunkAddress(Vector3Int position) {
		return Vector3Int.FloorToInt(position.ToVector3() / this.ChunkSize);
	}

	public Vector3 GetChunkCenter(Vector3Int chunkAddress) {
		return this.MapBehaviour.GetWorldspacePosition(chunkAddress * this.ChunkSize) + (this.ChunkSize - 1) * 0.5f * AbstractMap.BLOCK_SIZE * Vector3.one;
	}

	private Chunk getChunk(Vector3Int chunkAddress) {
		if (this.Chunks.ContainsKey(chunkAddress)) {
			return this.Chunks[chunkAddress];
		}
		var chunk = new Chunk(new Bounds(this.GetChunkCenter(chunkAddress), Vector3.one * AbstractMap.BLOCK_SIZE * this.ChunkSize));
		this.Chunks[chunkAddress] = chunk;
		this.ChunksInRange.Add(chunk);
		return chunk;
	}

	public Chunk getChunkFromPosition(Vector3Int position) {
		return this.getChunk(this.GetChunkAddress(position));
	}

	public Room GetRoom(Vector3Int position) {
		if (this.RoomsByPosition.ContainsKey(position)) {
			return this.RoomsByPosition[position];
		} else {
			return null;
		}
	}

	public void AddSlot(Slot slot) {
		if (!slot.Collapsed) {
			return;
		}
		var chunk = this.getChunkFromPosition(slot.Position);
		if (!slot.Module.Prototype.IsInterior) {
			for (int i = 0; i < 6; i++) {
				var face = slot.Module.GetFace(i);
				if (face.IsOcclusionPortal) {
					var portal = this.getPortal(slot.Position, i);
					if (portal.Room != null && !portal.Room.Portals.Contains(portal)) {
						portal.Room.Portals.Add(portal);
					}
				}
			}

			chunk.AddBlock(slot);
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
			if (neighbor.Collapsed && this.RoomsByPosition.ContainsKey(neighbor.Position) && !neighbor.Module.GetFace((i + 3) % 6).IsOcclusionPortal) {
				if (room == null) {
					room = this.RoomsByPosition[neighbor.Position];
				} if (room != this.RoomsByPosition[neighbor.Position]) {
					room = this.mergeRooms(this.RoomsByPosition[neighbor.Position], room);
				}
			}
		}
		if (room == null) {
			room = new Room();
			chunk.Rooms.Add(room);
		}
		room.Slots.Add(slot);
		foreach (var renderer in slot.GameObject.GetComponentsInChildren<Renderer>()) {
			room.Renderers.Add(renderer);
			renderer.enabled = room.Visible;
		}
		this.RoomsByPosition[slot.Position] = room;

		for (int i = 0; i < 6; i++) {
			var face = slot.Module.GetFace(i);
			if (face.Connector == 1) {
				continue;
			}
			var neighbor = slot.GetNeighbor(i);
			if (face.IsOcclusionPortal || (neighbor != null && neighbor.Collapsed && (!neighbor.Module.Prototype.IsInterior || neighbor.Module.GetFace((i + 3) % 6).IsOcclusionPortal))) {
				var portal = this.getPortal(slot.Position, i);
				room.Portals.Add(portal);
				var otherRoom = portal.Follow(room);
				if (otherRoom != null && !otherRoom.Portals.Contains(portal)) {
					otherRoom.Portals.Add(portal);
				}
			}
		}
		this.updatePortals(slot.Position, room);
	}

	private void updatePortals(Vector3Int position, Room room) {
		Portal[] portals = null;
		if (this.portalsByPosition.TryGetValue(position, out portals)) {
			for (int i = 0; i < 3; i++) {
				if (portals[i] != null) {
					portals[i].Room1 = room;
				}
			}
		}
		for (int i = 0; i < 3; i++) {
			var neighborPosition = position + Orientations.Direction[3 + i];
			if (this.portalsByPosition.TryGetValue(neighborPosition, out portals) && portals[i] != null) {
				portals[i].Room2 = room;
			}
		}
	}

	public void ClearOutdatedSlots() {
		if (!this.outdatedSlots.Any()) {
			return;
		}
		var items = this.outdatedSlots.ToArray();
		this.outdatedSlots.Clear();
		foreach (var position in items) {
			var slot = this.MapBehaviour.Map.GetSlot(position);
			if (slot == null || !slot.Collapsed) {
				continue;
			}
			this.AddSlot(slot);
		}
	}

	private void removePortal(Portal portal) {
		if (this.portalsByPosition.ContainsKey(portal.Position1)) {
			this.portalsByPosition[portal.Position1][portal.Direction] = null;
			if (this.portalsByPosition[portal.Position1].All(p => p == null)) {
				this.portalsByPosition.Remove(portal.Position1);
			}
		}		
		this.getChunkFromPosition(portal.Position1).Portals.Remove(portal);
		this.getChunkFromPosition(portal.Position2).Portals.Remove(portal);
		if (portal.Room1 != null) {
			portal.Room1.Portals.Remove(portal);
		}
		if (portal.Room2 != null) {
			portal.Room2.Portals.Remove(portal);
		}
	}

	public void RemoveSlot(Slot slot) {
		var chunk = this.getChunkFromPosition(slot.Position);
		chunk.RemoveBlock(slot);

		if (this.RoomsByPosition.ContainsKey(slot.Position)) {
			var room = this.RoomsByPosition[slot.Position];
			foreach (var portal in room.Portals.ToArray()) {
				this.removePortal(portal);
			}
			foreach (var roomSlot in room.Slots) {
				this.outdatedSlots.Add(roomSlot.Position);
				this.RoomsByPosition.Remove(roomSlot.Position);
			}
			this.removeRoom(room);
		}
		this.outdatedSlots.Remove(slot.Position);
	}

	private void removeRoom(Room room) {
		foreach (var slot in room.Slots) {
			var chunk = this.getChunkFromPosition(slot.Position);
			if (chunk.Rooms.Contains(room)) {
				chunk.Rooms.Remove(room);
			}
		}
	}

	private Room mergeRooms(Room room1, Room room2) {
		foreach (var slot in room1.Slots) {
			this.RoomsByPosition[slot.Position] = room2;
			room2.Slots.Add(slot);
		}
		room2.Renderers.AddRange(room1.Renderers);
		room2.VisibilityOutdated = true;
		foreach (var portal in room1.Portals) {
			portal.ReplaceRoom(room1, room2);
			room2.Portals.Add(portal);
		}
		this.removeRoom(room1);
		return room2;
	}
	
	private void addPortalToChunks(Portal portal) {
		var chunk1 = this.getChunkFromPosition(portal.Position1);
		chunk1.Portals.Add(portal);
		var chunk2 = this.getChunkFromPosition(portal.Position2);
		if (chunk2 != chunk1) {
			chunk2.Portals.Add(portal);
		}
	}

	private Portal getPortal(Vector3Int position, int direction) {
		if (direction >= 3) {
			position = position + Orientations.Direction[direction];
			direction -= 3;
		}
		if (this.portalsByPosition.ContainsKey(position)) {
			var array = this.portalsByPosition[position];
			if (array[direction] == null) {
				var portal = new Portal(position, direction, this);
				array[direction] = portal;
				this.addPortalToChunks(portal);
				return portal;
			} else {
				return array[direction];
			}
		} else {
			var portal = new Portal(position, direction, this);
			var array = new Portal[3];
			array[direction] = portal;
			this.portalsByPosition[position] = array;
			this.addPortalToChunks(portal);
			return portal;
		}
	}

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmos(CullingData cullingData, GizmoType gizmoType) {
		if (!cullingData.DrawGizmo || cullingData.ChunksInRange == null) {
			return;
		}
		foreach (var chunk in cullingData.ChunksInRange) {
			foreach (var room in chunk.Rooms) {
				room.DrawGizmo(cullingData.MapBehaviour);
			}
		}
	}
#endif
}
