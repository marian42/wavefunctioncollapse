using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;

public class InfiniteMap : AbstractMap {
	private Dictionary<Vector3Int, Slot> slots;

	public readonly int Height;

	public Vector3Int rangeLimitCenter;
	public int rangeLimit = 80;

	private TilingMap defaultColumn;

	public InfiniteMap(int height) : base() {
		this.Height = height;
		this.slots = new Dictionary<Vector3Int, Slot>();

		if (ModuleData.Current == null || ModuleData.Current.Length == 0) {
			throw new InvalidOperationException("Module data was not available, please create module data first.");
		}
	}

	public override Slot GetSlot(Vector3Int position, bool create) {
		if (position.y >= this.Height || position.y < 0) {
			return null;
		}

		if (this.slots.ContainsKey(position)) {
			return this.slots[position];
		}
		if (!create) {
			return null;
		}

		if ((position - this.rangeLimitCenter).magnitude > this.rangeLimit) {
#if UNITY_EDITOR
			Debug.LogWarning("Touched Range Limit!");
#endif
			return null;
		}

		if (this.defaultColumn != null) {
			this.slots[position] = new Slot(position, this, this.defaultColumn.GetSlot(position));
		} else {
			this.slots[position] = new Slot(position, this);
		}
		return this.slots[position];
	}

	public override void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints) {
		this.defaultColumn = new TilingMap(new Vector3Int(1, this.Height, 1));

		foreach (var constraint in constraints) {
			int y = constraint.RelativeY;
			if (y < 0) {
				y += this.Height;
			}
			int[] directions = null;
			switch (constraint.Direction) {
				case BoundaryConstraint.ConstraintDirection.Up:
					directions = new int[] { 4 }; break;
				case BoundaryConstraint.ConstraintDirection.Down:
					directions = new int[] { 1 }; break;
				case BoundaryConstraint.ConstraintDirection.Horizontal:
					directions = Orientations.HorizontalDirections; break;
			}

			foreach (int d in directions) {
				switch (constraint.Mode) {
					case BoundaryConstraint.ConstraintMode.EnforceConnector:
						this.defaultColumn.GetSlot(new Vector3Int(0, y, 0)).EnforceConnector(d, constraint.Connector);
						break;
					case BoundaryConstraint.ConstraintMode.ExcludeConnector:
						this.defaultColumn.GetSlot(new Vector3Int(0, y, 0)).ExcludeConnector(d, constraint.Connector);
						break;
				}
			}			
		}

		foreach (var slot in this.defaultColumn.GetAllSlots()) {
			float _ = slot.Modules.Entropy; // Inititalize cached value
		}
	}

	public override IEnumerable<Slot> GetAllSlots() {
		return this.slots.Values;
	}
}
