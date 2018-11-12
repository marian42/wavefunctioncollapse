using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HistoryItem {
	// Use indices instead of references (int instead of Module) since references seem to confuse the garbage collector
	public Dictionary<Vector3i, ModuleSet> RemovedModules;

	public HistoryItem() {
		this.RemovedModules = new Dictionary<Vector3i, ModuleSet>();
	}

	public void RemoveModule(Slot slot, Module module) {
		if (!this.RemovedModules.ContainsKey(slot.Position)) {
			this.RemovedModules[slot.Position] = new ModuleSet();
		}
		this.RemovedModules[slot.Position].Add(module);
	}
}
