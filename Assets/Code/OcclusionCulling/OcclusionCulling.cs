using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[RequireComponent(typeof(MapBehaviour))]
public class OcclusionCulling : MonoBehaviour {
	public MapBehaviour MapBehaviour;
	public AbstractMap Map;

	private Dictionary<Vector3i, Room> roomsByPosition;

	private Dictionary<Vector3i, Portal[]> portalsByPosition;

	private HashSet<Vector3i> outdatedSlots;

	private Dictionary<Vector3i, Chunk> chunks;
	private List<Chunk> visibleChunks;

	public Camera Camera;
	public Plane[] cameraFrustumPlanes;

	public int ChunkSize = 3;

	public void Initialize() {
		this.MapBehaviour = this.GetComponent<MapBehaviour>();
		this.Map = this.MapBehaviour.Map;
		this.roomsByPosition = new Dictionary<Vector3i, Room>();
		this.portalsByPosition = new Dictionary<Vector3i, Portal[]>();
		this.outdatedSlots = new HashSet<Vector3i>();
		this.chunks = new Dictionary<Vector3i, Chunk>();
		this.visibleChunks = new List<Chunk>();
	}

	private Vector3i getChunkAddress(Vector3i position) {
		return new Vector3i(Mathf.FloorToInt((float)position.X / this.ChunkSize), Mathf.FloorToInt((float)position.Y / this.ChunkSize), Mathf.FloorToInt((float)position.Z / this.ChunkSize));
	}

	private Chunk getChunk(Vector3i chunkAddress) {
		if (this.chunks.ContainsKey(chunkAddress)) {
			return this.chunks[chunkAddress];
		}
		var center = this.MapBehaviour.GetWorldspacePosition(chunkAddress * this.ChunkSize) + (this.ChunkSize - 1) * 0.5f * AbstractMap.BLOCK_SIZE * Vector3.one;
		var chunk = new Chunk(new Bounds(center, Vector3.one * AbstractMap.BLOCK_SIZE * this.ChunkSize));
		this.chunks[chunkAddress] = chunk;
		this.visibleChunks.Add(chunk);
		return chunk;
	}

	public Chunk getChunkFromPosition(Vector3i position) {
		return this.getChunk(this.getChunkAddress(position));
	}

	public Room GetRoom(Vector3i position) {
		if (this.roomsByPosition.ContainsKey(position)) {
			return this.roomsByPosition[position];
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

			chunk.AddBlock(slot.GameObject.GetComponentsInChildren<Renderer>(), slot.Position);
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
			if (neighbor.Collapsed && this.roomsByPosition.ContainsKey(neighbor.Position) && !neighbor.Module.GetFace((i + 3) % 6).IsOcclusionPortal) {
				if (room == null) {
					room = this.roomsByPosition[neighbor.Position];
				} if (room != this.roomsByPosition[neighbor.Position]) {
					room = this.mergeRooms(this.roomsByPosition[neighbor.Position], room);
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
		}
		room.VisibilityOutdated = true;
		this.roomsByPosition[slot.Position] = room;

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
	}

	public void ClearOutdatedSlots() {
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

	private void removePortal(Portal portal) {
		this.portalsByPosition[portal.Position1][portal.Direction] = null;
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
		chunk.RemoveBlock(slot.Position);

		if (this.roomsByPosition.ContainsKey(slot.Position)) {
			var room = this.roomsByPosition[slot.Position];
			foreach (var portal in room.Portals.ToArray()) {
				this.removePortal(portal);
			}
			foreach (var roomSlot in room.Slots) {
				this.outdatedSlots.Add(roomSlot.Position);
				this.roomsByPosition.Remove(roomSlot.Position);
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
			this.roomsByPosition[slot.Position] = room2;
			room2.Slots.Add(slot);
		}
		room2.Renderers.AddRange(room1.Renderers);
		room2.VisibilityOutdated = true;
		foreach (var portal in room1.Portals) {
			room2.Portals.Add(portal);
		}
		this.removeRoom(room1);
		return room2;
	}

	private void ShowPortal(Portal portal, Room currentRoom) {
		portal.Draw(Color.green);
		portal.DrawFrustum(this.Camera.transform.position, Color.red);
		portal.Visible = true;
		var frustumPlanes = portal.GetFrustumPlanes(this.Camera.transform.position);
		if (portal.IsInside) {
			// Looking into another room
			var otherRoom = portal.Follow(currentRoom);
			otherRoom.SetVisibility(true);
			foreach (var roomPortal in otherRoom.Portals) {
				if (roomPortal != portal
					&& !roomPortal.Visible
					&& GeometryUtility.TestPlanesAABB(frustumPlanes, roomPortal.Bounds)
					&& GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, portal.Bounds)) {
					this.ShowPortal(roomPortal, otherRoom);
				}
			}
		} else {
			// Looking outside
			foreach (var chunk in this.visibleChunks) {
				if (GeometryUtility.TestPlanesAABB(frustumPlanes, chunk.Bounds) && GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, chunk.Bounds)) {
					chunk.SetExteriorVisibility(true);
					// TODO skip frustum test?
					// TODO skip visiblePortals check???
					foreach (var outsidePortal in chunk.Portals) {
						if (outsidePortal.IsInside 
							|| outsidePortal.Visible
							|| !GeometryUtility.TestPlanesAABB(frustumPlanes, outsidePortal.Bounds)
							|| !outsidePortal.FacesCamera()) {
							continue;
						}
						outsidePortal.Visible = true;
						var otherRoom = outsidePortal.Room;
						if (otherRoom == null) {
							continue;
						}
						otherRoom.SetVisibility(true);
						portal.Draw(Color.yellow);
						portal.DrawFrustum(this.Camera.transform.position, Color.cyan);
						foreach (var roomPortal in otherRoom.Portals) {
							if (roomPortal != portal && portal.IsInside && !roomPortal.Visible && GeometryUtility.TestPlanesAABB(frustumPlanes, roomPortal.Bounds)) {
								this.ShowPortal(roomPortal, otherRoom);
							}
						}
					}
				}				
			}			
		}
	}

	void Update() {
		var start = System.DateTime.Now;

		this.cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(this.Camera);
		var cameraPosition = this.MapBehaviour.GetMapPosition(this.Camera.transform.position);
		
		foreach (var chunk in this.visibleChunks) {
			chunk.SetRoomVisibility(false);
			foreach (var portal in chunk.Portals) {
				portal.Visible = false;
			}
		}

		if (this.roomsByPosition.ContainsKey(cameraPosition)) {
			// Camera is inside a room
			foreach (var chunk in this.visibleChunks) {
				chunk.SetExteriorVisibility(false);
			}
			var cameraRoom = this.roomsByPosition[cameraPosition];
			cameraRoom.SetVisibility(true);
						
			foreach (var portal in cameraRoom.Portals) {
				if (!portal.IsVisibleFromInside(this.Camera.transform.position)) {
					continue;
				}
				this.ShowPortal(portal, cameraRoom);
			}
		} else {
			// Camera is outside of any room
			foreach (var chunk in this.visibleChunks) {
				chunk.SetExteriorVisibility(true);
			}
			foreach (var chunk in this.visibleChunks) {
				if (!GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, chunk.Bounds)) {
					continue;
				}
				foreach (var portal in chunk.Portals) {
					if (portal.Room == null || portal.IsInside || portal.Visible) {
						continue;
					}
					if (portal.IsVisibleFromOutside()) {
						var room = portal.Room;
						room.SetVisibility(true);
						var frustumPlanes = portal.GetFrustumPlanes(this.Camera.transform.position);
						foreach (var roomPortal in room.Portals) {
							if (roomPortal != portal && !roomPortal.Visible && GeometryUtility.TestPlanesAABB(frustumPlanes, roomPortal.Bounds)) {
								this.ShowPortal(roomPortal, room);
							}
						}
					}
				}
			}
		}

		var time = (System.DateTime.Now - start).TotalMilliseconds;
		this.CullingTime = time;
	}

	public double CullingTime;

	void OnDisable() {
		foreach (var chunk in this.chunks.Values) {
			chunk.SetExteriorVisibility(true);
			chunk.SetRoomVisibility(true);
		}
	}

	private void addPortalToChunks(Portal portal) {
		var chunk1 = this.getChunkFromPosition(portal.Position1);
		chunk1.Portals.Add(portal);
		var chunk2 = this.getChunkFromPosition(portal.Position2);
		if (chunk2 != chunk1) {
			chunk2.Portals.Add(portal);
		}
	}

	private Portal getPortal(Vector3i position, int direction) {
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

	public bool ShowRooms = true;

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmo(OcclusionCulling occlusion, GizmoType gizmoType) {
		if (occlusion.visibleChunks == null) {
			return;
		}
		foreach (var chunk in occlusion.visibleChunks) {
			if (occlusion.ShowRooms) {
				foreach (var room in chunk.Rooms) {
					room.DrawGizmo(occlusion.MapBehaviour);
				}
			}
			foreach (var portal in chunk.Portals) {
				portal.DrawGizmo(occlusion.MapBehaviour, portal.IsInside ? Color.black : Color.red);
			}
		}		
	}
#endif
}
