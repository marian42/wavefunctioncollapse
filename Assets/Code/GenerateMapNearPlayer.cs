using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MapBehaviour))]
public class GenerateMapNearPlayer : MonoBehaviour {

	private MapBehaviour mapBehaviour;
	private InfiniteMap map;

	public Transform Target;

	public int ChunkSize = 4;

	public float Range = 30;

	private Vector3 targetPosition;
	private Vector3 mapPosition;

	private HashSet<Vector3Int> generatedChunks;

	private Thread thread;

	void Start() {
		this.generatedChunks = new HashSet<Vector3Int>();
		this.mapBehaviour = this.GetComponent<MapBehaviour>();
		this.mapBehaviour.Initialize();
		this.map = this.mapBehaviour.Map;
		this.generate();
		this.mapBehaviour.BuildAllSlots();

		this.thread = new Thread(this.generatorThread);
		this.thread.Start();
	}

	public void OnDisable() {
		this.thread.Abort();
	}

	private void generate() {
		float chunkSize = InfiniteMap.BLOCK_SIZE * this.ChunkSize;

		float targetX = this.targetPosition.x - this.mapPosition.x + InfiniteMap.BLOCK_SIZE / 2;
		float targetZ = this.targetPosition.z - this.mapPosition.z + InfiniteMap.BLOCK_SIZE / 2;

		int chunkX = Mathf.FloorToInt(targetX / chunkSize);
		int chunkZ = Mathf.FloorToInt(targetZ / chunkSize);

		Vector3Int closestMissingChunk = Vector3Int.zero;
		float closestDistance = this.Range;
		bool any = false;

		for (int x = Mathf.FloorToInt(chunkX - this.Range / chunkSize); x < chunkX + this.Range / chunkSize; x++) {
			for (int z = Mathf.FloorToInt(chunkZ - this.Range / chunkSize); z < chunkZ + this.Range / chunkSize; z++) {
				var chunk = new Vector3Int(x, 0, z);
				if (this.generatedChunks.Contains(chunk)) {
					continue;
				}
				var center = (chunk.ToVector3() + new Vector3(0.5f, 0f, 0.5f)) * chunkSize - new Vector3(1f, 0f, 1f) * InfiniteMap.BLOCK_SIZE / 2;
				float distance = Vector3.Distance(center, this.targetPosition + Vector3.down * this.targetPosition.y);

				if (distance < closestDistance) {
					closestMissingChunk = chunk;
					any = true;
					closestDistance = distance;
				}
			}
		}

		if (any) {
			this.createChunk(closestMissingChunk);
		}
	}

	private void createChunk(Vector3Int chunkAddress) {
		this.map.rangeLimitCenter = chunkAddress * this.ChunkSize + new Vector3Int(this.ChunkSize / 2, 0, this.ChunkSize / 2);
		this.map.RangeLimit = this.ChunkSize + 20;
		this.map.Collapse(chunkAddress * this.ChunkSize, new Vector3Int(this.ChunkSize, this.map.Height, this.ChunkSize));
		this.generatedChunks.Add(chunkAddress);
	}

	private void generatorThread() {
		try {
			while (true) {
				this.generate();
				Thread.Sleep(50);
			}
		}
		catch (Exception exception) {
			if (exception is System.Threading.ThreadAbortException) {
				return;
			}
			Debug.LogError(exception);
		}
		
	}

	private IEnumerable<Slot> getSlotsInChunk(Vector3Int chunkAddress) {
		for (int x = 0; x < this.ChunkSize; x++) {
			for (int y = 0; y < this.map.Height; y++) {
				for (int z = 0; z < this.ChunkSize; z++) {
					yield return this.map.GetSlot(chunkAddress * this.ChunkSize + new Vector3Int(x, y, z));
				}
			}
		}
	}
	
	void Update () {
		this.targetPosition = this.Target.position;
		this.mapPosition = this.mapBehaviour.transform.position;
	}
}
