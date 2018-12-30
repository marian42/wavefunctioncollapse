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

		var slot = map.GetSlot(position, false);

		if (slot == null) {
			if (GUILayout.Button("CreateSlot")) {
				map.GetSlot(position);
			}
		} else {
			this.showEditor(slot, mapBehaviour);
		}
	}
}