using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

[CreateAssetMenu(menuName = "Wave Function Collapse/Module Data", fileName = "modules.asset")]
public class ModuleData : ScriptableObject, ISerializationCallbackReceiver {
	public static Module[] Current;

	public GameObject Prototypes;

	public Module[] Modules;

#if UNITY_EDITOR
	public void SimplifyNeighborData() {
		ModuleData.Current = this.Modules;
		const int height = 12;
		int count = 0;
		var center = new Vector3Int(0, height / 2, 0);

		int p = 0;
		foreach (var module in this.Modules) {
			var map = new InfiniteMap(height);
			var slot = map.GetSlot(center);
			try {
				slot.Collapse(module);
			}
			catch (CollapseFailedException exception) {
				throw new InvalidOperationException("Module " + module.Name + " creates a failure at relative position " + (exception.Slot.Position - center) + ".");
			}
			for (int direction = 0; direction < 6; direction++) {
				var neighbor = slot.GetNeighbor(direction);
				int unoptimizedNeighborCount = module.PossibleNeighbors[direction].Count;
				module.PossibleNeighbors[direction].Intersect(neighbor.Modules);
				count += unoptimizedNeighborCount - module.PossibleNeighbors[direction].Count;
			}
			module.PossibleNeighborsArray = module.PossibleNeighbors.Select(ms => ms.ToArray()).ToArray();
			p++;
			EditorUtility.DisplayProgressBar("Simplifying... " + count, module.Name, (float)p / this.Modules.Length);
		}
		Debug.Log("Removed " + count + " impossible neighbors.");
		EditorUtility.ClearProgressBar();
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
	}



	private IEnumerable<ModulePrototype> getPrototypes() {
		foreach (Transform transform in this.Prototypes.transform) {
			var item = transform.GetComponent<ModulePrototype>();
			if (item != null && item.enabled) {
				yield return item;
			}
		}
	}

	public void CreateModules(bool respectNeigborExclusions = true) {
		int count = 0;
		var modules = new List<Module>();

		var prototypes = this.getPrototypes().ToArray();

		var scenePrototype = new Dictionary<Module, ModulePrototype>();

		for (int i = 0; i < prototypes.Length; i++) {
			var prototype = prototypes[i];
			for (int face = 0; face < 6; face++) {
				if (prototype.Faces[face].ExcludedNeighbours == null) {
					prototype.Faces[face].ExcludedNeighbours = new ModulePrototype[0];
				}
			}

			for (int rotation = 0; rotation < 4; rotation++) {
				if (rotation == 0 || !prototype.CompareRotatedVariants(0, rotation)) {
					var module = new Module(prototype.gameObject, rotation, count);
					modules.Add(module);
					scenePrototype[module] = prototype;
					count++;
				}
			}

			EditorUtility.DisplayProgressBar("Creating module prototypes...", prototype.gameObject.name, (float)i / prototypes.Length);
		}

		ModuleData.Current = modules.ToArray();

		foreach (var module in modules) {
			module.PossibleNeighbors = new ModuleSet[6];
			for (int direction = 0; direction < 6; direction++) {
				var face = scenePrototype[module].Faces[Orientations.Rotate(direction, module.Rotation)];
				module.PossibleNeighbors[direction] = new ModuleSet(modules
					.Where(neighbor => module.Fits(direction, neighbor)
						&& (!respectNeigborExclusions || (
							!face.ExcludedNeighbours.Contains(scenePrototype[neighbor])
							&& !scenePrototype[neighbor].Faces[Orientations.Rotate((direction + 3) % 6, neighbor.Rotation)].ExcludedNeighbours.Contains(scenePrototype[module]))
							&& (!face.EnforceWalkableNeighbor || scenePrototype[neighbor].Faces[Orientations.Rotate((direction + 3) % 6, neighbor.Rotation)].Walkable)
							&& (face.Walkable || !scenePrototype[neighbor].Faces[Orientations.Rotate((direction + 3) % 6, neighbor.Rotation)].EnforceWalkableNeighbor))
					));
			}

			module.PossibleNeighborsArray = module.PossibleNeighbors.Select(ms => ms.ToArray()).ToArray();
		}
		EditorUtility.ClearProgressBar();

		this.Modules = modules.ToArray();
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
	}
#endif

	public void OnBeforeSerialize() {
		
	}

	public void OnAfterDeserialize() {
		ModuleData.Current = this.Modules;
		foreach (var module in this.Modules) {
			module.PossibleNeighborsArray = module.PossibleNeighbors.Select(ms => ms.ToArray()).ToArray();
		}
	}

	public void SavePrototypes() {
		EditorUtility.SetDirty(this.Prototypes);
		AssetDatabase.SaveAssets();
		foreach (var module in this.Modules) {
			module.Prototype = module.Prefab.GetComponent<ModulePrototype>();
		}
	}
}
