using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;

public class MapGenerator : MonoBehaviour {
	public const float BlockSize = 2f;

	[HideInInspector]
	public Module[] Modules;

	public Dictionary<Vector3i, Slot> Map;

	public Vector3i Size;

	public int HeightLimit = 4;

	public Slot LatestFilled;

	public int UpConnector;
	public int DownConnector;

	public bool AllowExclusions = true;

	public bool BuildOnCollapse;

	private int[][] initialNeighborCandidateHealth;

	public int RangeLimit = 20;

	public int TestModule = 0;

	private Slot[] defaultColumn;

	private bool defaultColumnMode = false;

	public Slot GetSlot(Vector3i position, bool create = true) {
		if (position.Y > this.HeightLimit || position.Y < -this.HeightLimit) {
			return null;
		}

		if (this.defaultColumnMode) {
			return this.defaultColumn[this.HeightLimit + position.Y];
		}

		if (position.Magnitude > this.RangeLimit) {
			return null;
		}

		if (this.Map.ContainsKey(position)) {
			return this.Map[position];
		}
		if (!create) {
			return null;
		}

		var result = new Slot(position, this);
		this.initializeSlot(result);
		this.Map[position] = result;
		return result;
	}

	public Slot GetSlot(int x, int y, int z, bool create = true) {
		return this.GetSlot(new Vector3i(x, y, z), create);
	}

	private void initializeSlot(Slot slot) {
		var prototype = this.defaultColumn[slot.Position.Y + this.HeightLimit];

		slot.NeighborCandidateHealth = prototype.NeighborCandidateHealth.Select(a => a.ToArray()).ToArray();
		slot.Modules = new HashSet<int>(prototype.Modules);
	}
	
	private void createModules() {
		this.Modules = ModulePrototype.CreateModules(this).ToArray();
	}

	private void prepareModuleData() {
		this.createModules();
		this.createInitialNeighborCandidateHealth();
	}

	private void createInitialNeighborCandidateHealth() {
		this.initialNeighborCandidateHealth = new int[6][];
		for (int i = 0; i < 6; i++) {
			initialNeighborCandidateHealth[i] = new int[this.Modules.Length];
			foreach (var module in this.Modules) {
				foreach (int possibleNeighbour in module.PossibleNeighbours[i]) {
					initialNeighborCandidateHealth[i][possibleNeighbour]++;
				}
			}
		}

		for (int d = 0; d < 6; d++) {
			for (int i = 0; i < this.Modules.Count(); i++) {
				if (initialNeighborCandidateHealth[d][i] == 0) {
					throw new Exception("Module " + this.Modules[i].Prototype.name + " cannot be reached from direction " + d + " (" + this.Modules[i].Prototype.Faces[d].ToString() + ")!");
				}
			}
		}
	}

	private void prepareDefaultColumn() {
		this.defaultColumnMode = true;
		this.defaultColumn = new Slot[this.HeightLimit * 2 + 1];
		for (int y = -this.HeightLimit; y <= this.HeightLimit; y++) {
			var slot = new Slot(new Vector3i(0, y, 0), this);
			this.defaultColumn[y + this.HeightLimit] = slot;
			slot.NeighborCandidateHealth = initialNeighborCandidateHealth.Select(a => a.ToArray()).ToArray();
		}
		
		this.defaultColumn[this.HeightLimit * 2].EnforeConnector(Orientations.UP, this.UpConnector);
		this.defaultColumn[0].EnforeConnector(Orientations.DOWN, this.DownConnector);
		this.defaultColumn[0].ExcludeConnector(Orientations.FORWARD, 11);
		this.defaultColumn[0].ExcludeConnector(Orientations.LEFT, 11);
		this.defaultColumn[0].ExcludeConnector(Orientations.RIGHT, 11);
		this.defaultColumn[0].ExcludeConnector(Orientations.BACK, 11);

		this.defaultColumnMode = false;
	}

	public void Generate() {
		this.destroyChildren();		
		this.Map = new Dictionary<Vector3i, Slot>();

		this.prepareModuleData();
		this.prepareDefaultColumn();

		this.Collapse();
	}

	public void Collapse(Vector3i start, Vector3i size) {
		var targets = new List<Vector3i>();
		for (int x = 0; x < size.X; x++) {
			for (int y = 0; y < size.Y; y++) {
				for (int z = 0; z < size.Z; z++) {
					targets.Add(start + new Vector3i(x, y, z));
				}
			}
		}
		this.Collapse(targets);
	}

	public void Collapse() {
		this.Collapse(-this.Size / 2, this.Size);
	}

	public void Collapse(IEnumerable<Vector3i> targets) {
		var slots = new HashSet<Slot>(targets.Select(target => this.GetSlot(target)).Where(slot => !slot.Collapsed));

		while (slots.Any()) {
			int minEntropy = slots.Min(slot => slot.Entropy);
			if (minEntropy == 0) {
				throw new Exception("Wavefunction collapse failed.");
			}
			var candidates = slots.Where(slot => !slot.Collapsed && slot.Entropy == minEntropy).ToList();
			
			var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
			selected.CollapseRandom();
			slots.Remove(selected);
		}

		Debug.Log("WFC complete.");
	}

	public Vector3 GetWorldspacePosition(Vector3i position) {
		return this.transform.position
			+ Vector3.up * MapGenerator.BlockSize / 2f
			+ new Vector3(
				(float)(position.X) * MapGenerator.BlockSize,
				(float)(position.Y) * MapGenerator.BlockSize,
				(float)(position.Z) * MapGenerator.BlockSize);
	}

	private void destroyChildren() {
		var children = new List<Transform>();
		foreach (Transform child in this.transform) {
			children.Add(child);
		}
		foreach (var child in children) {
			GameObject.DestroyImmediate(child.gameObject);
		}
	}

	public void EnforceWalkway(Vector3i start, int direction) {
		var slot = this.GetSlot(start);
		var toRemove = slot.Modules.Where(i => !this.Modules[i].GetFace(direction).Walkable).ToList();
		slot.RemoveModules(toRemove);
	}

	public void EnforceWalkway(Vector3i start, Vector3i destination) {
		int direction = Orientations.GetIndex((destination - start).ToVector3());
		this.EnforceWalkway(start, direction);
		this.EnforceWalkway(destination, (direction + 3) % 6);
	}

	public void Build() {
		foreach (var slot in this.Map.Values) {
			slot.Build();
		}
	}

	public bool VisualizeInEditor = false;

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmoForMyScript(MapGenerator mapGenerator, GizmoType gizmoType) {
		if (!mapGenerator.VisualizeInEditor) {
			return;
		}
		if (mapGenerator.Map == null) {
			return;
		}
		foreach (var slot in mapGenerator.Map.Values) {
			if (slot.Collapsed) {
				continue;
			}
			Gizmos.DrawWireSphere(mapGenerator.GetWorldspacePosition(slot.Position), 0.2f);
		}
	}
#endif
}
