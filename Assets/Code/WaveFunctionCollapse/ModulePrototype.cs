using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

public class ModulePrototype : MonoBehaviour {
	[System.Serializable]
	public abstract class FaceDetails {
		public bool Walkable;

		public int Connector;

		[HideInInspector]
		public Fingerprint Fingerprint;

		public virtual void ResetConnector() {
			this.Connector = 0;
		}

		public ModulePrototype[] ExcludedNeighbours;

		public bool EnforceWalkableNeighbor = false;

		public bool IsOcclusionPortal = false;
	}

	[System.Serializable]
	public class HorizontalFaceDetails : FaceDetails {
		public bool Symmetric;
		public bool Flipped;

		public override string ToString() {
			return this.Connector.ToString() + (this.Symmetric ? "s" : (this.Flipped ? "F" : ""));
		}

		public override void ResetConnector() {
			base.ResetConnector();
			this.Symmetric = false;
			this.Flipped = false;
		}
	}

	[System.Serializable]
	public class VerticalFaceDetails : FaceDetails {
		public bool Invariant;
		public int Rotation;

		public override string ToString() {
			return this.Connector.ToString() + (this.Invariant ? "i" : (this.Rotation != 0 ? "_bcd".ElementAt(this.Rotation).ToString() : ""));
		}

		public override void ResetConnector() {
			base.ResetConnector();
			this.Invariant = false;
			this.Rotation = 0;
		}
	}

	public float Probability = 1.0f;
	public bool Spawn = true;
	public bool IsInterior = false;

	public HorizontalFaceDetails Left;
	public VerticalFaceDetails Down;
	public HorizontalFaceDetails Back;
	public HorizontalFaceDetails Right;
	public VerticalFaceDetails Up;
	public HorizontalFaceDetails Forward;

	public FaceDetails[] Faces {
		get {
			return new FaceDetails[] {
				this.Left,
				this.Down,
				this.Back,
				this.Right,
				this.Up,
				this.Forward
			};
		}
	}

	public Mesh GetMesh(bool createEmptyFallbackMesh = true) {
		var meshFilter = this.GetComponent<MeshFilter>();
		if (meshFilter != null && meshFilter.sharedMesh != null) {
			return meshFilter.sharedMesh;
		}
		if (createEmptyFallbackMesh) {
			var mesh = new Mesh();
			return mesh;
		}
		return null;
	}
	
	
#if UNITY_EDITOR
	private static ModulePrototypeEditorData editorData;
	private static GUIStyle style;

	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmo(ModulePrototype modulePrototype, GizmoType gizmoType) {
		var transform = modulePrototype.transform;
		Vector3 position = transform.position;
		var rotation = transform.rotation;

		if (ModulePrototype.editorData == null || ModulePrototype.editorData.ModulePrototype != modulePrototype) {
			ModulePrototype.editorData = new ModulePrototypeEditorData(modulePrototype);
		}

		Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
		if ((gizmoType & GizmoType.Selected) != 0) {
			for (int i = 0; i < 6; i++) {
				var hint = ModulePrototype.editorData.GetConnectorHint(i);
				if (hint.Mesh != null) {
					Gizmos.DrawMesh(hint.Mesh,
						position + rotation * Orientations.Direction[i].ToVector3() * 2f,
						rotation * Quaternion.Euler(Vector3.up * 90f * hint.Rotation));
				}
			}
		}
		for (int i = 0; i < 6; i++) {	
			if (modulePrototype.Faces[i].Walkable) {
				Gizmos.color = Color.red;
				Gizmos.DrawLine(position + Vector3.down * 0.1f, position + rotation * Orientations.Rotations[i] * Vector3.forward + Vector3.down * 0.1f);
			}
			if (modulePrototype.Faces[i].IsOcclusionPortal) {
				Gizmos.color = Color.blue;

				var dir = rotation * Orientations.Rotations[i] * Vector3.forward;
				Gizmos.DrawWireCube(position + dir, (Vector3.one - new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z))) * 2f);
			}			
		}

		if (ModulePrototype.style == null) {
			ModulePrototype.style = new GUIStyle();
			ModulePrototype.style.alignment = TextAnchor.MiddleCenter;
		}

		ModulePrototype.style.normal.textColor = Color.black;
		for (int i = 0; i < 6; i++) {
			var face = modulePrototype.Faces[i];
			Handles.Label(position + rotation * Orientations.Rotations[i] * Vector3.forward * InfiniteMap.BLOCK_SIZE / 2f, face.ToString(), ModulePrototype.style);
		}
	}
#endif

	public void EnsureFingerprints() {
		var faces = this.Faces;
		if (faces[0].Fingerprint != null) {
			return;
		}
		
		var mesh = this.GetMesh();

		for (int i = 0; i < 6; i++) {
			faces[i].Fingerprint = new Fingerprint(mesh, Orientations.Rotations[i], i == 4);
		}
	}

	public bool ConnectorsSet {
		get {
			return this.Faces.Any(face => face.Connector != 0);
		}
	}

	public void GuessConnectors() {
		var fingerprints = new Dictionary<int, Fingerprint>();

		foreach (var modulePrototype in this.transform.parent.GetComponentsInChildren<ModulePrototype>()) {
			if (modulePrototype == this || !modulePrototype.ConnectorsSet) {
				continue;
			}
			modulePrototype.EnsureFingerprints();
			for (int direction = 0; direction < 6; direction++) {
				var face = modulePrototype.Faces[direction];
				if (!fingerprints.ContainsKey(face.Connector)) {
					fingerprints[face.Connector] = face.Fingerprint;
				}
			}
		}

		this.EnsureFingerprints();

		for (int i = 0; i < 6; i++) {
			bool found = false;
			var face = this.Faces[i];

			if (face is HorizontalFaceDetails) {
				var hface = face as HorizontalFaceDetails;
				foreach (var connector in fingerprints.Keys) {
					if (fingerprints[connector].Symmetric != hface.Fingerprint.Symmetric) {
						continue;
					}
					if (Fingerprint.Compare(fingerprints[connector].Base, hface.Fingerprint.Base)) {
						found = true;
						hface.Connector = connector;
						hface.Symmetric = fingerprints[connector].Symmetric;
						hface.Flipped = false;
						break;
					}
					if (!fingerprints[connector].Symmetric && Fingerprint.Compare(fingerprints[connector].Base, hface.Fingerprint.Flipped)) {
						found = true;
						hface.Connector = connector;
						hface.Symmetric = false;
						hface.Flipped = true;
						break;
					}
				}
				if (!found) {
					hface.Connector = getNewConnector(fingerprints);
					hface.Flipped = false;
					hface.Symmetric = hface.Fingerprint.Symmetric;
					fingerprints[hface.Connector] = face.Fingerprint;
				}
			}

			if (face is VerticalFaceDetails) {
				var vface = face as VerticalFaceDetails;
				foreach (var connector in fingerprints.Keys) {
					if (fingerprints[connector].Invariant != vface.Fingerprint.Invariant) {
						continue;
					}
					for (int r = 0; r < (vface.Fingerprint.Invariant ? 1 : 4); r++) {
						if (Fingerprint.Compare(fingerprints[connector].Rotated[r], vface.Fingerprint.Base)) {
							found = true;
							vface.Connector = connector;
							vface.Invariant = vface.Fingerprint.Invariant;
							vface.Rotation = r;
							break;
						}
					}
				}
				if (!found) {
					vface.Connector = getNewConnector(fingerprints);
					vface.Rotation = 0;
					vface.Invariant = vface.Fingerprint.Invariant;
					fingerprints[vface.Connector] = vface.Fingerprint;
				}
			}			
		}
	}

	private int getNewConnector(Dictionary<int, Fingerprint> dict) {
		int result = 0;
		while (dict.ContainsKey(result)) result++;
		return result;
	}

	public bool CompareRotatedVariants(int r1, int r2) {
		if (!(this.Faces[Orientations.UP] as VerticalFaceDetails).Invariant || !(this.Faces[Orientations.DOWN] as VerticalFaceDetails).Invariant) {
			return false;
		}

		for (int i = 0; i < 4; i++) {
			var face1 = this.Faces[Orientations.Rotate(Orientations.HorizontalDirections[i], r1)] as HorizontalFaceDetails;
			var face2 = this.Faces[Orientations.Rotate(Orientations.HorizontalDirections[i], r2)] as HorizontalFaceDetails;

			if (face1.Connector != face2.Connector) {
				return false;
			}

			if (!face1.Symmetric && !face2.Symmetric && face1.Flipped != face2.Flipped) {
				return false;
			}
		}

		return true;
	}

	void Update() { }

	void Reset() {
		this.Up = new VerticalFaceDetails();
		this.Down = new VerticalFaceDetails();
		this.Right = new HorizontalFaceDetails();
		this.Left = new HorizontalFaceDetails();
		this.Forward = new HorizontalFaceDetails();
		this.Back = new HorizontalFaceDetails();

		foreach (var face in this.Faces) {
			face.ExcludedNeighbours = new ModulePrototype[] { };
		}
	}
}
