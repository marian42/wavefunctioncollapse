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
	private List<Chunk> chunksInRange;

	public Camera Camera;
	public Plane[] cameraFrustumPlanes;

	public int ChunkSize = 3;

	public float RenderRange;
	private Vector3i previousChunkAddress;

	public void Initialize() {
		this.MapBehaviour = this.GetComponent<MapBehaviour>();
		this.Map = this.MapBehaviour.Map;
		this.roomsByPosition = new Dictionary<Vector3i, Room>();
		this.portalsByPosition = new Dictionary<Vector3i, Portal[]>();
		this.outdatedSlots = new HashSet<Vector3i>();
		this.chunks = new Dictionary<Vector3i, Chunk>();
		this.chunksInRange = new List<Chunk>();
	}

	private Vector3i getChunkAddress(Vector3i position) {
		return new Vector3i(Mathf.FloorToInt((float)position.X / this.ChunkSize), Mathf.FloorToInt((float)position.Y / this.ChunkSize), Mathf.FloorToInt((float)position.Z / this.ChunkSize));
	}

	private Vector3 getChunkCenter(Vector3i chunkAddress) {
		return this.MapBehaviour.GetWorldspacePosition(chunkAddress * this.ChunkSize) + (this.ChunkSize - 1) * 0.5f * AbstractMap.BLOCK_SIZE * Vector3.one;
	}

	private Chunk getChunk(Vector3i chunkAddress) {
		if (this.chunks.ContainsKey(chunkAddress)) {
			return this.chunks[chunkAddress];
		}
		var chunk = new Chunk(new Bounds(this.getChunkCenter(chunkAddress), Vector3.one * AbstractMap.BLOCK_SIZE * this.ChunkSize));
		this.chunks[chunkAddress] = chunk;
		this.chunksInRange.Add(chunk);
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
			renderer.enabled = room.Visible;
		}
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
		chunk.RemoveBlock(slot);

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

	public void ShowOutside(Plane[] frustumPlanes) {
		foreach (var chunk in this.chunksInRange) {
			if ((frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, chunk.Bounds)) || !GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, chunk.Bounds)) {
				continue;
			}
			chunk.SetExteriorVisibility(true);
			foreach (var outsidePortal in chunk.Portals) {
				var otherRoom = outsidePortal.Room;
				if (otherRoom == null
					|| outsidePortal.IsInside
					|| (frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, outsidePortal.Bounds))
					|| !GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, outsidePortal.Bounds)
					|| !outsidePortal.FacesCamera()) {
					continue;
				}

				otherRoom.SetVisibility(true);
				this.ShowPortal(outsidePortal, true);
			}
		}
	}

	private void ShowPortal(Portal portal, bool insideOnly = false) {
#if UNITY_EDITOR
		portal.Draw(Color.green);
		portal.DrawFrustum(this.Camera.transform.position, Color.red);
		portal.Bounds.Draw(Color.green);
#endif
		var frustumPlanes = portal.GetFrustumPlanes(this.Camera.transform.position);
		var room = portal.Follow();

		if (room != null) {
			// Looking into a room
			room.SetVisibility(true);
			foreach (var roomPortal in room.Portals) {
				if (roomPortal != portal
					&& roomPortal.FacesCamera(room)
					&& GeometryUtility.TestPlanesAABB(frustumPlanes, roomPortal.Bounds)
					&& GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, portal.Bounds)) {
					this.ShowPortal(roomPortal, insideOnly);
				}
			}
		} else if (!insideOnly) {
			// Looking outside
			this.ShowOutside(frustumPlanes);
		}
	}

	void Update() {
		var start = System.DateTime.Now;

		this.updateRenderRange();

		this.cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(this.Camera);
		var cameraPosition = this.MapBehaviour.GetMapPosition(this.Camera.transform.position);

		bool insideRoom = this.roomsByPosition.ContainsKey(cameraPosition);
		foreach (var chunk in this.chunksInRange) {
			chunk.SetRoomVisibility(false);
			chunk.SetExteriorVisibility(!insideRoom);
		}
		
		if (insideRoom) {
			var cameraRoom = this.roomsByPosition[cameraPosition];
			cameraRoom.SetVisibility(true);
						
			foreach (var portal in cameraRoom.Portals) {
				if (portal.IsVisibleFromInside(this.Camera.transform.position) && portal.FacesCamera(cameraRoom)) {
					this.ShowPortal(portal);
				}
			}
		} else {
			this.ShowOutside(null);
		}

		var time = (System.DateTime.Now - start).TotalMilliseconds;
		this.CullingTime = time;
	}

	private void updateRenderRange() {
		var cameraPosition = this.Camera.transform.position;
		for (int i = 0; i < this.chunksInRange.Count; i++) {
			if (Vector3.Distance(this.chunksInRange[i].Bounds.center, cameraPosition) > this.RenderRange) {
				this.chunksInRange[i].SetInRenderRange(false);
				this.chunksInRange.RemoveAt(i);
				i--;
			}
		}

		var currentChunkAddress = this.getChunkAddress(this.MapBehaviour.GetMapPosition(this.Camera.transform.position));
		if (currentChunkAddress == this.previousChunkAddress) {
			return;
		}
		this.previousChunkAddress = currentChunkAddress;

		int chunkCount = (int)(this.RenderRange / (AbstractMap.BLOCK_SIZE * this.ChunkSize));
		for (int x = currentChunkAddress.X - chunkCount; x <= currentChunkAddress.X + chunkCount; x++) {
			for (int y = 0; y < Mathf.CeilToInt((float)this.MapBehaviour.MapHeight / this.ChunkSize); y++) {
				for (int z = currentChunkAddress.Z - chunkCount; z <= currentChunkAddress.Z + chunkCount; z++) {
					var address = new Vector3i(x, y, z);
					if (Vector3.Distance(this.Camera.transform.position, this.getChunkCenter(address)) > this.RenderRange) {
						continue;
					}
					if (!this.chunks.ContainsKey(address)) {
						continue;
					}
					var chunk = this.chunks[address];
					if (chunk.InRenderRange) {
						continue;
					}
					chunk.SetInRenderRange(true);
					this.chunksInRange.Add(chunk);
				}
			}
		}
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

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmo(OcclusionCulling occlusion, GizmoType gizmoType) {
		if (occlusion.chunksInRange == null) {
			return;
		}
		foreach (var chunk in occlusion.chunksInRange) {
			foreach (var room in chunk.Rooms) {
				room.DrawGizmo(occlusion.MapBehaviour);
			}
		}
	}
#endif
}
