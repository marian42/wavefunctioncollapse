using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Slot {
	public int X;
	public int Y;
	public int Z;

	private MapGenerator mapGenerator;

	public Module Module;

	public HashSet<int> Modules;

	public Slot[] Neighbours;

	public bool Collapsed {
		get {
			return this.Module != null;
		}
	}

	public int Entropy {
		get {
			return this.Modules.Count;
		}
	}

	public Slot(int x, int y, int z, MapGenerator mapGenerator) {
		this.X = x;
		this.Y = y;
		this.Z = z;
		this.mapGenerator = mapGenerator;
		this.Modules = new HashSet<int>(Enumerable.Range(0, mapGenerator.Modules.Length));
	}

	public void InitializeNeighbours() {
		this.Neighbours = new Slot[6];
		if (this.X > 0) {
			this.Neighbours[0] = this.mapGenerator.Map[this.X - 1, this.Y, this.Z];
		}
		if (this.Y > 0) {
			this.Neighbours[1] = this.mapGenerator.Map[this.X, this.Y - 1, this.Z];
		}
		if (this.Z > 0) {
			this.Neighbours[2] = this.mapGenerator.Map[this.X, this.Y, this.Z - 1];
		}
		if (this.X < this.mapGenerator.SizeX - 1) {
			this.Neighbours[3] = this.mapGenerator.Map[this.X + 1, this.Y, this.Z];
		}
		if (this.Y < this.mapGenerator.SizeY - 1) {
			this.Neighbours[4] = this.mapGenerator.Map[this.X, this.Y + 1, this.Z];
		}
		if (this.Z < this.mapGenerator.SizeZ - 1) {
			this.Neighbours[5] = this.mapGenerator.Map[this.X, this.Y, this.Z + 1];
		}
	}

	public bool HasConnector(int direction, int connector) {
		return this.Modules.Any(i => this.mapGenerator.Modules[i].Connectors[direction] == connector);
	}

	public void Collapse(int index) {
		this.Module = this.mapGenerator.Modules[index];
		this.Build();
		this.mapGenerator.SlotsFilled++;

		if (!this.Modules.Contains(index)) {
			this.markRed();
			throw new Exception("Illegal collapse!");
		}

		var toRemove = this.Modules.ToList();
		toRemove.Remove(index);
		this.RemoveModules(toRemove);
	}

	public void CollapseRandom() {
		if (!this.Modules.Any()) {
			throw new Exception("No modules to select.");	
		}
		if (this.Collapsed) {
			throw new Exception("Slot is already collapsed.");
		}
		int i = UnityEngine.Random.Range(0, this.Modules.Count);
		this.Collapse(this.Modules.ElementAt(i));
	}

	public void RemoveModules(List<int> modules) {
		if (!this.Modules.Any()) {
			return;
		}

		var affectedConnectors = new HashSet<int>[6];
		for (int i = 0; i < 6; i++) {
			affectedConnectors[i] = new HashSet<int>();
		}

		foreach (var i in modules) {
			if (this.Modules.Contains(i)) {
				for (int j = 0; j < 6; j++) {
					affectedConnectors[j].Add(this.mapGenerator.Modules[i].Connectors[j]);
				}
			}
		}

		foreach (var i in modules) {
			this.Modules.Remove(i);
		}

		if (this.Modules.Count == 0) {
			this.markRed();
			throw new Exception("No more modules allowed.");
		}		

		for (int d = 0; d < 6; d++) {
			if (this.Neighbours[d] == null || !affectedConnectors[d].Any()) {
				continue;
			}

			foreach (var connector in affectedConnectors[d].ToArray()) {
				if (this.HasConnector(d, connector)) {
					affectedConnectors[d].Remove(connector);
				}
			}
			if (affectedConnectors[d].Any()) {
				this.Neighbours[d].RemoveConnectors((d + 3) % 6, affectedConnectors[d]);
			}
		}
	}

	private void markRed() {
		var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.parent = this.mapGenerator.transform;
		cube.GetComponent<MeshRenderer>().sharedMaterial.color = Color.red;
		cube.transform.position = this.GetPosition();
	}

	public void RemoveConnectors(int direction, HashSet<int> connectors) {
		this.RemoveModules(this.Modules.Where(module => connectors.Contains(this.mapGenerator.Modules[module].Connectors[direction])).ToList());
	}

	public void Build() {
		if (this.Module == null) {
			return;
		}

		var gameObject = new GameObject();
		var moduleBehaviour = gameObject.AddComponent<ModuleBehaviour>();
		moduleBehaviour.Initialize(this.Module, this.mapGenerator.Material);
		moduleBehaviour.Number = Array.IndexOf(this.mapGenerator.Modules, this.Module);
		gameObject.transform.parent = this.mapGenerator.transform;
		gameObject.transform.position = this.GetPosition();
	}

	public Vector3 GetPosition() {
		return this.mapGenerator.GetWorldspacePosition(this.X, this.Y, this.Z);
	}
}
