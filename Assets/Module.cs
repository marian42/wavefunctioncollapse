using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[System.Serializable]
public class Module {
	public readonly Quaternion Orientation;

	public readonly Mesh mesh;

	public Fingerprint[] Fingerprints;

	public int[] Connectors;	

	public Module(Mesh mesh, Quaternion orientation, MapGenerator mapGenerator) {
		this.mesh = mesh;
		this.Orientation = orientation;

		//this.Fingerprints = Enumerable.Range(0, 6).Select(i => this.getFingerprint(i)).ToArray();
		this.Connectors = this.Fingerprints.Select(fingerprint => mapGenerator.GetConnector(fingerprint)).ToArray();
	}

	public static Module CreateEmpty(MapGenerator mapGenerator) {
		var mesh = new Mesh();
		return new Module(mesh, Quaternion.identity, mapGenerator);
	}
}
