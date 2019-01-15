using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Slot {
	public Vector3Int Position;

	// List of modules that can still be placed here
	public ModuleSet Modules;

	// Direction -> Module -> Number of items in this.getneighbor(direction).Modules that allow this module as a neighbor
	public short[][] ModuleHealth;

	private AbstractMap map;

	public Module Module;

	public GameObject GameObject;

	public bool Collapsed {
		get {
			return this.Module != null;
		}
	}

	public Slot(Vector3Int position, AbstractMap map) {
		this.Position = position;
		this.map = map;
		this.ModuleHealth = map.CopyInititalModuleHealth();
		this.Modules = new ModuleSet(initializeFull: true);
	}

	public Slot(Vector3Int position, AbstractMap map, Slot prototype) {
		this.Position = position;
		this.map = map;
		this.ModuleHealth = prototype.ModuleHealth.Select(a => a.ToArray()).ToArray();
		this.Modules = new ModuleSet(prototype.Modules);
	}

	// TODO only look up once and then cache???
	public Slot GetNeighbor(int direction) {
		return this.map.GetSlot(this.Position + Orientations.Direction[direction]);
	}

	public void Collapse(Module module) {
		if (this.Collapsed) {
			Debug.LogWarning("Trying to collapse already collapsed slot.");
			return;
		}

		this.map.History.Push(new HistoryItem(this));

		this.Module = module;
		var toRemove = new ModuleSet(this.Modules);
		toRemove.Remove(module);
		this.RemoveModules(toRemove);

		this.map.NotifySlotCollapsed(this);
	}
	
	private void checkConsistency(Module module) {
		for (int d = 0; d < 6; d++) {
			if (this.GetNeighbor(d) != null && this.GetNeighbor(d).Collapsed && !this.GetNeighbor(d).Module.PossibleNeighbors[(d + 3) % 6].Contains(module)) {
				throw new Exception("Illegal collapse, not in neighbour list. (Incompatible connectors)");
			}
		}

		if (!this.Modules.Contains(module)) {
			throw new Exception("Illegal collapse!");
		}
	}

	public void CollapseRandom() {
		if (!this.Modules.Any()) {
			throw new CollapseFailedException(this);
		}
		if (this.Collapsed) {
			throw new Exception("Slot is already collapsed.");
		}
		
		float max = this.Modules.Select(module => module.Prototype.Probability).Sum();
		float roll = (float)(InfiniteMap.Random.NextDouble() * max);
		float p = 0;
		foreach (var candidate in this.Modules) {
			p += candidate.Prototype.Probability;
			if (p >= roll) {
				this.Collapse(candidate);
				return;
			}
		}
		this.Collapse(this.Modules.First());
	}

	private static int iterationCount = 0;


	// This modifies the supplied ModuleSet as a side effect
	public void RemoveModules(ModuleSet modulesToRemove, bool recursive = true) {
#if UNITY_EDITOR
		Slot.iterationCount++;
#endif

		modulesToRemove.Intersect(this.Modules);

		if (this.map.History != null && this.map.History.Any()) {
			var item = this.map.History.Peek();
			if (!item.RemovedModules.ContainsKey(this.Position)) {
				item.RemovedModules[this.Position] = new ModuleSet();
			}
			item.RemovedModules[this.Position].Add(modulesToRemove);
		}

		for (int d = 0; d < 6; d++) {
			int inverseDirection = (d + 3) % 6;
			var neighbor = this.GetNeighbor(d);
			if (neighbor == null || neighbor.Forgotten) {
				continue;
			}

			foreach (var module in modulesToRemove) {
				for (int i = 0; i < module.PossibleNeighborsArray[d].Length; i++) {
					var possibleNeighbor = module.PossibleNeighborsArray[d][i];
					if (neighbor.ModuleHealth[inverseDirection][possibleNeighbor.Index] == 1 && neighbor.Modules.Contains(possibleNeighbor)) {
						this.map.RemovalQueue[neighbor.Position].Add(possibleNeighbor);
					}
#if UNITY_EDITOR
					if (neighbor.ModuleHealth[inverseDirection][possibleNeighbor.Index] < 1) {
						throw new System.InvalidOperationException("ModuleHealth must not be negative. " + this.Position + " d: " + d);
					}
#endif
					neighbor.ModuleHealth[inverseDirection][possibleNeighbor.Index]--;
				}
			}
		}

		this.Modules.Remove(modulesToRemove);

		if (this.Modules.Empty) {
			throw new CollapseFailedException(this);
		}

		if (recursive) {
			this.map.FinishRemovalQueue();
		}
	}

	public static void ResetIterationCount() {
		iterationCount = 0;
	}

	public static int GetIterationCount() {
		return iterationCount;
	}

	/// <summary>
	/// Add modules non-recursively.
	/// Returns true if this lead to this slot changing from collapsed to not collapsed.
	/// </summary>
	public void AddModules(ModuleSet modulesToAdd) {
		foreach (var module in modulesToAdd) {
			if (this.Modules.Contains(module) || module == this.Module) {
				continue;
			}
			for (int d = 0; d < 6; d++) {
				int inverseDirection = (d + 3) % 6;
				var neighbor = this.GetNeighbor(d);
				if (neighbor == null || neighbor.Forgotten) {
					continue;
				}

				foreach (var possibleNeighbor in module.PossibleNeighbors[d]) {
					neighbor.ModuleHealth[inverseDirection][possibleNeighbor.Index]++;
				}
			}
			this.Modules.Add(module);
		}

		if (this.Collapsed && !this.Modules.Empty) {
			this.Module = null;
			this.map.NotifySlotCollapseUndone(this);
		}
	}

	public void EnforceConnector(int direction, int connector) {
		var toRemove = this.Modules.Where(module => !module.Fits(direction, connector));
		this.RemoveModules(ModuleSet.FromEnumerable(toRemove));
	}

	public void ExcludeConnector(int direction, int connector) {
		var toRemove = this.Modules.Where(module => module.Fits(direction, connector));
		this.RemoveModules(ModuleSet.FromEnumerable(toRemove));
	}

	public override int GetHashCode() {
		return this.Position.GetHashCode();
	}

	public void Forget() {
		this.ModuleHealth = null;
		this.Modules = null;
	}

	public bool Forgotten {
		get {
			return this.Modules == null;
		}
	}
}
