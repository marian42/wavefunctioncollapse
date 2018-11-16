using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;

public class InfiniteMap : AbstractMap {
	public Dictionary<Vector3i, Slot> Slots;

	public readonly int Height;

	public Vector3i rangeLimitCenter;
	public int rangeLimit = 80;

	private TilingMap defaultColumn;

	public InfiniteMap(int height) : base() {
		this.Height = height;
		this.Slots = new Dictionary<Vector3i, Slot>();

		if (Module.All == null || Module.All.Length == 0) {
			throw new InvalidOperationException("Module data was not available, please create module data first.");
		}
	}

	public override Slot GetSlot(Vector3i position, bool create) {
		if (position.Y >= this.Height || position.Y < 0) {
			return null;
		}

		if (this.Slots.ContainsKey(position)) {
			return this.Slots[position];
		}
		if (!create) {
			return null;
		}

		if ((position - this.rangeLimitCenter).Magnitude > this.rangeLimit) {
#if UNITY_EDITOR
			Debug.LogWarning("Touched Range Limit!");
#endif
			return null;
		}

		if (this.defaultColumn != null) {
			this.Slots[position] = new Slot(position, this, this.defaultColumn.GetSlot(position));
		} else {
			this.Slots[position] = new Slot(position, this, true);
		}
		return this.Slots[position];
	}

	public void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints) {
		this.defaultColumn = new TilingMap(new Vector3i(1, this.Height, 1));

		foreach (var constraint in constraints) {
			int y = constraint.RelativeY;
			if (y < 0) {
				y += this.Height;
			}
			switch (constraint.Mode) {
				case BoundaryConstraint.ConstraintMode.EnforceConnector:
					this.defaultColumn.GetSlot(new Vector3i(0, y, 0)).EnforceConnector((int)constraint.Direction, constraint.Connector);
					break;
				case BoundaryConstraint.ConstraintMode.ExcludeConnector:
					this.defaultColumn.GetSlot(new Vector3i(0, y, 0)).ExcludeConnector((int)constraint.Direction, constraint.Connector);
					break;
			}
		}
	}
}
