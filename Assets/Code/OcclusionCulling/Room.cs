using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Room {
	public HashSet<Slot> Slots;
	public List<Portal> Portals;

	public readonly Color Color;

	public Room() {
		this.Slots = new HashSet<Slot>();
		this.Portals = new List<Portal>();
		this.Color = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
	}

#if UNITY_EDITOR
	public void DrawGizmo(MapBehaviour map) {
		Gizmos.color = this.Color;

		foreach (var slot in this.Slots) {
			Gizmos.DrawWireCube(map.GetWorldspacePosition(slot.Position), Vector3.one * 2f);
		}
	}
#endif
}
