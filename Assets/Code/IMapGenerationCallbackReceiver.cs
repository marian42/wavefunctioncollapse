using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMapGenerationCallbackReceiver {
	void OnGenerateChunk(Vector3Int chunkAddress, GenerateMapNearPlayer source);
}
