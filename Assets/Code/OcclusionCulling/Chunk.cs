using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {
	public readonly Bounds Bounds;

	public List<ExteriorBlock> ExteriorBlocks;
	public List<Portal> Portals;
	public List<Room> Rooms;

	private Dictionary<Vector3i, ExteriorBlock> exteriorblocksByPosition;

	public bool ExteriorBlocksVisible = true;

	public Chunk(Bounds bounds) {
		this.Bounds = bounds;
		this.ExteriorBlocks = new List<ExteriorBlock>();
		this.Portals = new List<Portal>();
		this.Rooms = new List<Room>();
		this.exteriorblocksByPosition = new Dictionary<Vector3i,ExteriorBlock>();
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
		foreach (var block in this.ExteriorBlocks) {
			block.SetVisibility(value);
		}
		this.ExteriorBlocksVisible = value;
	}

	public void SetRoomVisibility(bool value) {
		foreach (var room in this.Rooms) {
			room.SetVisibility(value);
		}
	}

	public void AddBlock(ExteriorBlock block, Vector3i position) {
		if (this.exteriorblocksByPosition.ContainsKey(position)) {
			this.ExteriorBlocks.Remove(this.exteriorblocksByPosition[position]);
		}
		this.exteriorblocksByPosition[position] = block;
		this.ExteriorBlocks.Add(block);
		this.ExteriorBlocksVisible = true;
	}

	public void RemoveBlock(Vector3i position) {
		if (!this.exteriorblocksByPosition.ContainsKey(position)) {
			return;
		}
		this.ExteriorBlocks.Remove(this.exteriorblocksByPosition[position]);
	}
}
