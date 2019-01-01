using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[RequireComponent(typeof(MapBehaviour))]
public class OcclusionCulling : MonoBehaviour {
	public MapBehaviour MapBehaviour;
	public AbstractMap Map;

	private List<Room> rooms;

	private Dictionary<Vector3i, Room> roomsByPosition;

	private Dictionary<Vector3i, Portal[]> portalsByPosition;

	public List<Portal> Portals;

	private HashSet<Vector3i> outdatedSlots;

	public Dictionary<Vector3i, ExteriorBlock> exteriorBlocks;

	private HashSet<Portal> visiblePortals;

	public Camera Camera;
	public Plane[] cameraFrustumPlanes;

	public void Initialize() {
		this.MapBehaviour = this.GetComponent<MapBehaviour>();
		this.Map = this.MapBehaviour.Map;
		this.rooms = new List<Room>();
		this.roomsByPosition = new Dictionary<Vector3i, Room>();
		this.portalsByPosition = new Dictionary<Vector3i, Portal[]>();
		this.Portals = new List<Portal>();
		this.outdatedSlots = new HashSet<Vector3i>();
		this.exteriorBlocks = new Dictionary<Vector3i, ExteriorBlock>();
		this.visiblePortals = new HashSet<Portal>();
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

			this.exteriorBlocks[slot.Position] = new ExteriorBlock(slot, this.MapBehaviour);
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
			this.rooms.Add(room);
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
		this.Portals.Remove(portal);
		if (portal.Room1 != null) {
			portal.Room1.Portals.Remove(portal);
		}
		if (portal.Room2 != null) {
			portal.Room2.Portals.Remove(portal);
		}
	}

	public void RemoveSlot(Slot slot) {
		this.exteriorBlocks.Remove(slot.Position);

		if (this.roomsByPosition.ContainsKey(slot.Position)) {
			var room = this.roomsByPosition[slot.Position];
			foreach (var portal in room.Portals.ToArray()) {
				this.removePortal(portal);
			}
			foreach (var roomSlot in room.Slots) {
				this.outdatedSlots.Add(roomSlot.Position);
				this.roomsByPosition.Remove(roomSlot.Position);
			}
			this.rooms.Remove(room);
		}
		this.outdatedSlots.Remove(slot.Position);
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
		this.rooms.Remove(room1);
		return room2;
	}

	private void ShowPortal(Portal portal, Room currentRoom) {
		this.visiblePortals.Add(portal);
		var frustumPlanes = portal.GetFrustumPlanes(this.Camera.transform.position);
		if (portal.IsInside) {
			// Looking into another room
			var otherRoom = portal.Follow(currentRoom);
			otherRoom.SetVisibility(true);
			foreach (var roomPortal in otherRoom.Portals) {
				if (roomPortal != portal && !this.visiblePortals.Contains(roomPortal) && GeometryUtility.TestPlanesAABB(frustumPlanes, roomPortal.Bounds)) {
					this.ShowPortal(roomPortal, otherRoom);
				}
			}
		} else {
			// Looking outside
			foreach (var outsideBlock in this.exteriorBlocks.Values) {
				if (!outsideBlock.Visible && GeometryUtility.TestPlanesAABB(frustumPlanes, outsideBlock.Bounds)) {
					outsideBlock.SetVisibility(true);
				}
			}
			foreach (var outsidePortal in this.Portals) {
				if (outsidePortal.IsInside || this.visiblePortals.Contains(outsidePortal) || !GeometryUtility.TestPlanesAABB(frustumPlanes, outsidePortal.Bounds)) {
					continue;
				}
				var otherRoom = outsidePortal.Room;
				if (otherRoom == null) {
					continue;
				}
				otherRoom.SetVisibility(true);
				foreach (var roomPortal in otherRoom.Portals) {
					if (roomPortal != portal && !this.visiblePortals.Contains(roomPortal) && GeometryUtility.TestPlanesAABB(frustumPlanes, roomPortal.Bounds)) {
						this.ShowPortal(roomPortal, otherRoom);
					}
				}
			}
		}
	}

	void Update() {
		this.cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(this.Camera);
		var cameraPosition = this.MapBehaviour.GetMapPosition(this.Camera.transform.position);
		
		foreach (var room in this.rooms) {
			room.SetVisibility(false);
		}

		this.visiblePortals.Clear();

		if (this.roomsByPosition.ContainsKey(cameraPosition)) {
			// Camera is inside a room
			foreach (var exteriorBlock in this.exteriorBlocks.Values) {
				exteriorBlock.SetVisibility(false);
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
			foreach (var exteriorBlock in this.exteriorBlocks.Values) {
				exteriorBlock.SetVisibility(true);
			}
			foreach (var portal in this.Portals) {
				if (portal.Room == null || portal.IsInside || this.visiblePortals.Contains(portal)) {
					continue;
				}
				if (portal.IsVisibleFromOutside()) {
					var room = portal.Room;
					room.SetVisibility(true);
					var frustumPlanes = portal.GetFrustumPlanes(this.Camera.transform.position);
					foreach (var roomPortal in room.Portals) {
						if (roomPortal != portal && !this.visiblePortals.Contains(roomPortal) && GeometryUtility.TestPlanesAABB(frustumPlanes, roomPortal.Bounds)) {
							this.ShowPortal(roomPortal, room);
						}
					}
				}				
			}
		}	
	}

	void OnDisable() {
		foreach (var room in this.rooms) {
			room.SetVisibility(true);
		}
		foreach (var block in this.exteriorBlocks.Values) {
			block.SetVisibility(true);
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
				this.Portals.Add(portal);
				return portal;
			} else {
				return array[direction];
			}
		} else {
			var portal = new Portal(position, direction, this);
			var array = new Portal[3];
			array[direction] = portal;
			this.portalsByPosition[position] = array;
			this.Portals.Add(portal);
			return portal;
		}
	}

	public bool ShowRooms = true;

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmo(OcclusionCulling occlusion, GizmoType gizmoType) {
		if (occlusion.rooms == null || occlusion.Portals == null) {
			return;
		}
		if (occlusion.ShowRooms) {
			foreach (var room in occlusion.rooms) {
				room.DrawGizmo(occlusion.MapBehaviour);
			}
		}
		foreach (var portals in occlusion.portalsByPosition.Values) {
			foreach (var portal in portals.Where(p => p != null)) {
				portal.DrawGizmo(occlusion.MapBehaviour, portal.IsInside ? Color.black : Color.red);
			}
		}
	}
#endif
}
