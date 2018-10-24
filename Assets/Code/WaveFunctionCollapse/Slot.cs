using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Slot {
	public Vector3i Position;

	private MapGenerator mapGenerator;

	public Module Module {
		get {
			return this.mapGenerator.Modules[this.ModuleIndex];
		}
	}

	public int ModuleIndex;

	// List of modules that can still be placed here
	public HashSet<int> Modules;

	// References to neigbor slots
	public Slot[] Neighbours;

	// Direction -> Module -> Number of entries in this.Modules that allow that module as a neighbor in that direction
	public int[][] NeighborCandidateHealth;

	public bool Collapsed {
		get {
			return this.ModuleIndex != -1;
		}
	}

	public int Entropy {
		get {
			return this.Modules.Count;
		}
	}

	public BlockBehaviour BlockBehaviour;

	public Slot(Vector3i position, MapGenerator mapGenerator) {
		this.Position = position;
		this.mapGenerator = mapGenerator;
		this.Modules = new HashSet<int>(Enumerable.Range(0, mapGenerator.Modules.Length));
		this.ModuleIndex = -1;
	}

	public void InitializeNeighbours() {
		this.Neighbours = new Slot[6];
		if (this.Position.X > 0) {
			this.Neighbours[0] = this.mapGenerator.Map[this.Position.X - 1, this.Position.Y, this.Position.Z];
		}
		if (this.Position.Y > 0) {
			this.Neighbours[1] = this.mapGenerator.Map[this.Position.X, this.Position.Y - 1, this.Position.Z];
		}
		if (this.Position.Z > 0) {
			this.Neighbours[2] = this.mapGenerator.Map[this.Position.X, this.Position.Y, this.Position.Z - 1];
		}
		if (this.Position.X < this.mapGenerator.SizeX - 1) {
			this.Neighbours[3] = this.mapGenerator.Map[this.Position.X + 1, this.Position.Y, this.Position.Z];
		}
		if (this.Position.Y < this.mapGenerator.SizeY - 1) {
			this.Neighbours[4] = this.mapGenerator.Map[this.Position.X, this.Position.Y + 1, this.Position.Z];
		}
		if (this.Position.Z < this.mapGenerator.SizeZ - 1) {
			this.Neighbours[5] = this.mapGenerator.Map[this.Position.X, this.Position.Y, this.Position.Z + 1];
		}
	}

	public void Collapse(int index) {
		if (this.Collapsed) {
			throw new System.InvalidOperationException("Slot is already collapsed.");
		}

		this.ModuleIndex = index;
		this.mapGenerator.LatestFilled = this;
		this.mapGenerator.SlotsFilled++;

		this.checkConsistency(index);

		var toRemove = this.Modules.ToList();
		toRemove.Remove(index);
		this.RemoveModules(toRemove);

		if (this.mapGenerator.BuildOnCollapse) {
			this.Build();
		}
	}

	private void checkConsistency(int index) {
		for (int d = 0; d < 6; d++) {
			if (this.Neighbours[d] != null && this.Neighbours[d].Collapsed && !this.Neighbours[d].Module.PossibleNeighbours[(d + 3) % 6].Contains(index)) {
				this.markRed();
				// This would be a result of inconsistent code, should not be possible.
				throw new Exception("Illegal collapse, not in neighbour list.");
			}
		}

		if (!this.Modules.Contains(index)) {
			this.markRed();
			// This would be a result of inconsistent code, should not be possible.
			throw new Exception("Illegal collapse!");
		}
	}

	public void CollapseRandom() {
		if (!this.Modules.Any()) {
			throw new Exception("No modules to select.");	
		}
		if (this.Collapsed) {
			throw new Exception("Slot is already collapsed.");
		}
		var candidates = this.Modules.ToList();
		float max = candidates.Select(i => this.mapGenerator.Modules[i].Probability).Sum();
		float roll = UnityEngine.Random.Range(0f, max);
		float p = 0;
		foreach (var candidate in candidates) {
			p += this.mapGenerator.Modules[candidate].Probability;
			if (p >= roll) {
				this.Collapse(candidate);
				return;
			}			
		}
		this.Collapse(candidates.First());
	}

	public void RemoveModules(List<int> modulesToRemove) {
		var affectedNeighbouredModules = Enumerable.Range(0, 6).Select(_ => new List<int>()).ToArray();

		foreach (int module in modulesToRemove) {
			if (!this.Modules.Contains(module) || module == this.ModuleIndex) {
				continue;
			}
			for (int d = 0; d < 6; d++) {
				foreach (int possibleNeighbour in this.mapGenerator.Modules[module].PossibleNeighbours[d]) {
					if (this.NeighborCandidateHealth[d][possibleNeighbour] == 1) {
						affectedNeighbouredModules[d].Add(possibleNeighbour);
					}
					this.NeighborCandidateHealth[d][possibleNeighbour]--;
				}
			}
			this.Modules.Remove(module);
		}

		if (this.Modules.Count == 0) {
			this.markRed();
			throw new Exception("Wavefunction collapse failed.");
		}

		for (int d = 0; d < 6; d++) {
			if (affectedNeighbouredModules[d].Any() && this.Neighbours[d] != null) {
				this.Neighbours[d].RemoveModules(affectedNeighbouredModules[d]);
			}
		}
	}

	private void markRed() {
		var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.parent = this.mapGenerator.transform;
		cube.GetComponent<MeshRenderer>().sharedMaterial.color = Color.red;
		cube.transform.position = this.GetPosition();
	}

	public void Build() {
		if (!this.Collapsed || this.Module.Prototype.Spawn == false) {
			return;
		}

		AbstractModulePrototype model = this.Module.Prototype;
		if (this.Module.Models.Count > 1) {
			float max = this.Module.Models.Select(m => m.Probability).Sum();
			float roll = UnityEngine.Random.Range(0f, max);
			float p = 0;
			foreach (var candidate in this.Module.Models) {
				p += candidate.Probability;
				if (p >= roll) {
					model = candidate;
					break;
				}
			}
		}

		var gameObject = GameObject.Instantiate(model.gameObject);
		GameObject.DestroyImmediate(gameObject.GetComponent<AbstractModulePrototype>());
		gameObject.transform.parent = this.mapGenerator.transform;
		gameObject.transform.position = this.GetPosition();
		gameObject.transform.rotation = Quaternion.Euler(Vector3.up * 90f * this.Module.Rotation);
		var blockBehaviour = gameObject.AddComponent<BlockBehaviour>();
		blockBehaviour.Prototype = this.Module.Prototype;
		blockBehaviour.Neighbours = new BlockBehaviour[6];
		for (int i = 0; i < 6; i++) {
			if (this.Neighbours[i] != null && this.Neighbours[i].BlockBehaviour != null) {
				var otherBlock = this.Neighbours[i].BlockBehaviour;
				blockBehaviour.Neighbours[i] = otherBlock;
				otherBlock.Neighbours[(i + 3) % 6] = blockBehaviour;
			}
		}
	}

	public Vector3 GetPosition() {
		return this.mapGenerator.GetWorldspacePosition(this.Position.X, this.Position.Y, this.Position.Z);
	}

	public void EnforeConnector(int direction, int connector) {
		var toRemove = this.Modules.Where(i => !this.mapGenerator.Modules[i].Fits(direction, connector)).ToList();
		this.RemoveModules(toRemove);
	}
}
