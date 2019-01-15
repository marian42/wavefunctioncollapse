using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Room {
	public HashSet<Slot> Slots;
	public List<Portal> Portals;

	public readonly Color Color;

	public bool Visible;
	public bool VisibilityOutdated;

	public List<Renderer> Renderers;

	public Room() {
		this.Slots = new HashSet<Slot>();
		this.Portals = new List<Portal>();
		this.Renderers = new List<Renderer>();
		this.Color = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
		this.Visible = !Application.isPlaying;
	}

	public void SetVisibility(bool visible) {
		if (!this.VisibilityOutdated && visible == this.Visible) {
			return;
		}
		this.VisibilityOutdated = false;
		this.Visible = visible;
		foreach (var renderer in this.Renderers) {
			renderer.enabled = visible;
		}
	}

#if UNITY_EDITOR
	public void DrawGizmo(MapBehaviour map) {
		if (!this.Visible || this.VisibilityOutdated) {
			return;
		}
		Gizmos.color = this.Color;

		foreach (var slot in this.Slots) {
			Gizmos.DrawWireCube(map.GetWorldspacePosition(slot.Position), Vector3.one * 2f);
		}
	}
#endif
}
