using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DefaultColumn : IMap {
	private readonly Slot[] slots;

	public Slot GetSlot(int y) {
		if (y < 0 || y >= this.slots.Length) {
			return null;
		}
		return this.slots[y];
	}

	public Slot GetSlot(Vector3i position) {
		return this.GetSlot(position.Y);
	}

	public DefaultColumn(MapGenerator mapGenerator) {
		this.slots = new Slot[mapGenerator.Height];
		for (int y = 0; y < mapGenerator.Height; y++) {
			var slot = new Slot(new Vector3i(0, y, 0), mapGenerator, this);
			this.slots[y] = slot;
			slot.ModuleHealth = mapGenerator.InitialModuleHealth.Select(a => a.ToArray()).ToArray();
		}

		/*
		foreach (var constraint in mapGenerator.BoundaryConstraints) {
			int y = constraint.RelativeY;
			if (y < 0) {
				y += mapGenerator.Height;
			}
			switch (constraint.Mode) {
				case BoundaryConstraint.ConstraintMode.EnforceConnector:
					this.slots[y].EnforceConnector((int)constraint.Direction, constraint.Connector);
					break;
				case BoundaryConstraint.ConstraintMode.ExcludeConnector:
					this.slots[y].ExcludeConnector((int)constraint.Direction, constraint.Connector);
					break;
			}
		}
		 */
	}
}
