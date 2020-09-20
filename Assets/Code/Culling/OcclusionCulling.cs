using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[RequireComponent(typeof(MapBehaviour))]
[RequireComponent(typeof(CullingData))]
public class OcclusionCulling : MonoBehaviour {
	public Camera Camera;

	private CullingData cullingData;
	private Plane[] cameraFrustumPlanes;

	public void OnEnable() {
		this.cullingData = this.GetComponent<CullingData>();
	}
	
	public void ShowOutside(Plane[] frustumPlanes, int directionMask) {
		foreach (var chunk in this.cullingData.ChunksInRange) {
			if ((frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, chunk.Bounds)) || !GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, chunk.Bounds)) {
				continue;
			}
			chunk.SetExteriorVisibility(true);
			foreach (var outsidePortal in chunk.Portals) {
				var room = outsidePortal.Room;
				if (room == null
					|| outsidePortal.IsInside
					|| (frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, outsidePortal.Bounds))
					|| !GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, outsidePortal.Bounds)
					|| !outsidePortal.FacesCamera(this.Camera.transform.position)) {
					continue;
				}

				bool positiveAxis = outsidePortal.Room1 == null;
				int positiveBitmask = 1 << outsidePortal.Direction;
				int negativeBitmask = 8 << outsidePortal.Direction;
				if (!positiveAxis && (directionMask & positiveBitmask) != 0
					|| positiveAxis && (directionMask & negativeBitmask) != 0) {
					continue;
				}

				room.SetVisibility(true);
				this.ShowPortal(outsidePortal, room, positiveAxis ? directionMask | positiveBitmask : directionMask | negativeBitmask, true);
			}
		}
	}

	/// <summary>
	/// Shows the part of the world that can be seen through the portal.
	/// </summary>
	/// <param name="portal"></param> The portal we're looking through
	/// <param name="room"></param> The room that becomes visible by looking through the portal or null if looking to the outside through the supplied portal
	/// <param name="directionMask"></param> The first six bits are used to indicate if the call stack hase "gone" through a portal in the Left, Down, Back, Right, ... directions.
	/// After going through a portal in one direction, portals facing the opposite direction will be ignored.
	/// <param name="stayInside"></param> After handling an outside frustum area and going inside, don't go back out again, as that area has already been handled.
	private void ShowPortal(Portal portal, Room room, int directionMask, bool stayInside) {
#if UNITY_EDITOR
		if (this.cullingData.DrawGizmo) {
			portal.Draw(Color.green);
			portal.DrawFrustum(this.Camera.transform.position, Color.red);
			portal.Bounds.Draw(Color.green);
		}
#endif
		var frustumPlanes = portal.GetFrustumPlanes(this.Camera.transform.position);

		if (room != null) {
			// Looking into a room
			room.SetVisibility(true);
			foreach (var roomPortal in room.Portals) {
				if (roomPortal != portal
					&& GeometryUtility.TestPlanesAABB(frustumPlanes, roomPortal.Bounds)) {

					bool positiveAxis = room == roomPortal.Room1;
					int positiveBitmask = 1 << roomPortal.Direction;
					int negativeBitmask = 8 << roomPortal.Direction;
					if (!positiveAxis && (directionMask & positiveBitmask) != 0
						|| positiveAxis && (directionMask & negativeBitmask) != 0) {
							continue;
					}

					this.ShowPortal(roomPortal, roomPortal.Follow(room), positiveAxis ? directionMask | positiveBitmask : directionMask | negativeBitmask, stayInside);
				}
			}
		} else if (!stayInside) {
			// Looking outside
			this.ShowOutside(frustumPlanes, directionMask);
		}
	}

	void Update() {
		this.cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(this.Camera);
		var cameraPosition = this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position);

		bool cameraIsInsideRoom = this.cullingData.RoomsByPosition.ContainsKey(cameraPosition);
		foreach (var chunk in this.cullingData.ChunksInRange) {
			chunk.SetRoomVisibility(false);
			chunk.SetExteriorVisibility(!cameraIsInsideRoom);
		}
		
		if (cameraIsInsideRoom) {
			var cameraRoom = this.cullingData.RoomsByPosition[cameraPosition];
			cameraRoom.SetVisibility(true);
						
			foreach (var portal in cameraRoom.Portals) {
				if ((GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, portal.Bounds) || portal.Bounds.Contains(cameraPosition))
					&& portal.FacesCamera(cameraRoom, this.Camera.transform.position)) {
					this.ShowPortal(portal, portal.Follow(cameraRoom), portal.Room1 == cameraRoom ? 1 << portal.Direction : 8 << portal.Direction, false);
				}
			}
		} else {
			this.ShowOutside(null, 0);
		}
	}

	void OnDisable() {
		foreach (var chunk in this.cullingData.Chunks.Values) {
			chunk.SetExteriorVisibility(true);
			chunk.SetRoomVisibility(true);
		}
	}
}
