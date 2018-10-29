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

	private int[][] createInitialNeighborCandidateHealth(Module[] modules) {
		var initialNeighborCandidateHealth = new int[6][];
		for (int i = 0; i < 6; i++) {
			initialNeighborCandidateHealth[i] = new int[modules.Length];
			foreach (var module in modules) {
				foreach (var possibleNeighbor in module.PossibleNeighbors[i]) {
					initialNeighborCandidateHealth[i][possibleNeighbor.Index]++;
				}
			}
		}

		for (int d = 0; d < 6; d++) {
			for (int i = 0; i < modules.Length; i++) {
				if (initialNeighborCandidateHealth[d][i] == 0) {
					Debug.LogError("Module " + modules[i].Prototype.name + " cannot be reached from direction " + d + " (" + modules[i].Prototype.Faces[d].ToString() + ")!", modules[i].Prototype.gameObject);
					throw new Exception("Unreachable module.");
				}
			}
		}
		return initialNeighborCandidateHealth;
	}

	public DefaultColumn(MapGenerator mapGenerator) {
		var initialNeighborCandidateHealth = this.createInitialNeighborCandidateHealth(mapGenerator.Modules);

		this.slots = new Slot[mapGenerator.Height];
		for (int y = 0; y < mapGenerator.Height; y++) {
			var slot = new Slot(new Vector3i(0, y, 0), mapGenerator, this);
			this.slots[y] = slot;
			slot.NeighborCandidateHealth = initialNeighborCandidateHealth.Select(a => a.ToArray()).ToArray();
		}

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
	}
}
