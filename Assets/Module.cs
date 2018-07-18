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

	public Module(ModulePrototype prototype, int rotation, MapGenerator mapGenerator) {
		this.Prototype = prototype;
		this.Rotation = rotation;
	}

	private static readonly int[] horizontalFaces = { 0, 2, 3, 5 };

	public bool Fits(int direction, Module module) {
		int otherDirection = (direction + 3) % 6;

		if (horizontalFaces.Contains(direction)) {
			var f1 = this.Prototype.Faces[horizontalFaces[(Array.IndexOf(horizontalFaces, direction) + this.Rotation) % 4]] as ModulePrototype.HorizontalFaceDetails;
			var f2 = module.Prototype.Faces[horizontalFaces[(Array.IndexOf(horizontalFaces, otherDirection) + module.Rotation) % 4]] as ModulePrototype.HorizontalFaceDetails;
			return f1.Connector == f2.Connector && (f1.Symmetric || f1.Flipped != f2.Flipped);
		} else {
			var f1 = this.Prototype.Faces[direction] as ModulePrototype.VerticalFaceDetails;
			var f2 = module.Prototype.Faces[otherDirection] as ModulePrototype.VerticalFaceDetails;
			return f1.Connector == f2.Connector && (f1.Invariant || (f1.Rotation + this.Rotation) % 4 == (f2.Rotation + module.Rotation) % 4);
		}
	}
}
