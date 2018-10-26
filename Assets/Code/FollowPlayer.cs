using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MapGenerator))]
public class FollowPlayer : MonoBehaviour {

	private MapGenerator mapGenerator;

	public Transform Target;

	public int ChunkSize = 4;

	public float Range;

	private HashSet<Vector3i> completedChunks;

	private void initializeMap() {
		int initTries = 10;
		while (initTries-- > 0) {
			try {
				this.mapGenerator.Initialize();
				break;
			}
			catch (System.Exception e) {
				Debug.LogWarning(e);
			}
		}
	}

	void Start () {
		this.mapGenerator = this.GetComponent<MapGenerator>();
		this.initializeMap();
		this.completedChunks = new HashSet<Vector3i>();
	}

	private void generate() {
		float chunkSize = MapGenerator.BlockSize * this.ChunkSize;

		float targetX = this.Target.position.x - this.mapGenerator.transform.position.x + MapGenerator.BlockSize / 2;
		float targetZ = this.Target.position.z - this.mapGenerator.transform.position.z + MapGenerator.BlockSize / 2;

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
				float distance = Vector3.Distance(center, this.Target.position + Vector3.down * this.Target.position.y);

				if (distance < closestDistance) {
					closestMissingChunk = chunk;
					any = true;
					closestDistance = distance;
				}
			}
		}

		if (any) {
			this.completedChunks.Add(closestMissingChunk);
			try {
				this.mapGenerator.Collapse(closestMissingChunk * this.ChunkSize, new Vector3i(this.ChunkSize, this.mapGenerator.Height, this.ChunkSize));
			}
			catch (Exception e) {
				Debug.LogWarning(e);
			}
		}
	}
	
	void Update () {
		this.generate();
	}
}
