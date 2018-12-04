using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

public class MapBehaviour : MonoBehaviour, ISerializationCallbackReceiver {
	public InfiniteMap Map;

	public int MapHeight = 6;

	public BoundaryConstraint[] BoundaryConstraints;

	[HideInInspector, UnityEngine.SerializeField]
	public Module[] Modules;
	
	public void CreateModules() {
		this.Modules = ModulePrototype.CreateModules(true).ToArray();
		Module.All = this.Modules;
	}

	public Vector3 GetWorldspacePosition(Vector3i position) {
		return this.transform.position
			+ Vector3.up * InfiniteMap.BLOCK_SIZE / 2f
			+ new Vector3(
				(float)(position.X) * InfiniteMap.BLOCK_SIZE,
				(float)(position.Y) * InfiniteMap.BLOCK_SIZE,
				(float)(position.Z) * InfiniteMap.BLOCK_SIZE);
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

	public void Initialize() {
		this.Clear();
		this.Map = new InfiniteMap(this.MapHeight);
		if (this.BoundaryConstraints != null && this.BoundaryConstraints.Any()) {
			this.Map.ApplyBoundaryConstraints(this.BoundaryConstraints);
		}
	}

	public bool Initialized {
		get {
			return this.Map != null;
		}
	}
	
	public void Update() {
		if (this.Map == null || this.Map.BuildQueue == null) {
			return;
		}

		int itemsLeft = 50;

		while (this.Map.BuildQueue.Count != 0 && itemsLeft > 0) {
			var slot = this.Map.BuildQueue.Peek();
			if (slot == null) {
				return;
			}
			if (this.BuildSlot(slot)) {
				itemsLeft--;
			}
			this.Map.BuildQueue.Dequeue();
		}
	}

	public bool BuildSlot(Slot slot) {
		if (slot.GameObject != null) {
#if UNITY_EDITOR
			GameObject.DestroyImmediate(slot.GameObject);
#else
			GameObject.Destroy(slot.GameObject);
#endif
		}

		if (!slot.Collapsed || slot.Module.Prototype.Spawn == false) {
			return false;
		}
		var module = slot.Module;
		if (module == null) { // Can be null due to race conditions
			return false;
		}

		var gameObject = GameObject.Instantiate(module.Prototype.gameObject);
		gameObject.name = module.Prototype.gameObject.name + " " + slot.Position;
		GameObject.DestroyImmediate(gameObject.GetComponent<ModulePrototype>());
		gameObject.transform.parent = this.transform;
		gameObject.transform.position = this.GetWorldspacePosition(slot.Position);
		gameObject.transform.rotation = Quaternion.Euler(Vector3.up * 90f * module.Rotation);
		var blockBehaviour = gameObject.AddComponent<BlockBehaviour>();
		blockBehaviour.Slot = slot;
		slot.GameObject = gameObject;
		return true;
	}

	public void BuildAllSlots() {
		while (this.Map.BuildQueue.Count != 0) {
			this.BuildSlot(this.Map.BuildQueue.Dequeue());
		}
	}
	
	public void OnBeforeSerialize() { }

	public void OnAfterDeserialize() {
		if (this.Modules != null && this.Modules.Length != 0) {
			foreach (var module in this.Modules) {
				module.DeserializeNeigbors(this.Modules);
			}
		}
		Module.All = this.Modules;
	}

	public bool VisualizeSlots = false;

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmoForMyScript(MapBehaviour mapBehaviour, GizmoType gizmoType) {
		if (!mapBehaviour.VisualizeSlots) {
			return;
		}
		if (mapBehaviour.Map == null) {
			return;
		}
		foreach (var slot in mapBehaviour.Map.GetAllSlots()) {
			if (slot.Collapsed || slot.Modules.Count == Module.All.Length) {
				continue;
			}
			Handles.Label(mapBehaviour.GetWorldspacePosition(slot.Position), slot.Modules.Count.ToString());
		}
	}
#endif

#if UNITY_EDITOR
	public void SimplifyNeighborData() {
		const int height = 12;
		int count = 0;
		var center = new Vector3i(0, height / 2, 0);
		
		int p = 0;
		foreach (var module in ModuleData.Current) {
			var map = new InfiniteMap(height);
			var slot = map.GetSlot(center);
			try {
				slot.Collapse(module);
			}
			catch (CollapseFailedException exception) {
				this.BuildAllSlots();
				throw new InvalidOperationException("Module " + module.Name + " creates a failure at relative position " + (exception.Slot.Position - center) + ".");
			}
			for (int direction = 0; direction < 6; direction++) {
				var neighbor = slot.GetNeighbor(direction);
				int unoptimizedNeighborCount = module.PossibleNeighbors[direction].Length;
				module.PossibleNeighbors[direction] = module.PossibleNeighbors[direction].Where(m => neighbor.Modules.Contains(m)).ToArray();
				count += unoptimizedNeighborCount - module.PossibleNeighbors[direction].Length;
			}
			module.Cloud = new Dictionary<Vector3i, ModuleSet>();
			foreach (var cloudSlot in map.GetAllSlots()) {
				if (cloudSlot.Position.Equals(center)) {
					continue;
				}
				if (cloudSlot.Modules.Full) {
					continue;
				}
				module.Cloud[cloudSlot.Position - center] = cloudSlot.Modules;
			}
			Debug.Log(module.Cloud.Keys.Count);
			p++;
			EditorUtility.DisplayProgressBar("Simplifying... " + count, module.Name, (float)p / ModuleData.Current.Length);
		}
		Debug.Log("Removed " + count + " impossible neighbors.");
		EditorUtility.ClearProgressBar();
	}
#endif
}
