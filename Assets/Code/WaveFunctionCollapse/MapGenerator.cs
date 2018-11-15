using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;

public class MapGenerator : IMap {
	public const float BlockSize = 2f;

	public static System.Random Random;

	public Dictionary<Vector3i, Slot> Slots;

	public readonly int Height;

	public Vector3i rangeLimitCenter;
	public int rangeLimit = 80;

	private DefaultColumn defaultColumn;

	private HashSet<Slot> workArea;

	public readonly Queue<Slot> BuildQueue;

	public RingBuffer<HistoryItem> History;

	private int backtrackBarrier;
	private int backtrackAmount = 0;

	public QueueDictionary<Vector3i, ModuleSet> RemovalQueue;

	public int[][] InitialModuleHealth;

	public MapGenerator(int height) {
		this.Height = height;
		MapGenerator.Random = new System.Random();
		this.Slots = new Dictionary<Vector3i, Slot>();
		this.BuildQueue = new Queue<Slot>();
		this.History = new RingBuffer<HistoryItem>(3000);
		this.backtrackBarrier = 0;
		this.RemovalQueue = new QueueDictionary<Vector3i, ModuleSet>(() => new ModuleSet());

		if (Module.All == null || Module.All.Length == 0) {
			throw new InvalidOperationException("Module data was not available, please create module data first.");
		}
		this.InitialModuleHealth = this.createInitialModuleHealth(Module.All);
		this.defaultColumn = new DefaultColumn(this);
	}

	public Slot GetSlot(Vector3i position, bool create) {
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
			this.Slots[position] = new Slot(position, this, this);
			this.Slots[position].ModuleHealth = this.InitialModuleHealth.Select(a => a.ToArray()).ToArray();
		}
		return this.Slots[position];
	}

	public Slot GetSlot(Vector3i position) {
		return this.GetSlot(position, true);
	}

	public void Collapse(Vector3i start, Vector3i size, bool showProgress = false) {
		var targets = new List<Vector3i>();
		for (int x = 0; x < size.X; x++) {
			for (int y = 0; y < size.Y; y++) {
				for (int z = 0; z < size.Z; z++) {
					targets.Add(start + new Vector3i(x, y, z));
				}
			}
		}
		this.Collapse(targets, showProgress);
	}

	public void ClearRemovalQueue() {
		while (this.RemovalQueue.Any()) {
			var kvp = this.RemovalQueue.Dequeue();
			var slot = this.GetSlot(kvp.Key);
			if (!slot.Collapsed) {
				slot.RemoveModules(kvp.Value, false);
			}
		}
	}

	public void Collapse(IEnumerable<Vector3i> targets, bool showProgress = false) {
		this.RemovalQueue.Clear();
		this.workArea = new HashSet<Slot>(targets.Select(target => this.GetSlot(target)).Where(slot => slot != null && !slot.Collapsed));
		
		while (this.workArea.Any()) {
			int minEntropy = this.workArea.Min(slot => slot.Entropy);
			var candidates = this.workArea.Where(slot => !slot.Collapsed && slot.Entropy == minEntropy).ToArray();
			
			var selected = candidates[MapGenerator.Random.Next(0, candidates.Length - 1)];
			try {
				selected.CollapseRandom();
			}
			catch (CollapseFailedException) {
				this.RemovalQueue.Clear();
				if (this.History.TotalCount > this.backtrackBarrier) {
					this.backtrackBarrier = this.History.TotalCount;
					this.backtrackAmount = 2;
				} else {
					this.backtrackAmount *= 2;
				}
				if (this.backtrackAmount > 10) {
					Debug.Log("Backtracking " + this.backtrackAmount + " steps...");
				}
				this.Undo(this.backtrackAmount);
			}

#if UNITY_EDITOR
			if (showProgress) {
				EditorUtility.DisplayProgressBar("Collapsing area... ", this.workArea.Count + " left...", 1f - (float)this.workArea.Count() / targets.Count());
			}
#endif
		}

#if UNITY_EDITOR
		if (showProgress) {
			EditorUtility.ClearProgressBar();
		}
#endif
	}

	public void Undo(int steps) {
		while (steps > 0 && this.History.Any()) {
			var item = this.History.Pop();

			foreach (var slotAddress in item.RemovedModules.Keys) {
				this.GetSlot(slotAddress).AddModules(item.RemovedModules[slotAddress]);
			}
			steps--;
		}
	}

	public void EnforceWalkway(Vector3i start, int direction) {
		var slot = this.GetSlot(start);
		var toRemove = slot.Modules.Where(module => !module.GetFace(direction).Walkable);
		slot.RemoveModules(ModuleSet.FromEnumerable(toRemove));
	}

	public void EnforceWalkway(Vector3i start, Vector3i destination) {
		int direction = Orientations.GetIndex((destination - start).ToVector3());
		this.EnforceWalkway(start, direction);
		this.EnforceWalkway(destination, (direction + 3) % 6);
	}

	public void MarkSlotComplete(Slot slot) {
		if (this.workArea != null) {
			this.workArea.Remove(slot);
		}
		this.BuildQueue.Enqueue(slot);
	}

	public void MarkSlotIncomplete(Slot slot) {
		if (this.workArea != null) {
			this.workArea.Add(slot);
		}
	}

	private int[][] createInitialModuleHealth(Module[] modules) {
		var initialModuleHealth = new int[6][];
		for (int i = 0; i < 6; i++) {
			initialModuleHealth[i] = new int[modules.Length];
			foreach (var module in modules) {
				foreach (var possibleNeighbor in module.PossibleNeighbors[(i + 3) % 6]) {
					initialModuleHealth[i][possibleNeighbor.Index]++;
				}
			}
		}

		for (int i = 0; i < modules.Length; i++) {
			for (int d = 0; d < 6; d++) {
				if (initialModuleHealth[d][i] == 0) {
					Debug.LogError("Module " + modules[i].Name + " cannot be reached from direction " + d + " (" + modules[i].GetFace(d).ToString() + ")!", modules[i].Prototype.gameObject);
					throw new Exception("Unreachable module.");
				}
			}
		}
		return initialModuleHealth;
	}
}
