using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(SlotInspector))]
public class SlotInspectorEditor : Editor {

	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		SlotInspector slotInspector = (SlotInspector)target;
		var mapBehaviour = slotInspector.MapBehaviour;
		var map = mapBehaviour.MapGenerator;
		if (mapBehaviour == null) {
			return;
		}

		if (!mapBehaviour.Initialized) {
			if (GUILayout.Button("Initialize Map")) {
				mapBehaviour.Initialize();
			}
			return;
		}

		var position = slotInspector.GetPosition();
		GUILayout.Label("Position: " + position);

		var slot = map.GetSlot(position, false);

		if (slot == null) {
			if (GUILayout.Button("CreateSlot")) {
				map.GetSlot(position);
			}
			return;
		}

		if (slot.Collapsed) {			
			GUILayout.Label("Collapsed: " + slot.Module);
			return;
		}

		GUILayout.Label("Possible modules: " + slot.Modules.Count() + " / " + mapBehaviour.Modules.Count());

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

			EditorGUILayout.PrefixLabel(proto.gameObject.name);
			foreach (var module in list) {
				if (GUILayout.Button("R" + module.Rotation)) {
					slot.Collapse(module);
					mapBehaviour.BuildAllSlots();
				}
			}

			GUILayout.EndHorizontal();
		}		
	}
}