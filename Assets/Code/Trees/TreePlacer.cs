using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TreePlacer : MonoBehaviour, IMapGenerationCallbackReceiver {
	private class NotSuitableForTreeGrowingException : Exception {}

	private MapBehaviour mapBehaviour;

	public int MaxHeight = 4;

	public GameObject TreePrefab;

	private HashSet<int> modulesThatGrowTrees = null;

	private HashSet<Vector3Int> chunksWaitingForTrees = new HashSet<Vector3Int>();

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
					if (slot != null && !slot.ConstructionComplete) {
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

		// First, find a slot for the tree to grow and check if its neighbourhood is completely generated.
		// If nearby blocks are generated, start growing the tree.
		// Otherwise, add the chunk address to chunksWaitingForTrees and generate the tree once all surrounding *chunks*  are generated.
		try {
			this.chunksWaitingForTrees.Remove(chunkAddress);
			Vector3 treePosition = this.getTreePosition(chunkAddress, source.ChunkSize);
			if (this.checkIfNearbySlotsAreBuilt(this.mapBehaviour.GetMapPosition(treePosition), 2)) {
				this.StartCoroutine(this.PlantTree(treePosition, false));
			} else {
				this.chunksWaitingForTrees.Add(chunkAddress);
			}
		} catch (NotSuitableForTreeGrowingException) { }
		
		for (int x = chunkAddress.x - 1; x <= chunkAddress.x + 1; x++) {
			for (int z = chunkAddress.z - 1; x <= chunkAddress.z + 1; x++) {
				var queryChunkAddress = new Vector3Int(x, 0, z);
				if (this.chunksWaitingForTrees.Contains(queryChunkAddress) && this.checkIfNeighbourChunksAreGenerated(queryChunkAddress, source)) {
					try {
						this.chunksWaitingForTrees.Remove(queryChunkAddress);
						Vector3 treePosition = this.getTreePosition(queryChunkAddress, source.ChunkSize);
						this.StartCoroutine(this.PlantTree(treePosition, true));
					} catch (NotSuitableForTreeGrowingException) { }
				}
			}
		}
	}

	private bool checkIfNeighbourChunksAreGenerated(Vector3Int chunkAddress, GenerateMapNearPlayer generateMapNearPlayer) {
		for (int x = chunkAddress.x - 1; x <= chunkAddress.x + 1; x++) {
			for (int z = chunkAddress.z - 1; x <= chunkAddress.z + 1; x++) {
				var queryChunkAddress = new Vector3Int(x, 0, z);
				if (!generateMapNearPlayer.IsGenerated(queryChunkAddress)) {
					return false;
				}
			}
		}
		return true;
	}

	private Vector3 getTreePosition(Vector3Int chunkAddress, int chunkSize) {
		var candidates = new List<Slot>();
		int startingHeight = Math.Min(this.mapBehaviour.MapHeight - 1, this.MaxHeight - 1);
		for (int x = chunkSize * chunkAddress.x; x < chunkSize * (chunkAddress.x + 1); x++) {
			for (int z = chunkSize * chunkAddress.z; z < chunkSize * (chunkAddress.z + 1); z++) {
				for (int y = startingHeight; y >= 0; y--) {
					var slot = this.mapBehaviour.Map.GetSlot(new Vector3Int(x, y, z));
					if (slot != null && slot.Collapsed && this.modulesThatGrowTrees.Contains(slot.Module.Index)) {
						candidates.Add(slot);
						break;
					}
				}
			}
		}
		if (!candidates.Any()) {
			throw new NotSuitableForTreeGrowingException();
		}
		var candidate = candidates.GetBest(slot => -slot.Position.y);
		return this.mapBehaviour.GetWorldspacePosition(candidate.Position) + Vector3.down * 0.6f;
	}

	private IEnumerator PlantTree(Vector3 position, bool checkNearbySlots) {
		var slotPosition = this.mapBehaviour.GetMapPosition(position);

		if (checkNearbySlots) {
			while (!this.checkIfNearbySlotsAreBuilt(slotPosition, 2)) {
				yield return new WaitForSeconds(0.2f);
			}
		}

		var treeGameObject = GameObject.Instantiate(this.TreePrefab);
		treeGameObject.transform.position = position;
		treeGameObject.transform.SetParent(this.mapBehaviour.Map.GetSlot(slotPosition).GameObject.transform);
		var treeGenerator = treeGameObject.GetComponent<TreeGenerator>();
		if (treeGameObject.activeInHierarchy) {
			treeGenerator.StartCoroutine(treeGenerator.BuildCoroutine());
		}
	}
}
