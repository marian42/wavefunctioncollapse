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

	[Range(10, 1000)]
	public int Iterations = 100;

	public Node Root;

	public bool GenerateLeaves = true;

	[Range(0.2f, 1f)]
	public float LeafQuadRadius = 0.3f;

	[HideInInspector]
	public int RayCastCount;

	public int MaxChildrenPerNode = 3;

	public int MeshSubdivisions = 5;

	[Range(1, 20)]
	public int BatchSize = 5;

	[HideInInspector]
	public GameObject LeafColliders;

	public ColorGenerator[] ColorSchemes;

	private static float map(float inLower, float inUpper, float outLower, float outUpper, float value) {
		return outLower + (value - inLower) * (outUpper - outLower) / (inUpper - inLower);
	}
	
	private void calculateEnergy() {
		foreach (var node in this.Root.GetTree()) {
			if (node.Children.Length >= MaxChildrenPerNode) {
				continue;
			}
			node.CalculateEnergy();
		}
	}
	
	public void Reset() {
		this.transform.DeleteChildren();
		this.Root = new Node(Vector3.zero, this);
		this.RayCastCount = 0;
		this.calculateEnergy();

		this.LeafColliders = new GameObject();
		this.LeafColliders.name = "Leaf Colliders";
		this.LeafColliders.transform.SetParent(this.transform);
		this.LeafColliders.transform.localPosition = Vector3.zero;
	}

	public void Grow(int batchSize) {
		var nodes = this.Root.GetTree().OrderByDescending(n => n.Energy).ToArray();

		int remainingOperations = batchSize;
		foreach (var node in nodes) {
			if (node.Children.Length == 0) {
				node.Grow();
				remainingOperations--;
			} else if (node.Children.Length < this.MaxChildrenPerNode && node.Depth > 1) {
				node.Branch();
				remainingOperations--;
			}
			if (remainingOperations == 0) {
				break;
			}
		}

		this.calculateEnergy();
	}

	public void Prune(float amount) {
		var nodes = this.Root.GetTree().Where(n => n.Children.Length == 0).OrderByDescending(n => n.Energy).ToArray();

		for (int i = 0; i < nodes.Length * amount; i++) {
			if (nodes[i].Parent == null) {
				continue;
			}
			nodes[i].RemoveLeafCollider();
			nodes[i].Parent.Children = nodes[i].Parent.Children.Where(n => n != nodes[i]).ToArray();
		}		
	}

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
		this.GetComponent<MeshFilter>().sharedMesh = this.CreateMesh(this.MeshSubdivisions);
		this.createBranchColliders();
		this.LeafColliders.layer = 9;
		if (this.GenerateLeaves) {
			this.generateColor();
		}
	}

	private float getBranchRadius(Node node) {
		return node.Children.Length == 0 ? 0 : map(0, 1, this.StemSize, this.BranchSize, Mathf.Pow(map(1, this.Root.SubtreeSize, 1, 0, node.SubtreeSize), this.SizeFalloff));
	}

	public Mesh CreateMesh(int subdivisions) {
		this.Root.CalculateSubtreeSize();
		var nodes = this.Root.GetTree().ToArray();
		var leafNodes = nodes.Where(node => node.Children.Length == 0).ToArray();
		int edgeCount = nodes.Sum(node => node.Children.Length);
		int vertexCount = nodes.Length * subdivisions;

		if (edgeCount == 0) {
			return null;
		}

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
				vertices[vertexIndex] = node.Position + normal * this.getBranchRadius(node) * (1f + offset);
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

					vertices[vertexIndex + 0] = node.Position + tangent1 * this.LeafQuadRadius;
					vertices[vertexIndex + 1] = node.Position + tangent2 * this.LeafQuadRadius;
					vertices[vertexIndex + 2] = node.Position - tangent1 * this.LeafQuadRadius;
					vertices[vertexIndex + 3] = node.Position - tangent2 * this.LeafQuadRadius;
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

	private void createBranchColliders() {
		foreach (var node in this.Root.GetTree()) {
			if (node.Parent == null || node.Depth > 6) {
				continue;
			}
			var position1 = node.Parent.Position;
			var position2 = node.Position;

			var container = new GameObject();
			container.name = "Branch Collider";
			container.transform.SetParent(this.transform);
			container.transform.localPosition = (position1 + position2) * 0.5f;
			container.transform.localRotation = Quaternion.LookRotation(position2 - position1);

			var collider = container.AddComponent<CapsuleCollider>();
			float radius = this.getBranchRadius(node.Parent);
			collider.radius = radius;
			collider.height = (position2 - position1).magnitude + radius * 2f;
			collider.direction = 2;
		}
	}

	private void generateColor() {
		float totalProbability = this.ColorSchemes.Sum(s => s.Probability);
		float roll = Random.Range(0f, totalProbability);
		foreach (var item in this.ColorSchemes) {
			if (item.Probability > roll) {
				this.GetComponent<MeshRenderer>().materials[1].color = item.GetColor();
				return;
			} else {
				roll -= item.Probability;
			}
		}
	}
}
