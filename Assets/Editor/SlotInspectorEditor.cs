using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(SlotInspector))]
public class SlotInspectorEditor : Editor {
	private string filterString = "";

	private void showEditor(Slot slot, MapBehaviour mapBehaviour) {
		if (slot.Collapsed) {
			GUILayout.Label("Collapsed: " + slot.Module);
			GUILayout.Space(20f);
			GUILayout.Label("Add exclusion rules:");
			this.createNeighborExlusionUI(slot, mapBehaviour);
			return;
		}

		GUILayout.Label("Possible modules: " + slot.Modules.Count() + " / " + ModuleData.Current.Count());
		GUILayout.Label("Entropy: " + slot.Modules.Entropy);

		if (GUILayout.Button("Collapse Random")) {
			slot.CollapseRandom();
			mapBehaviour.BuildAllSlots();
		}

		var prototypes = new Dictionary<ModulePrototype, List<Module>>();

		foreach (var module in slot.Modules.ToArray()) {
			var proto = module.Prototype;
			if (!prototypes.ContainsKey(proto)) {
				prototypes[proto] = new List<Module>();
			}
			prototypes[proto].Add(module);
		}

		if (prototypes.Any()) {
			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Filter: ");
			this.filterString = GUILayout.TextField(this.filterString);
			GUILayout.EndHorizontal();
		}

		int hiddenByFilter = 0;
		foreach (var proto in prototypes.Keys) {
			if (this.filterString != "" && !proto.gameObject.name.ToLower().Contains(this.filterString.ToLower())) {
				hiddenByFilter++;
				continue;
			}
			
			var list = prototypes[proto];

			GUILayout.BeginHorizontal();

			EditorGUILayout.PrefixLabel(proto.gameObject.name + " (" + (100f * list.Sum(module => module.Prototype.Probability) / slot.Modules.Sum(module => module.Prototype.Probability)).ToString("0.0") + "%)");
			foreach (var module in list) {
				if (GUILayout.Button("R" + module.Rotation)) {
					slot.Collapse(module);
					mapBehaviour.BuildAllSlots();
				}
			}

			GUILayout.EndHorizontal();
		}

		if (hiddenByFilter > 0) {
			GUILayout.Label("(+" + hiddenByFilter + " that don't match the filter query)");
		}

		var defaultSlot = mapBehaviour.Map.GetDefaultSlot(slot.Position.y);
		var removedPrototypes = new List<ModulePrototype>();
		var removedByDefault = new List<ModulePrototype>();

		foreach (var module in ModuleData.Current) {
			if (!prototypes.ContainsKey(module.Prototype)) {
				prototypes.Add(module.Prototype, null);
				if (defaultSlot != null && !defaultSlot.Modules.Contains(module)) {
					removedByDefault.Add(module.Prototype);
				} else {
					removedPrototypes.Add(module.Prototype);
				}
			}
		}

		if (removedPrototypes.Any()) {
			GUILayout.Space(15f);
			GUILayout.Label("Removed modules:");
			foreach (var prototype in removedPrototypes) {
				GUILayout.Label(prototype.gameObject.name);
			}
		}
		if (removedByDefault.Any()) {
			GUILayout.Space(15f);
			GUILayout.Label("Modules always removed at this y coordinate:");
			foreach (var prototype in removedByDefault) {
				GUILayout.Label(prototype.gameObject.name);
			}
		}
	}

	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		SlotInspector slotInspector = (SlotInspector)target;
		var mapBehaviour = slotInspector.MapBehaviour;
		var map = mapBehaviour.Map;
		if (mapBehaviour == null) {
			return;
		}

		var position = slotInspector.MapBehaviour.GetMapPosition(slotInspector.transform.position);
		GUILayout.Label("Position: " + position);

		if (!mapBehaviour.Initialized) {
			if (GUILayout.Button("Initialize Map")) {
				mapBehaviour.Initialize();
				mapBehaviour.Map.GetSlot(position);
			}
			return;
		}

		if (GUILayout.Button("Reset Map")) {
			mapBehaviour.Clear();
			mapBehaviour.Initialize();
			mapBehaviour.Map.GetSlot(position);
		}

		if (mapBehaviour.Map.History.Any() && GUILayout.Button("Undo Last Collapse")) {
			GameObject.DestroyImmediate(mapBehaviour.Map.History.Peek().Slot.GameObject);
			mapBehaviour.Map.Undo(1);
		}

		GUILayout.Space(10f);

		if (map.IsSlotInitialized(position)) {
			this.showEditor(map.GetSlot(position), mapBehaviour);
		} else {
			if (GUILayout.Button("Create Slot")) {
				map.GetSlot(position);
			}
		}
	}

	public void OnSceneGUI() {
		SlotInspector slotInspector = (SlotInspector)this.target;
		slotInspector.transform.position = slotInspector.MapBehaviour.GetWorldspacePosition(slotInspector.MapBehaviour.GetMapPosition(slotInspector.transform.position));
	}

	private void createNeighborExlusionUI(Slot slot, MapBehaviour mapBehaviour) {
		var style = new GUIStyle();

		for (int i = 0; i < 6; i++) {
			GUILayout.Space(10f);
			style.normal.textColor = getColor(i);
			var neighbor = slot.GetNeighbor(i);

			GUILayout.Label(Orientations.Names[i], style);
			if (neighbor == null || !neighbor.Collapsed) {
				GUILayout.Label("(No neighbor)");
				continue;
			}

			GUILayout.Label(neighbor.Module.ToString());

			if (neighbor.Module == null) {
				continue;
			}

			var ownFace = slot.Module.GetFace(i);
			var neighborFace = neighbor.Module.GetFace((i + 3) % 6);

			if (ownFace.ExcludedNeighbours.Contains(neighbor.Module.Prototype) && neighborFace.ExcludedNeighbours.Contains(slot.Module.Prototype)) {
				GUILayout.Label("(Already exlcuded)");
				continue;
			}

			if (GUILayout.Button("Exclude neighbor")) {
				if (!ownFace.ExcludedNeighbours.Contains(neighbor.Module.Prototype)) {
					ownFace.ExcludedNeighbours = ownFace.ExcludedNeighbours.Concat(new ModulePrototype[] { neighbor.Module.Prototype }).ToArray();
				}
				if (!neighborFace.ExcludedNeighbours.Contains(slot.Module.Prototype)) {
					neighborFace.ExcludedNeighbours = neighborFace.ExcludedNeighbours.Concat(new ModulePrototype[] { slot.Module.Prototype }).ToArray();
				}

				mapBehaviour.ModuleData.SavePrototypes();
				Debug.Log("Added exclusion rule.");
			}

			if (neighborFace.Walkable) {
				GUILayout.Label("(Neighbor is walkable)");
				continue;
			}

			if (ownFace.EnforceWalkableNeighbor && !neighborFace.Walkable) {
				GUILayout.Label("(Already exlcuded by walkability constraint)");
				continue;
			}

			if (!ownFace.EnforceWalkableNeighbor && !neighborFace.Walkable && GUILayout.Button("Enforce Walkable neighbor")) {
				ownFace.EnforceWalkableNeighbor = true;
				mapBehaviour.ModuleData.SavePrototypes();
				Debug.Log("Added exclusion rule.");
			}
		}
	}

	private Color getColor(int direction) {
		switch (direction) {
			case 0: return Color.red;
			case 1: return Color.green;
			case 2: return Color.blue;
			case 3: return Color.red;
			case 4: return Color.green;
			case 5: return Color.blue;
			default: throw new System.NotImplementedException();
		}
	}
}