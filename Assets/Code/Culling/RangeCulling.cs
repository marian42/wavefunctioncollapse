using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CullingData))]
public class RangeCulling : MonoBehaviour {

	private CullingData cullingData;

	public Camera Camera;

	public float Range = 40;

	private Vector3Int previousChunkAddress;

	public void OnEnable() {
		this.cullingData = this.GetComponent<CullingData>();
	}

	public void OnDisable() {
		this.cullingData.ChunksInRange = this.cullingData.Chunks.Values.ToList();
		foreach (var chunk in this.cullingData.ChunksInRange) {
			chunk.SetInRenderRange(true);
		}
	}

	void Update() {
		var chunksInRange = this.cullingData.ChunksInRange;
		var cameraPosition = this.Camera.transform.position;
		for (int i = 0; i < chunksInRange.Count; i++) {
			if (Vector3.Distance(chunksInRange[i].Bounds.center, cameraPosition) > this.Range) {
				chunksInRange[i].SetInRenderRange(false);
				chunksInRange.RemoveAt(i);
				i--;
			}
		}

		var currentChunkAddress = this.cullingData.GetChunkAddress(this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position));
		if (currentChunkAddress == this.previousChunkAddress) {
			return;
		}
		this.previousChunkAddress = currentChunkAddress;

		int chunkCount = (int)(this.Range / (AbstractMap.BLOCK_SIZE * this.Range));
		for (int x = currentChunkAddress.x - chunkCount; x <= currentChunkAddress.x + chunkCount; x++) {
			for (int y = 0; y < Mathf.CeilToInt((float)this.cullingData.MapBehaviour.MapHeight / this.cullingData.ChunkSize); y++) {
				for (int z = currentChunkAddress.z - chunkCount; z <= currentChunkAddress.z + chunkCount; z++) {
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
