using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DefaultColumn : IMap {
	private readonly Slot[] slots;

	private readonly int yOffset;

	public Slot GetSlot(int y) {
		int index = y + this.yOffset;
		if (index < 0 || index >= this.slots.Length) {
			return null;
		}
		return this.slots[index];
	}

	public Slot GetSlot(Vector3i position) {
		return this.GetSlot(position.Y);
	}

	private int[][] createInitialNeighborCandidateHealth(Module[] modules) {
		var initialNeighborCandidateHealth = new int[6][];
		for (int i = 0; i < 6; i++) {
			initialNeighborCandidateHealth[i] = new int[modules.Length];
			foreach (var module in modules) {
				foreach (int possibleNeighbour in module.PossibleNeighbours[i]) {
					initialNeighborCandidateHealth[i][possibleNeighbour]++;
				}
			}
		}

		for (int d = 0; d < 6; d++) {
			for (int i = 0; i < modules.Length; i++) {
				if (initialNeighborCandidateHealth[d][i] == 0) {
					throw new Exception("Module " + modules[i].Prototype.name + " cannot be reached from direction " + d + " (" + modules[i].Prototype.Faces[d].ToString() + ")!");
				}
			}
		}
		return initialNeighborCandidateHealth;
	}

	public DefaultColumn(MapGenerator mapGenerator) {
		this.yOffset = mapGenerator.HeightLimit;

		var initialNeighborCandidateHealth = this.createInitialNeighborCandidateHealth(mapGenerator.Modules);

		this.slots = new Slot[mapGenerator.HeightLimit * 2 + 1];
		for (int y = -mapGenerator.HeightLimit; y <= mapGenerator.HeightLimit; y++) {
			var slot = new Slot(new Vector3i(0, y, 0), mapGenerator, this);
			this.slots[y + this.yOffset] = slot;
			slot.NeighborCandidateHealth = initialNeighborCandidateHealth.Select(a => a.ToArray()).ToArray();
		}
		
		this.slots[this.slots.Length - 1].EnforeConnector(Orientations.UP, mapGenerator.UpConnector);
		this.slots[0].EnforeConnector(Orientations.DOWN, mapGenerator.DownConnector);
		this.slots[0].ExcludeConnector(Orientations.FORWARD, 11);
		this.slots[0].ExcludeConnector(Orientations.LEFT, 11);
		this.slots[0].ExcludeConnector(Orientations.RIGHT, 11);
		this.slots[0].ExcludeConnector(Orientations.BACK, 11);
	}
}
