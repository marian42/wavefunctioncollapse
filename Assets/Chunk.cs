using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class Chunk : MonoBehaviour {
	[System.Serializable]
	public class WalkConnection {
		public Vector3i A;
		public Vector3i B;

		public WalkConnection(Vector3i a, Vector3i b) {
			this.A = a;
			this.B = b;
		}
	}

	public int X;
	public int Z;

	public int Size = 8;

	public Chunk[] Neighbors;

	public Vector3i[] Connectors;

	public Vector3i Center;

	public List<WalkConnection> Walkways;

	public MapGenerator MapGenerator;

	public World World;

	public void Initialize() {
		if (this.World == null) {
			this.World = this.transform.parent.GetComponent<World>();
		}
		this.World.AddChunk(this);

		this.Neighbors = new Chunk[4];
		for (int i = 0; i < 4; i++) {
			int x = this.X;
			int z = this.Z;

			switch (i) {
				case 0: x++; break;
				case 1: z++; break;
				case 2: x--; break;
				case 3: z--; break;
				default: throw new System.NotImplementedException();
			}
			
			this.Neighbors[i] = this.World.GetChunk(x, z);
			if (this.Neighbors[i] != null) {
				this.Neighbors[i].Neighbors[(i + 2) % 4] = this;
			}
		}

		this.transform.position = new Vector3(this.X * this.Size * MapGenerator.BlockSize, 0, this.Z * this.Size * MapGenerator.BlockSize);
		this.gameObject.name = "Chunk (" + this.X + ", " + this.Z + ")";

		//this.Center = new Vector3i(Random.Range(1, this.Size - 2), Random.Range(1, this.Size - 2), Random.Range(1, this.Size - 2));
		this.Center = new Vector3i(Random.Range(2, this.Size - 3), this.Size / 2, Random.Range(2, this.Size - 3));
		// TODO improve variation of center points
		this.Connectors = new Vector3i[4];
		this.Walkways = new List<WalkConnection>();

		for (int i = 0; i < 4; i++) {
			if (this.Neighbors[i] != null) {
				this.Connectors[i] = this.Neighbors[i].Connectors[(i + 2) % 4] + (this.Size - 1) * getDirection(i);
			} else {
				int vertical = this.Size / 2; //Random.Range(1, this.Size - 2);
				// TODO improve variation of connector positions
				int horizontal = Random.Range(1, this.Size - 2);
				switch (i) {
					case 0: this.Connectors[i] = new Vector3i(this.Size - 1, vertical, horizontal); break;
					case 1: this.Connectors[i] = new Vector3i(horizontal, vertical, this.Size - 1); break;
					case 2: this.Connectors[i] = new Vector3i(0, vertical, horizontal); break;
					case 3: this.Connectors[i] = new Vector3i(horizontal, vertical, 0); break;
				}
			}
			var step1 = this.Center + getDirection(i);
			this.Walkways.Add(new WalkConnection(this.Center, step1));
			this.createPath(step1, this.Connectors[i]);
		}

		this.MapGenerator = this.GetComponent<MapGenerator>();
		if (this.MapGenerator == null) {
			this.MapGenerator = this.gameObject.AddComponent<MapGenerator>();
			this.MapGenerator.AllowExclusions = false;
		}
		this.MapGenerator.MapSize = Vector3.one * this.Size;
		this.MapGenerator.UpConnector = 0;
		this.MapGenerator.DownConnector = 1;
	}

	public void ExcludeModules(MapGenerator generator) {
		foreach (var way in this.Walkways) {
			generator.EnforceWalkway(way.A, way.B);
		}
		generator.EnforceWalkway(this.Connectors[0], 3);
		generator.EnforceWalkway(this.Connectors[1], 5);
		generator.EnforceWalkway(this.Connectors[2], 0);
		generator.EnforceWalkway(this.Connectors[3], 2);

		for (int direction = 0; direction < 4; direction++) {
			if (this.Neighbors[direction] == null) {
				continue;
			}
			var adjacentMapGenerator = this.Neighbors[direction].MapGenerator;

			int direction3D = Orientations.Get(getDirection(direction).ToVector3());
			Vector3i start;
			Vector3i horizontal;
			var vertical = new Vector3i(0, 1, 0);

			switch (direction) {
				case 0:
					start = new Vector3i(this.Size - 1, 0, 0);
					horizontal = new Vector3i(0, 0, 1);
					break;
				case 1:
					start = new Vector3i(this.Size - 1, 0, this.Size - 1);
					horizontal = new Vector3i(-1, 0, 0);
					break;
				case 2:
					start = new Vector3i(0, 0, this.Size - 1);
					horizontal = new Vector3i(0, 0, -1);
					break;
				case 3:
					start = new Vector3i(0, 0, 0);
					horizontal = new Vector3i(1, 0, 0);
					break;
				default: throw new System.NotImplementedException();
			}

			if (adjacentMapGenerator.SlotsFilled != this.Size * this.Size * this.Size) {
				continue;
			}

			for (int a = 0; a < this.Size; a++) {
				for (int b = 0; b < this.Size; b++) {
					var index = start + a * horizontal + b * vertical;
					var cell = this.MapGenerator.Map[index.X, index.Y, index.Z];
					var adjacentIndex = index - (this.Size - 1) * getDirection(direction);
					var adjacentCell = adjacentMapGenerator.Map[adjacentIndex.X, adjacentIndex.Y, adjacentIndex.Z];
					var toRemove = cell.Modules.Where(i => !this.MapGenerator.Modules[i].Fits(direction3D, adjacentCell.Module)).ToList();
					cell.RemoveModules(toRemove);
				}
			}
		}
	}

	private void createPath(Vector3i source, Vector3i destination) {
		var current = source;
		int i = 0;
		while (current != destination) {
			var dst = destination - current;
			var dstAbs = new Vector3i(System.Math.Abs(dst.X), System.Math.Abs(dst.Y), System.Math.Abs(dst.Z));
			Vector3i next;
			if (dstAbs.X >= dstAbs.Y && dstAbs.X >= dstAbs.Z) {
				next = current + new Vector3i(System.Math.Sign(dst.X), 0, 0);
			} else if (dstAbs.Y >= dstAbs.X && dstAbs.Y >= dstAbs.Z) {
				next = current + new Vector3i(0, System.Math.Sign(dst.Y), 0);
			} else {
				next = current + new Vector3i(0, 0, System.Math.Sign(dst.Z));
			}
			this.Walkways.Add(new WalkConnection(current, next));
			current = next;
			i++;
			if (i > 100) {
				throw new System.Exception();
			}
		}
	}

	private static Vector3i getDirection(int direction) {
		switch (direction) {
			case 0: return new Vector3i(1, 0, 0);
			case 1: return new Vector3i(0, 0, 1);
			case 2: return new Vector3i(-1, 0, 0);
			case 3: return new Vector3i(0, 0, -1);
			default: throw new System.NotImplementedException();
		}
	}

	public void Expand(int direction) {
		if (this.Neighbors[direction] != null) {
			return;
		}

		var gameObject = new GameObject();
		var chunk = gameObject.AddComponent<Chunk>();
		chunk.X = this.X;
		chunk.Z = this.Z;
		switch (direction) {
			case 0: chunk.X++; break;
			case 1: chunk.Z++; break;
			case 2: chunk.X--; break;
			case 3: chunk.Z--; break;
			default: throw new System.NotImplementedException();
		}
		chunk.World = this.World;
		chunk.transform.parent = this.World.transform;
		chunk.Initialize();
	}

	[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawGizmoForMyScript(Chunk chunk, GizmoType gizmoType) {
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(chunk.transform.position + chunk.Center.ToVector3() * MapGenerator.BlockSize + Vector3.up * 0.5f * MapGenerator.BlockSize, 0.5f);

		foreach (var way in chunk.Walkways) {
			Gizmos.DrawLine(chunk.transform.position + way.A.ToVector3() * MapGenerator.BlockSize + Vector3.up * 0.5f * MapGenerator.BlockSize, chunk.transform.position + way.B.ToVector3() * MapGenerator.BlockSize + Vector3.up * 0.5f * MapGenerator.BlockSize);
		}
	}
}
