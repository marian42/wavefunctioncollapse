using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TreePlacer : MonoBehaviour, IMapGenerationCallbackReceiver {
	private MapBehaviour mapBehaviour;

	public int MaxHeight = 4;

	public GameObject TreePrefab;

	public void OnEnable() {
		this.GetComponent<GenerateMapNearPlayer>().RegisterMapGenerationCallbackReceiver(this);
		this.mapBehaviour = this.GetComponent<MapBehaviour>();
	}

	public void OnDisable() {
		this.GetComponent<GenerateMapNearPlayer>().UnregisterMapGenerationCallbackReceiver(this);
	}

	private static Vector3 projectToGround(Vector3 position) {
		position.y = 100;
		RaycastHit hitInfo;
		if (Physics.Raycast(new Ray(position, Vector3.down), out hitInfo)) {
			return hitInfo.point;
		}
		return position;
	}

	public void OnGenerateChunk(Vector3Int chunkAddress, GenerateMapNearPlayer source) {
		var candidates = new List<Slot>();
		int startingHeight = Math.Min(this.mapBehaviour.MapHeight - 1, this.MaxHeight - 1);
		for (int x = source.ChunkSize * chunkAddress.x; x < source.ChunkSize * (chunkAddress.x + 1); x++ ) {
			for (int z = source.ChunkSize * chunkAddress.z; z < source.ChunkSize * (chunkAddress.z + 1); z++) {
				for (int y = startingHeight; y >= 0; y--) {
					var slot = this.mapBehaviour.Map.GetSlot(new Vector3Int(x, y, z));
					if (!slot.Collapsed || !slot.Module.Prototype.FlatSurface) {
						continue;
					} else if (slot.Module.Prototype.IsInterior) {
						break;
					} else {
						candidates.Add(slot);
						break;
					}
				}
			}
		}
		if (!candidates.Any()) {
			return;
		}
		foreach (var candindate in candidates) {
			var groundPosition = projectToGround(this.mapBehaviour.GetWorldspacePosition(candindate.Position));
			if (this.mapBehaviour.GetMapPosition(groundPosition) != candindate.Position) {
				continue;
			}
			this.PlantTree(groundPosition);
			break;
		}
	}

	public void PlantTree(Vector3 position) {
		var treeGameObject = GameObject.Instantiate(this.TreePrefab);
		treeGameObject.transform.position = position;
		var treeGenerator = treeGameObject.GetComponent<TreeGenerator>();
		treeGenerator.StartCoroutine("BuildCoroutine");
	}
}
