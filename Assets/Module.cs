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

	public Mesh mesh;

	public int[] Connectors;

	public ModulePrototype Prototype;

	public Module(ModulePrototype prototype, int rotation, MapGenerator mapGenerator) {
		this.Prototype = prototype;
		this.mesh = prototype.GetMesh();
		this.Rotation = rotation;
	}
}
