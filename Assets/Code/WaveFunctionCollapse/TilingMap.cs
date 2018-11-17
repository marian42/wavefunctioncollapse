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
		return this.slots[position.X % this.Size.X + (position.X % this.Size.X < 0 ? this.Size.X : 0), position.Y, position.Z % this.Size.Z + (position.Z % this.Size.Z < 0 ? this.Size.Z : 0)];
	}

	public override IEnumerable<Slot> GetAllSlots() {
		for (int x = 0; x < this.Size.X; x++) {
			for (int y = 0; y < this.Size.Y; y++) {
				for (int z = 0; z < this.Size.Z; z++) {
					yield return this.slots[x, y, z];
				}
			}
		}
	}

	public override void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints) {
		foreach (var constraint in constraints) {
			int y = constraint.RelativeY;
			if (y < 0) {
				y += this.Size.Y;
			}
			switch (constraint.Direction) {
				case BoundaryConstraint.ConstraintDirection.Up:
					for (int x = 0; x < this.Size.X; x++) {
						for (int z = 0; z < this.Size.Z; z++) {
							if (constraint.Mode == BoundaryConstraint.ConstraintMode.EnforceConnector) {
								this.GetSlot(new Vector3i(x, this.Size.Y - 1, z)).EnforceConnector(4, constraint.Connector);
							} else {
								this.GetSlot(new Vector3i(x, this.Size.Y - 1, z)).ExcludeConnector(4, constraint.Connector);
							}
						}
					}
					break;
				case BoundaryConstraint.ConstraintDirection.Down:
					for (int x = 0; x < this.Size.X; x++) {
						for (int z = 0; z < this.Size.Z; z++) {
							if (constraint.Mode == BoundaryConstraint.ConstraintMode.EnforceConnector) {
								this.GetSlot(new Vector3i(x, 0, z)).EnforceConnector(1, constraint.Connector);
							} else {
								this.GetSlot(new Vector3i(x, 0, z)).ExcludeConnector(1, constraint.Connector);
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
