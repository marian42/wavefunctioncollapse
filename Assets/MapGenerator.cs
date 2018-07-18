using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class MapGenerator : MonoBehaviour {

	public Transform BlueprintContainer;

	public const float BlockSize = 2f;

	public Vector3 MapSize;

	public Material Material;

	[HideInInspector]
	public Module[] Modules;

	public Slot[, ,] Map;

	[HideInInspector]
	public int SizeX;
	[HideInInspector]
	public int SizeY;
	[HideInInspector]
	public int SizeZ;

	[HideInInspector]
	public int SlotsFilled = 0;

	public IEnumerable<Slot> FlatMap {
		get {
			for (int y = this.SizeY - 1; y >= 0; y--) {
				for (int x = 0; x < this.SizeX; x++) {
					for (int z = 0; z < this.SizeZ; z++) {
						yield return this.Map[x, y, z];
					}
				}
			}
		}
	}

	void Start () {
		
	}
	
	void Update () {
		
	}

	private void createModules() {
		this.Modules = ModulePrototype.CreateModules(this).ToArray();
	}

	public IEnumerator Generate() {
		this.destroyChildren();
		this.createModules();

		this.SizeX = (int)this.MapSize.x;
		this.SizeY = (int)this.MapSize.y;
		this.SizeZ = (int)this.MapSize.z;
		this.Map = new Slot[SizeX, SizeY, SizeZ];

		for (int x = 0; x < this.SizeX; x++) {
			for (int y = 0; y < this.SizeY; y++) {
				for (int z = 0; z < this.SizeZ; z++) {
					this.Map[x, y, z] = new Slot(x, y, z, this);
				}
			}
		}

		var slotNeighboursInitialState = new int[6][];
		for (int i = 0; i < 6; i++) {
			slotNeighboursInitialState[i] = new int[this.Modules.Length];
			foreach (var module in this.Modules) {
				foreach (int possibleNeighbour in module.PossibleNeighbours[i]) {
					slotNeighboursInitialState[i][possibleNeighbour]++;
				}
			}
		}

		for (int d = 0; d < 6; d++) {
			for (int i = 0; i < this.Modules.Count(); i++) {
				if (slotNeighboursInitialState[d][i] == 0) {
					throw new Exception("Module " + this.Modules[i].Prototype.name + " cannot be reached from direction " + d + " (" + this.Modules[i].Prototype.Faces[d].ToString() + ")!");
				}
			}
		}

		foreach (var slot in this.FlatMap) {
			slot.InitializeNeighbours();
			slot.PossibleNeighbours = slotNeighboursInitialState.Select(a => a.ToArray()).ToArray();
		}

		this.SlotsFilled = 0;
		int total = this.SizeX * this.SizeY * this.SizeZ;

		this.Map[0, 0, 0].CollapseRandom();

		while (this.SlotsFilled < total) {
			this.Collapse();
			yield return new WaitForSeconds(0.5f);
		}
	}

	public void Collapse() {
		int minEntropy = this.FlatMap.Where(slot => !slot.Collapsed).Min(slot => slot.Entropy);
		var candidates = this.FlatMap.Where(slot => !slot.Collapsed && slot.Entropy == minEntropy).ToList();
		if (minEntropy == 0) {
			throw new Exception("Wavefunction collapse failed.");
		}
		
		int index = UnityEngine.Random.Range(0, candidates.Count);
		candidates[index].CollapseRandom();
	}

	public Vector3 GetWorldspacePosition(int x, int y, int z) {
		return this.transform.position
			+ Vector3.up * MapGenerator.BlockSize / 2f
			+ new Vector3(
				(float)(x) * MapGenerator.BlockSize,
				(float)(y) * MapGenerator.BlockSize,
				(float)(z) * MapGenerator.BlockSize);
	}

	private void destroyChildren() {
		var children = new List<Transform>();
		foreach (Transform child in this.transform) {
			children.Add(child);
		}
		foreach (var child in children) {
			GameObject.DestroyImmediate(child.gameObject);
		}
	}

	private void createBehaviours() {
		var container = new GameObject("slots").transform;
		container.parent = this.transform;
		foreach (var slot in this.FlatMap) {
			var gameObject = new GameObject();
			var slotBehaviour = gameObject.AddComponent<SlotBehaviour>();
			slotBehaviour.Slot = slot;
			gameObject.transform.parent = container;
			gameObject.transform.position = slot.GetPosition();
		}
	}
}
