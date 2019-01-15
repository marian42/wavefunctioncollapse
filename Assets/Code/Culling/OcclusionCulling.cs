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
	
	public void ShowOutside(Plane[] frustumPlanes) {
		foreach (var chunk in this.cullingData.ChunksInRange) {
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
					|| !outsidePortal.FacesCamera(this.Camera.transform.position)) {
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
		var room = portal.Follow(this.Camera.transform.position);

		if (room != null) {
			// Looking into a room
			room.SetVisibility(true);
			foreach (var roomPortal in room.Portals) {
				if (roomPortal != portal
					&& roomPortal.FacesCamera(room, this.Camera.transform.position)
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
		this.cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(this.Camera);
		var cameraPosition = this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position);

		bool insideRoom = this.cullingData.RoomsByPosition.ContainsKey(cameraPosition);
		foreach (var chunk in this.cullingData.ChunksInRange) {
			chunk.SetRoomVisibility(false);
			chunk.SetExteriorVisibility(!insideRoom);
		}
		
		if (insideRoom) {
			var cameraRoom = this.cullingData.RoomsByPosition[cameraPosition];
			cameraRoom.SetVisibility(true);
						
			foreach (var portal in cameraRoom.Portals) {
				if ((GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, portal.Bounds) || portal.Bounds.Contains(cameraPosition))
					&& portal.FacesCamera(cameraRoom, this.Camera.transform.position)) {
					this.ShowPortal(portal);
				}
			}
		} else {
			this.ShowOutside(null);
		}
	}

	void OnDisable() {
		foreach (var chunk in this.cullingData.ChunksInRange) {
			chunk.SetExteriorVisibility(true);
			chunk.SetRoomVisibility(true);
		}
	}
}
