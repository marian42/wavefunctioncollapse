using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class Module : ISerializationCallbackReceiver {
	public string Name;

	public ModulePrototype Prototype;
	public GameObject Prefab;

	public int Rotation;
	
	// Direction -> Array of modules that may be placed adjacent in this direction
	[System.NonSerialized]
	public Module[][] PossibleNeighbors;

	[HideInInspector]
	public int Index;

	public Dictionary<Vector3i, ModuleSet> Cloud;

	public Module(GameObject prefab, int rotation, int index) {
		this.Rotation = rotation;
		this.Index = index;
		this.Prefab = prefab;
		this.Prototype = this.Prefab.GetComponent<ModulePrototype>();
		this.Name = this.Prototype.gameObject.name + " R" + rotation;
	}

	public bool Fits(int direction, Module module) {
		int otherDirection = (direction + 3) % 6;

		if (Orientations.IsHorizontal(direction)) {
			var f1 = this.Prototype.Faces[Orientations.Rotate(direction, this.Rotation)] as ModulePrototype.HorizontalFaceDetails;
			var f2 = module.Prototype.Faces[Orientations.Rotate(otherDirection, module.Rotation)] as ModulePrototype.HorizontalFaceDetails;
			return f1.Connector == f2.Connector && (f1.Symmetric || f1.Flipped != f2.Flipped);
		} else {
			var f1 = this.Prototype.Faces[direction] as ModulePrototype.VerticalFaceDetails;
			var f2 = module.Prototype.Faces[otherDirection] as ModulePrototype.VerticalFaceDetails;
			return f1.Connector == f2.Connector && (f1.Invariant || (f1.Rotation + this.Rotation) % 4 == (f2.Rotation + module.Rotation) % 4);
		}
	}

	public bool Fits(int direction, int connector) {
		if (Orientations.IsHorizontal(direction)) {
			var f = this.GetFace(direction) as ModulePrototype.HorizontalFaceDetails;
			return f.Connector == connector;
		} else {
			var f = this.Prototype.Faces[direction] as ModulePrototype.VerticalFaceDetails;
			return f.Connector == connector;
		}
	}

	public ModulePrototype.FaceDetails GetFace(int direction) {
		return this.Prototype.Faces[Orientations.Rotate(direction, this.Rotation)];
	}

	public override string ToString() {
		return this.Name;
	}

	[UnityEngine.SerializeField]
	private int[] possibleNeighborIds;

	[System.Serializable]
	private class SerializableVectorModuleSetKVP {
		public Vector3i Position;
		public ModuleSet ModuleSet;

		public SerializableVectorModuleSetKVP(Vector3i position, ModuleSet moduleSet) {
			this.Position = position;
			this.ModuleSet = moduleSet;
		}
	}

	[UnityEngine.SerializeField]
	private SerializableVectorModuleSetKVP[] cloud;
	
	public void OnBeforeSerialize() {
		if (this.PossibleNeighbors == null || this.PossibleNeighbors.Length != 6) {
			this.possibleNeighborIds = null;
			return;
		}

		int nMax = this.PossibleNeighbors.Max(a => a.Length);
		this.possibleNeighborIds = new int[6 * nMax];

		for (int d = 0; d < 6; d++) {
			for (int i = 0; i < nMax; i++) {
				if (this.PossibleNeighbors[d].Length > i) {
					this.possibleNeighborIds[d * nMax + i] = this.PossibleNeighbors[d][i].Index;
				} else {
					this.possibleNeighborIds[d * nMax + i] = -1;
				}
			}
		}

		if (this.Cloud != null) {
			this.cloud = this.Cloud.Select(kvp => new SerializableVectorModuleSetKVP(kvp.Key, kvp.Value)).ToArray();
		} else {
			this.cloud = null;
		}
	}

	public void OnAfterDeserialize() {
		if (this.cloud != null) {
			this.Cloud = new Dictionary<Vector3i, ModuleSet>();
			foreach (var kvp in this.cloud) {
				this.Cloud[kvp.Position] = kvp.ModuleSet;
			}
		}		
	}

	public void DeserializeNeigbors(Module[] modules) {
		if (this.possibleNeighborIds == null || this.possibleNeighborIds.Length == 0) {
			return;
		}

		this.PossibleNeighbors = new Module[6][];
		int nMax = this.possibleNeighborIds.Length / 6;
		for (int d = 0; d < 6; d++) {
			var neighbors = new List<Module>();
			for (int i = 0; i < nMax; i++) {
				if (this.possibleNeighborIds[d * nMax + i] != -1) {
					neighbors.Add(modules[this.possibleNeighborIds[d * nMax + i]]);
				}
			}
			this.PossibleNeighbors[d] = neighbors.ToArray();
		}
	}
}
