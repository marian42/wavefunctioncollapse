using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(MapGenerator))]
public class FollowPlayer : MonoBehaviour {

	private MapGenerator mapGenerator;

	public Transform Target;

	public int ChunkSize = 4;

	public float Range;

	private HashSet<Vector3i> completedChunks;

	private Vector3 targetPosition;
	private Vector3 mapPosition;

	void Start() {
		this.completedChunks = new HashSet<Vector3i>();
		this.mapGenerator = this.GetComponent<MapGenerator>();
		this.mapGenerator.Initialize();
		this.generate();
		this.mapGenerator.BuildAllSlots();

		new Thread(this.generatorThread).Start();
	}

	private void generate() {
		float chunkSize = MapGenerator.BlockSize * this.ChunkSize;

		float targetX = this.targetPosition.x - this.mapPosition.x + MapGenerator.BlockSize / 2;
		float targetZ = this.targetPosition.z - this.mapPosition.z + MapGenerator.BlockSize / 2;

		int chunkX = Mathf.FloorToInt(targetX / chunkSize);
		int chunkZ = Mathf.FloorToInt(targetZ / chunkSize);

		Vector3i closestMissingChunk = Vector3i.zero;
		float closestDistance = this.Range;
		bool any = false;

		for (int x = Mathf.FloorToInt(chunkX - this.Range / chunkSize); x < chunkX + this.Range / chunkSize; x++) {
			for (int z = Mathf.FloorToInt(chunkZ - this.Range / chunkSize); z < chunkZ + this.Range / chunkSize; z++) {
				var chunk = new Vector3i(x, 0, z);
				if (this.completedChunks.Contains(chunk)) {
					continue;
				}
				var center = (chunk.ToVector3() + new Vector3(0.5f, 0f, 0.5f)) * chunkSize - new Vector3(1f, 0f, 1f) * MapGenerator.BlockSize / 2;
				float distance = Vector3.Distance(center, this.targetPosition + Vector3.down * this.targetPosition.y);

				if (distance < closestDistance) {
					closestMissingChunk = chunk;
					any = true;
					closestDistance = distance;
				}
			}
		}

		if (any) {
			this.completedChunks.Add(closestMissingChunk);
			this.mapGenerator.RangeLimitCenter = closestMissingChunk * this.ChunkSize + new Vector3i(this.ChunkSize / 2, 0, this.ChunkSize / 2);
			this.mapGenerator.RangeLimit = this.ChunkSize + 12;
			this.mapGenerator.Collapse(closestMissingChunk * this.ChunkSize, new Vector3i(this.ChunkSize, this.mapGenerator.Height, this.ChunkSize));
		} else {
			Thread.Sleep(80);
		}
	}

	private void generatorThread() {
		try {
			while (true) {
				this.generate();
			}
		}
		catch (Exception e) {
			Debug.LogError(e);
		}
		
	}
	
	void Update () {
		this.targetPosition = this.Target.position;
		this.mapPosition = this.mapGenerator.transform.position;
	}
}
