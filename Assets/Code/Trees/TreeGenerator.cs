using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TreeGenerator : MonoBehaviour {
	private const int QUADS_PER_LEAF = 16;

	[Range(0.0f, 0.6f)]
	public float StemSize = 0.3f;

	[Range(0.0f, 0.1f)]
	public float BranchSize = 0.1f;

	[Range(0.1f, 4f)]
	public float SizeFalloff = 0.9f;

	[Range(0f, 0.5f)]
	public float Distort = 0.5f;

	[Range(0f, 1.2f)]
	public float BranchLength = 2f;

	[Range(0.1f, 0.5f)]
	public float LeafColliderSize = 0.2f;

	[Range(0f, 90f)]
	public float BranchAngle = 30f;

	[Range(0.9f, 1f)]
	public float BranchLengthFalloff = 0.9f;

	[Range(0.0f, 0.01f)]
	public float DepthPenalty = 0.0f;

	[Range(10, 1000)]
	public int Iterations = 100;

	public Material Material;

	public Material LeafMaterial;

	public Node Root;

	public float MinEnergy;
	public float MaxEnergy;

	public bool GenerateLeaves = true;

	[Range(0.2f, 1f)]
	public float LeafRadius = 0.3f;

	[HideInInspector]
	public int RayCastCount;

	private static float map(float inLower, float inUpper, float outLower, float outUpper, float value) {
		return outLower + (value - inLower) * (outUpper - outLower) / (inUpper - inLower);
	}


#if UNITY_EDITOR
	[DrawGizmo(GizmoType.Selected)]
	static void DrawGizmo(TreeGenerator tree, GizmoType gizmoType) {
		Gizmos.color = Color.green;
		if (tree.Root != null) {
			tree.Root.Draw();
		}
	}
#endif

	private void calculateEnergy() {
		foreach (var node in this.Root.GetTree()) {
			if (node.Children.Length == 2) {
				continue;
			}
			node.CalculateEnergy();
		}
		this.MinEnergy = this.Root.GetTree().Where(n => n.Children.Length < 2).Select(n => n.Energy).Min();
		this.MaxEnergy = this.Root.GetTree().Where(n => n.Children.Length < 2).Select(n => n.Energy).Max();
	}

	public static GUIStyle GUIStyle;
	
	public void Reset() {
		TreeGenerator.GUIStyle = new GUIStyle();
		TreeGenerator.GUIStyle.normal.textColor = Color.red;
		this.Age = 0;
		this.transform.DeleteChildren();
		this.Root = new Node(Vector3.zero, this);
		this.RayCastCount = 0;
		this.calculateEnergy();
	}

	public int Age = 0;

	public void Grow(int batchSize) {
		var nodes = this.Root.GetTree().ToArray();
		nodes = nodes.OrderByDescending(n => n.Energy).ToArray();

		foreach (var node in nodes) {
			node.Age++;
		}
		this.Age++;

		foreach (var node in nodes) {
			if (node.Children.Length == 0) {
				node.Grow();
				batchSize--;
			} else if (node.Children.Length < 3 && node.Depth > 1) {
				node.Branch();
				batchSize--;
			}
			if (batchSize < 0) {
				break;
			}
		}

		this.calculateEnergy();
	}

	public void Prune(float amount) {
		var nodes = this.Root.GetTree().Where(n => n.Children.Length == 0).ToArray();
		nodes = nodes.OrderByDescending(n => n.Energy).ToArray();

		for (int i = 0; i < nodes.Length * amount; i++) {
			if (nodes[i].Parent == null) {
				continue;
			}
			nodes[i].Parent.Children = nodes[i].Parent.Children.Where(n => n != nodes[i]).ToArray();
		}		
	}

	private void createBranch(Vector3 from, Vector3 to, float radius) {
		var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		gameObject.transform.parent = this.transform;
		gameObject.transform.position = 0.5f * (from + to);
		gameObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, (to - from));
		gameObject.transform.localScale = new Vector3(radius, Vector3.Distance(from, to) / 2f, radius);
		gameObject.GetComponent<MeshRenderer>().sharedMaterial = this.Material;
	}

	public int MeshSubdivisions = 5;

	[Range(1, 20)]
	public int BatchSize = 5;

	public void Build() {
		var iterator = this.BuildCoroutine();
		while (iterator.MoveNext());
	}

	public IEnumerator BuildCoroutine() {
		this.Reset();

		for (int i = 0; i < this.Iterations / this.BatchSize; i++) {
			this.Grow(this.BatchSize);
			yield return null;
		}

		this.Prune(0.2f);

		this.Root.CalculateSubtreeSize();

		this.GetComponent<MeshFilter>().sharedMesh = this.CreateMesh(this.MeshSubdivisions);
	}

	public Mesh CreateMesh(int subdivisions) {
		var nodes = this.Root.GetTree().ToArray();
		var leafNodes = nodes.Where(node => node.Children.Length == 0).ToArray();
		int edgeCount = nodes.Sum(node => node.Children.Length);
		int vertexCount = nodes.Length * subdivisions;

		var treeTriangles = new int[(edgeCount * 6 - leafNodes.Length * 3) * (subdivisions - 1)];
		int[] leafTriangles = null;
		if (this.GenerateLeaves) {
			vertexCount += leafNodes.Length * QUADS_PER_LEAF * 4;
			leafTriangles = new int[leafNodes.Length * QUADS_PER_LEAF * 6];
		}
		var vertices = new Vector3[vertexCount];
		var normals = new Vector3[vertexCount];
		var uvs = new Vector2[vertexCount];
		var indices = new Dictionary<Node, int>();

		int vertexIndex = 0;
		foreach (var node in nodes) {
			float radius = node.Children.Length == 0 ? 0 : map(0, 1, this.StemSize, this.BranchSize, Mathf.Pow(map(1, this.Root.SubtreeSize, 1, 0, node.SubtreeSize), this.SizeFalloff));
			indices[node] = vertexIndex;
			var direction = (node.Children.Any() && node.Parent != null) ? node.Children.Aggregate<Node, Vector3>(Vector3.zero, (v, n) => v + n.Direction).normalized : node.Direction;
			if (node.Parent == null) {
				node.MeshOrientation = Vector3.Cross(Vector3.forward, direction);
			} else {
				node.MeshOrientation = (node.Parent.MeshOrientation - direction * Vector3.Dot(direction, node.Parent.MeshOrientation)).normalized;
			}
			for (int i = 0; i < subdivisions; i++) {
				float progress = (float)i / (subdivisions - 1);
				var normal = Quaternion.AngleAxis(360f * progress, direction) * node.MeshOrientation;
				normal.Normalize();
				normals[vertexIndex] = normal;
				float offset = 0;
				if (node.Depth < 4) {
					offset = Mathf.Pow(Mathf.Abs(Mathf.Sin(progress * 2f * Mathf.PI * 5f)), 0.5f) * 0.5f * (3 - node.Depth) / 3f;
				}
				vertices[vertexIndex] = node.Position + normal * radius * (1f + offset);
				uvs[vertexIndex] = new Vector2(progress * 6f, (node.Depth % 2) * 3f);
				vertexIndex++;
			}
		}

		int triangleIndex = 0;
		foreach (var node in nodes) {
			int nodeIndex = indices[node];

			foreach (var child in node.Children) {
				int childIndex = indices[child];

				for (int i = 0; i < subdivisions - 1; i++) {
					treeTriangles[triangleIndex++] = nodeIndex + i;
					treeTriangles[triangleIndex++] = nodeIndex + i + 1;
					treeTriangles[triangleIndex++] = childIndex + i;
				}

				if (child.Children.Length != 0) {
					for (int i = 0; i < subdivisions - 1; i++) {
						treeTriangles[triangleIndex++] = nodeIndex + i + 1;
						treeTriangles[triangleIndex++] = childIndex + i + 1;
						treeTriangles[triangleIndex++] = childIndex + i;
					}
				}
			}
		}
		triangleIndex = 0;

		if (this.GenerateLeaves) {
			var leafDirections = new Vector3[QUADS_PER_LEAF];
			var tangents1 = new Vector3[QUADS_PER_LEAF];
			var tangents2 = new Vector3[QUADS_PER_LEAF];
			float increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

			for (int i = 0; i < QUADS_PER_LEAF; i++) {
				float y = ((i * 2f / QUADS_PER_LEAF) - 1) + (1f / QUADS_PER_LEAF);
				float r = Mathf.Sqrt(1 - Mathf.Pow(y, 2f));
				float phi = (i + 1f) * increment;
				leafDirections[i] = new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
			}
			for (int i = 0; i < QUADS_PER_LEAF; i++) {
				tangents1[i] = Vector3.Cross(leafDirections[i], leafDirections[(i + 1) % QUADS_PER_LEAF]);
				tangents2[i] = Vector3.Cross(leafDirections[i], tangents1[i]);
			}

			foreach (var node in leafNodes) {
				var orientation = Quaternion.LookRotation(Random.onUnitSphere, Random.onUnitSphere);
				for (int i = 0; i < QUADS_PER_LEAF; i++) {
					var normal = orientation * leafDirections[i];
					var tangent1 = orientation * tangents1[i];
					var tangent2 = orientation * tangents2[i];

					vertices[vertexIndex + 0] = node.Position + tangent1 * this.LeafRadius;
					vertices[vertexIndex + 1] = node.Position + tangent2 * this.LeafRadius;
					vertices[vertexIndex + 2] = node.Position - tangent1 * this.LeafRadius;
					vertices[vertexIndex + 3] = node.Position - tangent2 * this.LeafRadius;
					normals[vertexIndex + 0] = normal;
					normals[vertexIndex + 1] = normal;
					normals[vertexIndex + 2] = normal;
					normals[vertexIndex + 3] = normal;
					uvs[vertexIndex + 0] = new Vector2(0f, 1f);
					uvs[vertexIndex + 1] = new Vector2(1f, 1f);
					uvs[vertexIndex + 2] = new Vector2(1f, 0f);
					uvs[vertexIndex + 3] = new Vector2(0f, 0f);
					leafTriangles[triangleIndex++] = vertexIndex + 0;
					leafTriangles[triangleIndex++] = vertexIndex + 1;
					leafTriangles[triangleIndex++] = vertexIndex + 2;
					leafTriangles[triangleIndex++] = vertexIndex + 2;
					leafTriangles[triangleIndex++] = vertexIndex + 3;
					leafTriangles[triangleIndex++] = vertexIndex + 0;
					vertexIndex += 4;
				}
			}
		}

		Debug.Log("Generated tree. " + this.RayCastCount + " ray casts, " + treeTriangles.Length + " tree triangles, " + (this.GenerateLeaves ? leafTriangles.Length : 0) + " leaf triangles.");
		
		var mesh = new Mesh();
		mesh.subMeshCount = 2;
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.SetTriangles(treeTriangles, 0);
		if (this.GenerateLeaves) {
			mesh.SetTriangles(leafTriangles, 1);
		}
		return mesh;
	}
}
