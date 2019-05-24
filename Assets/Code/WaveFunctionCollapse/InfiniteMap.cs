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
	public int RangeLimit = 80;

	private TilingMap defaultColumn;

	public InfiniteMap(int height) : base() {
		this.Height = height;
		this.slots = new Dictionary<Vector3Int, Slot>();
		this.defaultColumn = new TilingMap(new Vector3Int(1, height, 1));

		if (ModuleData.Current == null || ModuleData.Current.Length == 0) {
			throw new InvalidOperationException("Module data was not available, please create module data first.");
		}
	}

	public override Slot GetSlot(Vector3Int position) {
		if (position.y >= this.Height || position.y < 0) {
			return null;
		}

		if (this.slots.ContainsKey(position)) {
			return this.slots[position];
		}

		if (this.IsOutsideOfRangeLimit(position)) {
			return null;
		}

		this.slots[position] = new Slot(position, this, this.defaultColumn.GetSlot(position));
		return this.slots[position];
	}

	public bool IsOutsideOfRangeLimit(Vector3Int position) {
		return (position - this.rangeLimitCenter).magnitude > this.RangeLimit;
	}

	public override void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints) {
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

	public Slot GetDefaultSlot(int y) {
		return this.defaultColumn.GetSlot(Vector3Int.up * y);
	}
	
	public bool IsSlotInitialized(Vector3Int position) {
		return this.slots.ContainsKey(position);
	}

	private bool muteRangeLimitWarning = false;

	public void OnHitRangeLimit(Vector3Int position, ModuleSet modulesToRemove) {
		if (this.muteRangeLimitWarning || position.y < 0 || position.y >= this.Height) {
			return;
		}

		var prototypeNames = modulesToRemove.Select(module => module.Prototype.name).Distinct();
		Debug.LogWarning("Hit range limit at " + position + ". Module(s) to be removed:\n" + string.Join("\n", prototypeNames.ToArray()) + "\n");
		this.muteRangeLimitWarning = true;
	}
}
