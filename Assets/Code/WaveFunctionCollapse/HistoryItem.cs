using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HistoryItem {
	public Dictionary<Vector3i, ModuleSet> RemovedModules;

	public readonly Slot Slot;

	public HistoryItem(Slot slot) {
		this.RemovedModules = new Dictionary<Vector3i, ModuleSet>();
		this.Slot = slot;
	}
}
