using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// A finite sized map that uses horizontal world wrapping.
/// That means you can horizontally tile copies of this map and the edges will match
/// </summary>
public class TilingMap : AbstractMap {
	public readonly Vector3i Size;

	private readonly Slot[,,] slots;

	public TilingMap(Vector3i size) : base() {
		this.Size = size;
		this.slots = new Slot[size.X, size.Y, size.Z];

		for (int x = 0; x < size.X; x++) {
			for (int y = 0; y < size.Y; y++) {
				for (int z = 0; z < size.Z; z++) {
					this.slots[x,y,z] = new Slot(new Vector3i(x,y,z), this, true);
				}
			}
		}
	}

	public override Slot GetSlot(Vector3i position, bool create) {
		if (position.Y < 0 || position.Y >= this.Size.Y) {
			return null;
		}
		return this.slots[position.X % this.Size.X, position.Y, position.Z % this.Size.Z];
	}
}
