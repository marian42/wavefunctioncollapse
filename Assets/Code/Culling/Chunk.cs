using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {
	public readonly Bounds Bounds;

	public List<Renderer> Renderers;
	public List<GameObject> GameObjects;
	public List<Portal> Portals;
	public List<Room> Rooms;

	private Dictionary<Vector3Int, Renderer[]> renderersByPosition;

	public bool ExteriorBlocksVisible = true;
	public bool InRenderRange {
		get;
		private set;
	}

	public Chunk(Bounds bounds) {
		this.Bounds = bounds;
		this.Renderers = new List<Renderer>();
		this.Portals = new List<Portal>();
		this.Rooms = new List<Room>();
		this.renderersByPosition = new Dictionary<Vector3Int, Renderer[]>();
		this.GameObjects = new List<GameObject>();
		this.InRenderRange = true;
	}

	public void SetInRenderRange(bool value) {
		this.InRenderRange = value;
		foreach (var gameObject in this.GameObjects) {
			gameObject.SetActive(value);
		}
		// This only works for small rooms.
		// It will fail if a room has blocks outside the render range and close to the player. 
		foreach (var room in this.Rooms) {
			foreach (var slot in room.Slots) {
				slot.GameObject.SetActive(value);
			}
		}
	}

	public void SetExteriorVisibility(bool value) {
		if (this.ExteriorBlocksVisible == value) {
			return;
		}
		foreach (var renderer in this.Renderers) {
			renderer.shadowCastingMode = value ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
		}
		this.ExteriorBlocksVisible = value;
	}

	public void SetRoomVisibility(bool value) {
		foreach (var room in this.Rooms) {
			room.SetVisibility(value);
		}
	}

	public void AddBlock(Slot slot) {
		if (this.renderersByPosition.ContainsKey(slot.Position)) {
			foreach (var renderer in this.renderersByPosition[slot.Position]) {
				this.Renderers.Remove(renderer);
			}
		}
		var renderers = slot.GameObject.GetComponentsInChildren<Renderer>();
		this.renderersByPosition[slot.Position] = renderers;
		this.Renderers.AddRange(renderers);
		this.ExteriorBlocksVisible = true;
		this.GameObjects.Add(slot.GameObject);
		slot.GameObject.SetActive(this.InRenderRange);
	}

	public void RemoveBlock(Slot slot) {
		if (!this.renderersByPosition.ContainsKey(slot.Position)) {
			return;
		}
		foreach (var renderer in this.renderersByPosition[slot.Position]) {
			this.Renderers.Remove(renderer);
		}
		this.GameObjects.Remove(slot.GameObject);
	}
}
