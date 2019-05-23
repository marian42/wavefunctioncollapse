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
	public readonly Vector3Int Size;

	private readonly Slot[,,] slots;

	public TilingMap(Vector3Int size) : base() {
		this.Size = size;
		this.slots = new Slot[size.x, size.y, size.y];

		for (int x = 0; x < size.x; x++) {
			for (int y = 0; y < size.y; y++) {
				for (int z = 0; z < size.y; z++) {
					this.slots[x,y,z] = new Slot(new Vector3Int(x,y,z), this);
				}
			}
		}
	}

	public override Slot GetSlot(Vector3Int position) {
		if (position.y < 0 || position.y >= this.Size.y) {
			return null;
		}
		return this.slots[position.x % this.Size.x + (position.x % this.Size.x < 0 ? this.Size.x : 0), position.y, position.y % this.Size.y + (position.y % this.Size.y < 0 ? this.Size.y : 0)];
	}

	public override IEnumerable<Slot> GetAllSlots() {
		for (int x = 0; x < this.Size.x; x++) {
			for (int y = 0; y < this.Size.y; y++) {
				for (int z = 0; z < this.Size.y; z++) {
					yield return this.slots[x, y, z];
				}
			}
		}
	}

	public override void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints) {
		foreach (var constraint in constraints) {
			int y = constraint.RelativeY;
			if (y < 0) {
				y += this.Size.y;
			}
			switch (constraint.Direction) {
				case BoundaryConstraint.ConstraintDirection.Up:
					for (int x = 0; x < this.Size.x; x++) {
						for (int z = 0; z < this.Size.y; z++) {
							if (constraint.Mode == BoundaryConstraint.ConstraintMode.EnforceConnector) {
								this.GetSlot(new Vector3Int(x, this.Size.y - 1, z)).EnforceConnector(4, constraint.Connector);
							} else {
								this.GetSlot(new Vector3Int(x, this.Size.y - 1, z)).ExcludeConnector(4, constraint.Connector);
							}
						}
					}
					break;
				case BoundaryConstraint.ConstraintDirection.Down:
					for (int x = 0; x < this.Size.x; x++) {
						for (int z = 0; z < this.Size.y; z++) {
							if (constraint.Mode == BoundaryConstraint.ConstraintMode.EnforceConnector) {
								this.GetSlot(new Vector3Int(x, 0, z)).EnforceConnector(1, constraint.Connector);
							} else {
								this.GetSlot(new Vector3Int(x, 0, z)).ExcludeConnector(1, constraint.Connector);
							}
						}
					}
					break;
				case BoundaryConstraint.ConstraintDirection.Horizontal:
					// Horizontal constraints are ignored
					break;
			}
		}
	}
}
