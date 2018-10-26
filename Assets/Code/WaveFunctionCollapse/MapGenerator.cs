using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;

public class MapGenerator : MonoBehaviour, IMap {
	public const float BlockSize = 2f;

	[HideInInspector]
	public Module[] Modules;

	public Dictionary<Vector3i, Slot> Map;

	public int InitializationAreaSize = 4;

	public int Height = 8;

	public int UpConnector;
	public int DownConnector;

	public bool AllowExclusions = true;

	public int RangeLimit = 20;

	private DefaultColumn defaultColumn;

	public Slot GetSlot(Vector3i position, bool create) {
		if (position.Y >= this.Height || position.Y < 0) {
			return null;
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

		this.Map[position] = new Slot(position, this, this.defaultColumn.GetSlot(position)); ;
		return this.Map[position];
	}

	public Slot GetSlot(Vector3i position) {
		return this.GetSlot(position, true);
	}

	public Slot GetSlot(int x, int y, int z, bool create = true) {
		return this.GetSlot(new Vector3i(x, y, z), create);
	}
		
	private void createModules() {
		this.Modules = ModulePrototype.CreateModules(this).ToArray();
	}	

	public void Initialize() {
		this.destroyChildren();		
		this.Map = new Dictionary<Vector3i, Slot>();

		this.createModules();
		this.defaultColumn = new DefaultColumn(this);

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
		this.Collapse(new Vector3i(- this.InitializationAreaSize / 2, 0, - this.InitializationAreaSize / 2), new Vector3i(this.InitializationAreaSize, this.Height, this.InitializationAreaSize));
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
