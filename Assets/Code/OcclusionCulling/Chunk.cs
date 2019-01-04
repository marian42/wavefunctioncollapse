using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {
	public readonly Bounds Bounds;

	public List<Renderer> Renderers;
	public List<Portal> Portals;
	public List<Room> Rooms;

	private Dictionary<Vector3i, Renderer[]> renderersByPosition;

	public bool ExteriorBlocksVisible = true;

	public Chunk(Bounds bounds) {
		this.Bounds = bounds;
		this.Renderers = new List<Renderer>();
		this.Portals = new List<Portal>();
		this.Rooms = new List<Room>();
		this.renderersByPosition = new Dictionary<Vector3i, Renderer[]>();
	}

	public void SetVisibility(bool value) {
		this.SetExteriorVisibility(value);
		if (!value) {
			this.SetRoomVisibility(false);
		}
	}

	public void SetExteriorVisibility(bool value) {
		if (this.ExteriorBlocksVisible == value) {
			return;
		}
		foreach (var renderer in this.Renderers) {
			renderer.enabled = value;
		}
		this.ExteriorBlocksVisible = value;
	}

	public void SetRoomVisibility(bool value) {
		foreach (var room in this.Rooms) {
			room.SetVisibility(value);
		}
	}

	public void AddBlock(Renderer[] renderers, Vector3i position) {
		if (this.renderersByPosition.ContainsKey(position)) {
			foreach (var renderer in this.renderersByPosition[position]) {
				this.Renderers.Remove(renderer);
			}
		}
		this.renderersByPosition[position] = renderers;
		this.Renderers.AddRange(renderers);
		this.ExteriorBlocksVisible = true;
	}

	public void RemoveBlock(Vector3i position) {
		if (!this.renderersByPosition.ContainsKey(position)) {
			return;
		}
		foreach (var renderer in this.renderersByPosition[position]) {
			this.Renderers.Remove(renderer);
		}
	}
}
