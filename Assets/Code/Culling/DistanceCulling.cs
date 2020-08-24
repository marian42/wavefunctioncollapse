using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CullingData))]
public class DistanceCulling : MonoBehaviour {

	private CullingData cullingData;

	public Camera Camera;

	public float Range = 40;

	private Vector3Int chunkAddress;

	public void OnEnable() {
		this.cullingData = this.GetComponent<CullingData>();
		if (this.cullingData.ChunksInRange != null) {
			this.chunkAddress = this.cullingData.GetChunkAddress(this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position));
			this.UpdateChunks();
		}
	}

	public void OnDisable() {
		this.cullingData.ChunksInRange = this.cullingData.Chunks.Values.ToList();
		foreach (var chunk in this.cullingData.ChunksInRange) {
			chunk.SetInRenderRange(true);
		}
	}

	void Update() {
		var newChunkAddress = this.cullingData.GetChunkAddress(this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position));
		if (newChunkAddress == this.chunkAddress) {
			return;
		}
		this.chunkAddress = newChunkAddress;
		this.UpdateChunks();
	}

	float getHorizontalDistance(Vector3 a, Vector3 b) {
		return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.z - b.z, 2));
	}

	void UpdateChunks() {
		var chunksInRange = this.cullingData.ChunksInRange;
		for (int i = 0; i < chunksInRange.Count; i++) {
			if (getHorizontalDistance(chunksInRange[i].Bounds.center, this.Camera.transform.position) > this.Range) {
				chunksInRange[i].SetInRenderRange(false);
				chunksInRange.RemoveAt(i);
				i--;
			}
		}

		int chunkCount = (int)(this.Range / (AbstractMap.BLOCK_SIZE * this.cullingData.ChunkSize));
		for (int x = this.chunkAddress.x - chunkCount; x <= this.chunkAddress.x + chunkCount; x++) {
			for (int y = 0; y < Mathf.CeilToInt((float)this.cullingData.MapBehaviour.MapHeight / this.cullingData.ChunkSize); y++) {
				for (int z = this.chunkAddress.z - chunkCount; z <= this.chunkAddress.z + chunkCount; z++) {
					var address = new Vector3Int(x, y, z);
					if (Vector3.Distance(this.Camera.transform.position, this.cullingData.GetChunkCenter(address)) > this.Range) {
						continue;
					}
					if (!this.cullingData.Chunks.ContainsKey(address)) {
						continue;
					}
					var chunk = this.cullingData.Chunks[address];
					if (chunk.InRenderRange) {
						continue;
					}
					chunk.SetInRenderRange(true);
					chunksInRange.Add(chunk);
				}
			}
		}
	}
}
