using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

public class Node {
	public readonly Vector3 Position;
	public readonly Vector3 Direction;
	public readonly TreeGenerator Tree;

	public readonly int Depth;
	public float Energy;
	
	public readonly Node Parent;
	public Node[] Children;

	public SphereCollider LeafCollider;
	public Vector3 MeshOrientation;

	public int SubtreeSize {
		get;
		private set;
	}

	public Node(Vector3 position, TreeGenerator tree) {
		this.Position = position;
		this.Direction = Vector3.up;
		this.Depth = 0;
		this.Children = new Node[] { };
		this.Tree = tree;
	}

	public Node(Vector3 position, Node parent) {
		this.Position = position;
		this.Direction = (this.Position - parent.Position).normalized;
		this.Depth = parent.Depth + 1;
		this.Children = new Node[] { };
		parent.Children = parent.Children.Concat(new Node[] { this }).ToArray();
		this.Tree = parent.Tree;
		this.CrateLeafCollider();
		this.Parent = parent;
	}

	public void CrateLeafCollider() {
		this.LeafCollider = this.Tree.LeafColliders.AddComponent<SphereCollider>();
		this.LeafCollider.center = this.Position;
		this.LeafCollider.radius = this.Tree.LeafColliderSize;
	}

	public void RemoveLeafCollider() {
		if (this.LeafCollider == null) {
			return;
		}
		GameObject.DestroyImmediate(this.LeafCollider);
	}
	
	public IEnumerable<Node> GetTree() {
		yield return this;
		Node currentNode = this;
		var positions = new Stack<int>();
		int currentPosition = 0;

		while (true) {
			if (currentPosition < currentNode.Children.Length) {
				var child = currentNode.Children[currentPosition];
				currentPosition++;
				yield return child;
				if (child.Children.Length > 0) {
					currentNode = child;
					positions.Push(currentPosition);
					currentPosition = 0;
				}
			} else if (currentNode.Parent != null) {
				currentPosition = positions.Pop();
				currentNode = currentNode.Parent;
			} else {
				yield break;
			}
		}
	}
	
	private float raycast(Vector3 position, Vector3 direction, float skip = 0f) {
		this.Tree.RayCastCount++;
		var ray = new Ray(this.Tree.transform.position + position + direction.normalized * skip, direction);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) {
			return hit.distance + skip;
		} else {
			return System.Single.PositiveInfinity;
		}
	}


	public void Branch() {
		if (this.Children.Count() == 0) {
			return;
		}

		float length = this.Tree.BranchLength * Mathf.Pow(this.Tree.BranchLengthFalloff, this.Depth);
		var childDir = this.Children[0].Direction;
		var direction = this.getGrowthDirection(() => childDir * Mathf.Cos(this.Tree.BranchAngle * Mathf.Deg2Rad) + Vector3.Cross(childDir, UnityEngine.Random.onUnitSphere) * Mathf.Sign(this.Tree.BranchAngle * Mathf.Rad2Deg), length);

		if (!Mathf.Approximately(direction.magnitude, 0)) {
			new Node(this.Position + direction * length, this);
		}
	}

	private Vector3 getGrowthDirection(Func<Vector3> vectorGenerator, float minDistance) {
		Vector3 result = Vector3.zero;
		float longestDistance = minDistance;

		for (int i = 0; i < 20; i++) {
			var direction = vectorGenerator.Invoke().normalized;
			float range = this.raycast(this.Position, direction, 0.01f);
			if (System.Single.IsPositiveInfinity(range)) {
				return direction;
			}
			if (range > longestDistance) {
				result = direction;
				longestDistance = range;
			}
		}
		return result;
	}

	public void Grow() {
		if (this.Children.Count() != 0) {
			return;
		}

		float length = this.Tree.BranchLength * Mathf.Pow(this.Tree.BranchLengthFalloff, this.Depth);
		var direction = this.getGrowthDirection(() => this.Direction + this.Tree.Distort * UnityEngine.Random.onUnitSphere, length);
				
		if (!Mathf.Approximately(direction.magnitude, 0)) {
			new Node(this.Position + direction * length, this);
		}
	}

	public void CalculateEnergy() {
		this.Energy = 1f -0.001f * this.Depth - Mathf.Exp(-this.raycast(this.Position, Vector3.up, this.Tree.LeafColliderSize * 1.1f));
	}

	public void CalculateSubtreeSize() {
		if (this.Children.Length == 0) {
			this.SubtreeSize = 1;
		} else {
			foreach (var child in this.Children) {
				child.CalculateSubtreeSize();
			}
			this.SubtreeSize = this.Children.Sum(child => child.SubtreeSize) + 1;
		}
	}
}