using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class Module {
	public string Name;

	public ModulePrototype Prototype;
	public GameObject Prefab;

	public int Rotation;
	
	public ModuleSet[] PossibleNeighbors;
	public Module[][] PossibleNeighborsArray;

	[HideInInspector]
	public int Index;

	// This is precomputed to make entropy calculation faster
	public float PLogP;

	public Module(GameObject prefab, int rotation, int index) {
		this.Rotation = rotation;
		this.Index = index;
		this.Prefab = prefab;
		this.Prototype = this.Prefab.GetComponent<ModulePrototype>();
		this.Name = this.Prototype.gameObject.name + " R" + rotation;
		this.PLogP = this.Prototype.Probability * Mathf.Log(this.Prototype.Probability);
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

	[System.Serializable]
	private class SerializableVectorModuleSetKVP {
		public Vector3Int Position;
		public ModuleSet ModuleSet;

		public SerializableVectorModuleSetKVP(Vector3Int position, ModuleSet moduleSet) {
			this.Position = position;
			this.ModuleSet = moduleSet;
		}
	}	
}
