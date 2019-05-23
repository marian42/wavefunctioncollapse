using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(SlotInspector))]
public class SlotInspectorEditor : Editor {

	private void showEditor(Slot slot, MapBehaviour mapBehaviour) {
		if (slot.Collapsed) {
			GUILayout.Label("Collapsed: " + slot.Module);
			GUILayout.Space(20f);
			GUILayout.Label("Add exclusion rules:");
			BlockBehaviourEditor.CreateNeighborExlusionUI(slot);
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

		foreach (var proto in prototypes.Keys) {
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
		GUILayout.Space(10f);

		if (map.IsSlotInitialized(position)) {
			this.showEditor(map.GetSlot(position), mapBehaviour);
		} else {
			if (GUILayout.Button("Create Slot")) {
				map.GetSlot(position);
			}
		}
	}
}