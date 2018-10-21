using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour {
	private Dictionary<int, Dictionary<int, Chunk>> chunks;

	public Chunk GetChunk(int x, int z) {
		if (this.chunks == null) {
			foreach (var chunk in GameObject.FindObjectsOfType<Chunk>()) {
				this.AddChunk(chunk);
			}
		}
		if (!this.chunks.ContainsKey(x) || !this.chunks[x].ContainsKey(z)) {
			return null;
		} else {
			return this.chunks[x][z];
		}
	}

	public void AddChunk(Chunk chunk) {
		if (chunks == null) {
			chunks = new Dictionary<int, Dictionary<int, Chunk>>();
		}
		if (!chunks.ContainsKey(chunk.X)) {
			chunks[chunk.X] = new Dictionary<int, Chunk>();
		}
		chunks[chunk.X][chunk.Z] = chunk;
	}
}
