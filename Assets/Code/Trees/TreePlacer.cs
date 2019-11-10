using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TreePlacer : MonoBehaviour, IMapGenerationCallbackReceiver {
	private MapBehaviour mapBehaviour;

	public int MaxHeight = 4;

	public GameObject TreePrefab;

	private HashSet<int> modulesThatGrowTrees = null;

	private void prepareModulesThatGrowTrees() {
		this.modulesThatGrowTrees = new HashSet<int>();

		foreach (var module in this.mapBehaviour.ModuleData.Modules) {
			if (module.Prototype.GetComponent<TreeGrowingPrototype>() != null) {
				this.modulesThatGrowTrees.Add(module.Index);
			}
		}
	}

	private bool checkIfNearbySlotsAreBuilt(Vector3Int slotPosition, int range) {
		for (int x = slotPosition.x - range; x <= slotPosition.x + range; x++) {
			for (int y = slotPosition.y; y < this.mapBehaviour.MapHeight; y++) {
				for (int z = slotPosition.z - range; z <= slotPosition.z + range; z++) {
					var slot = this.mapBehaviour.Map.GetSlot(new Vector3Int(x, y, z));
					if (!slot.ConstructionComplete) {
						return false;
					}
				}
			}
		}
		return true;
	}

	public void OnEnable() {
		this.GetComponent<GenerateMapNearPlayer>().RegisterMapGenerationCallbackReceiver(this);
		this.mapBehaviour = this.GetComponent<MapBehaviour>();
	}

	public void OnDisable() {
		this.GetComponent<GenerateMapNearPlayer>().UnregisterMapGenerationCallbackReceiver(this);
	}

	public void OnGenerateChunk(Vector3Int chunkAddress, GenerateMapNearPlayer source) {
		if (this.modulesThatGrowTrees == null) {
			this.prepareModulesThatGrowTrees();
		}
		var candidates = new List<Slot>();
		int startingHeight = Math.Min(this.mapBehaviour.MapHeight - 1, this.MaxHeight - 1);
		for (int x = source.ChunkSize * chunkAddress.x; x < source.ChunkSize * (chunkAddress.x + 1); x++ ) {
			for (int z = source.ChunkSize * chunkAddress.z; z < source.ChunkSize * (chunkAddress.z + 1); z++) {
				for (int y = startingHeight; y >= 0; y--) {
					var slot = this.mapBehaviour.Map.GetSlot(new Vector3Int(x, y, z));
					if (slot.Collapsed && this.modulesThatGrowTrees.Contains(slot.Module.Index)) {
						candidates.Add(slot);
						break;
					}
				}
			}
		}
		if (!candidates.Any()) {
			return;
		}
		var candidate = candidates.GetBest(slot => -slot.Position.y);
		var groundPosition = this.mapBehaviour.GetWorldspacePosition(candidate.Position) + Vector3.down * 0.6f;
		this.StartCoroutine(this.PlantTree(groundPosition));
		this.PlantTree(groundPosition);
	}

	private IEnumerator PlantTree(Vector3 position) {
		var slotPosition = this.mapBehaviour.GetMapPosition(position);
		while (!this.checkIfNearbySlotsAreBuilt(slotPosition, 2)) {
			yield return null;
		}

		var treeGameObject = GameObject.Instantiate(this.TreePrefab);
		treeGameObject.transform.position = position;
		var treeGenerator = treeGameObject.GetComponent<TreeGenerator>();
		treeGenerator.StartCoroutine(treeGenerator.BuildCoroutine());
	}
}
