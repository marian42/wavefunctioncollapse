using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;

public class MapGenerator : MonoBehaviour, IMap, ISerializationCallbackReceiver {
	public const float BlockSize = 2f;

	public static System.Random Random;

	[HideInInspector]
	public Module[] Modules;

	public Dictionary<Vector3i, Slot> Map;

	public int DefaultSize = 4;

	public int Height = 8;

	public bool RespectNeighorExclusions = true;

	public Vector3i RangeLimitCenter;

	public int RangeLimit = 20;

	private DefaultColumn defaultColumn;

	public BoundaryConstraint[] BoundaryConstraints;

	private HashSet<Slot> workArea;

	private Queue<Slot> buildQueue;

	public RingBuffer<HistoryItem> History;

	private int backtrackBarrier;
	private int backtrackAmount = 0;

	public bool Initialized {
		get {
			return this.Map != null;
		}
	}

	public int[][] InitialModuleHealth;

	public Slot GetSlot(Vector3i position, bool create) {
		if (position.Y >= this.Height || position.Y < 0) {
			return null;
		}

		if (this.Map.ContainsKey(position)) {
			return this.Map[position];
		}
		if (!create) {
			return null;
		}

		if ((position - this.RangeLimitCenter).Magnitude > this.RangeLimit) {
#if UNITY_EDITOR
			Debug.LogWarning("Touched Range Limit!");
#endif
			return null;
		}

		if (this.defaultColumn != null) {
			this.Map[position] = new Slot(position, this, this.defaultColumn.GetSlot(position));
		} else {
			this.Map[position] = new Slot(position, this, this);
			this.Map[position].ModuleHealth = this.InitialModuleHealth.Select(a => a.ToArray()).ToArray();
		}
		return this.Map[position];
	}

	public Slot GetSlot(Vector3i position) {
		return this.GetSlot(position, true);
	}

	public Slot GetSlot(int x, int y, int z, bool create = true) {
		return this.GetSlot(new Vector3i(x, y, z), create);
	}
	
	public void CreateModules() {
		this.Modules = ModulePrototype.CreateModules(this.RespectNeighorExclusions).ToArray();
	}

#if UNITY_EDITOR
	public void SimplifyNeighborData() {
		this.Initialize();
		int count = 0;
		var center = new Vector3i(0, this.Height / 2, 0);
		this.defaultColumn = null;
		int p = 0;
		foreach (var module in this.Modules) {
			this.InitialModuleHealth = this.createInitialModuleHealth(this.Modules);
			foreach (var s in this.Map.Values) {
				s.Module = null;
				for (int d = 0; d < 6; d++) {
					for (int i = 0; i < this.Modules.Length; i++) {
						s.ModuleHealth[d][i] = this.InitialModuleHealth[d][i];
					}
				}

				if (s.Modules.Count() != this.Modules.Count()) {
					foreach (var m in this.Modules) {
						s.Modules.Add(m);
					}
				}
			}
			this.buildQueue.Clear();
			var slot = this.GetSlot(center);
			try {

			}
			catch (CollapseFailedException exception) {
				this.BuildAllSlots();
				throw new InvalidOperationException("Module " + module.Name + " creates a failure at relative position " + exception.Slot.Position + ".");
			}
			slot.Collapse(module);
			for (int direction = 0; direction < 6; direction++) {
				var neighbor = slot.GetNeighbor(direction);
				int unoptimizedNeighborCount = module.PossibleNeighbors[direction].Length;
				module.PossibleNeighbors[direction] = module.PossibleNeighbors[direction].Where(m => neighbor.Modules.Contains(m)).ToArray();
				count += unoptimizedNeighborCount - module.PossibleNeighbors[direction].Length;
			}
			p++;
			EditorUtility.DisplayProgressBar("Simplifying... " + count, module.Name, (float)p / this.Modules.Length);
		}
		Debug.Log("Removed " + count + " impossible neighbors.");
		EditorUtility.ClearProgressBar();
	}
#endif

	public void Initialize() {
		this.Clear();
		MapGenerator.Random = new System.Random();
		this.Map = new Dictionary<Vector3i, Slot>();
		this.buildQueue = new Queue<Slot>();
		this.History = new RingBuffer<HistoryItem>(3000);
		this.backtrackBarrier = 0;

		if (this.Modules == null || this.Modules.Length == 0) {
			Debug.LogWarning("Module data was not available, creating new data.");
			this.CreateModules();
		}
		this.InitialModuleHealth = this.createInitialModuleHealth(this.Modules);
		this.defaultColumn = new DefaultColumn(this);
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

	public void CollapseDefaultArea(bool showProgress = false) {
		this.Collapse(new Vector3i(- this.DefaultSize / 2, 0, - this.DefaultSize / 2), new Vector3i(this.DefaultSize, this.Height, this.DefaultSize), showProgress);
	}

	public void Collapse(IEnumerable<Vector3i> targets, bool showProgress = false) {
		this.workArea = new HashSet<Slot>(targets.Select(target => this.GetSlot(target)).Where(slot => slot != null && !slot.Collapsed));
		
		while (this.workArea.Any()) {
			int minEntropy = this.workArea.Min(slot => slot.Entropy);
			var candidates = this.workArea.Where(slot => !slot.Collapsed && slot.Entropy == minEntropy).ToList();
			
			var selected = candidates[MapGenerator.Random.Next(0, candidates.Count - 1)];
			try {
				selected.CollapseRandom();
			}
			catch (CollapseFailedException) {
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
				this.GetSlot(slotAddress).AddModules(item.RemovedModules[slotAddress].Select(i => this.Modules[i]).ToList());
			}
			steps--;
		}
	}

	public Vector3 GetWorldspacePosition(Vector3i position) {
		return this.transform.position
			+ Vector3.up * MapGenerator.BlockSize / 2f
			+ new Vector3(
				(float)(position.X) * MapGenerator.BlockSize,
				(float)(position.Y) * MapGenerator.BlockSize,
				(float)(position.Z) * MapGenerator.BlockSize);
	}

	public void Clear() {
		var children = new List<Transform>();
		foreach (Transform child in this.transform) {
			children.Add(child);
		}
		foreach (var child in children) {
			GameObject.DestroyImmediate(child.gameObject);
		}
		this.Map = null;
	}

	public void EnforceWalkway(Vector3i start, int direction) {
		var slot = this.GetSlot(start);
		var toRemove = slot.Modules.Where(module => !module.GetFace(direction).Walkable).ToList();
		slot.RemoveModules(toRemove);
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
		this.buildQueue.Enqueue(slot);
	}

	public void MarkSlotIncomplete(Slot slot) {
		if (this.workArea != null) {
			this.workArea.Add(slot);
		}
	}
	
	public void Update() {
		if (this.buildQueue == null) {
			return;
		}

		int itemsLeft = 50;

		while (this.buildQueue.Count != 0 && itemsLeft > 0) {
			var slot = this.buildQueue.Dequeue();
			if (slot == null) {
				return;
			}
			if (slot.Build()) {
				itemsLeft--;
			}
		}
	}

	public void BuildAllSlots() {
		while (this.buildQueue.Count != 0) {
			this.buildQueue.Dequeue().Build();
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

	public void OnBeforeSerialize() { }

	public void OnAfterDeserialize() {
		if (this.Modules != null && this.Modules.Length != 0) {
			foreach (var module in this.Modules) {
				module.DeserializeNeigbors(this.Modules);
			}
		}
	}
	
	public bool VisualizeSlots = false;

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmoForMyScript(MapGenerator mapGenerator, GizmoType gizmoType) {
		if (!mapGenerator.VisualizeSlots) {
			return;
		}
		if (mapGenerator.Map == null) {
			return;
		}
		foreach (var slot in mapGenerator.Map.Values) {
			if (slot.Collapsed || slot.Modules.Count() == mapGenerator.Modules.Count()) {
				continue;
			}
			Handles.Label(mapGenerator.GetWorldspacePosition(slot.Position), slot.Modules.Count().ToString());
		}
	}
#endif
}
