using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[System.Serializable]
public class Module {
	public Quaternion Orientation {
		get {
			return Quaternion.Euler(Vector3.up * this.Rotation * 90f);
		}
	}

	public int Rotation;

	public int[][] PossibleNeighbours;

	public ModulePrototype Prototype;

	public List<AbstractModulePrototype> Models;

	public float Probability;

	public Module(ModulePrototype prototype, int rotation, MapGenerator mapGenerator) {
		this.Prototype = prototype;
		this.Rotation = rotation;
		this.Models = new List<AbstractModulePrototype>();
		this.Models.Add(prototype);
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
			var f = this.Prototype.Faces[Orientations.Rotate(direction, this.Rotation)] as ModulePrototype.HorizontalFaceDetails;
			return f.Connector == connector;
		} else {
			var f = this.Prototype.Faces[direction] as ModulePrototype.VerticalFaceDetails;
			return f.Connector == connector;
		}
	}
}
