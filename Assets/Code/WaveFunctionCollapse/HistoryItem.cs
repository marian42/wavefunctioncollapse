using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HistoryItem {
	public Dictionary<Vector3Int, ModuleSet> RemovedModules;

	public readonly Slot Slot;

	public HistoryItem(Slot slot) {
		this.RemovedModules = new Dictionary<Vector3Int, ModuleSet>();
		this.Slot = slot;
	}
}
