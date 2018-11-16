using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Vector3i : IEquatable<Vector3i> {
	public int X;
	public int Y;
	public int Z;

	public Vector3i(int x, int y, int z) {
		this.X = x;
		this.Y = y;
		this.Z = z;
	}

	public Vector3i(Vector3 vector) {
		this.X = (int)vector.x;
		this.Y = (int)vector.y;
		this.Z = (int)vector.z;
	}

	public static Vector3i operator+ (Vector3i a, Vector3i b) {
		return new Vector3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	}

	public static Vector3i operator -(Vector3i a, Vector3i b) {
		return new Vector3i(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	}

	public static Vector3i operator -(Vector3i v) {
		return new Vector3i(-v.X, -v.Y, -v.Z);
	}

	public static Vector3i operator *(int f, Vector3i b) {
		return new Vector3i(f * b.X, f * b.Y, f * b.Z);
	}
	public static Vector3i operator *(Vector3i b, int f) {
		return new Vector3i(f * b.X, f * b.Y, f * b.Z);
	}

	public static Vector3i operator /(Vector3i b, int d) {
		return new Vector3i(b.X / d, b.Y / d, b.Z / d);
	}

	public static bool operator ==(Vector3i a, Vector3i b) {
		return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
	}

	public static bool operator !=(Vector3i a, Vector3i b) {
		return !(a == b);
	}

	public override bool Equals(object other) {
		return !(other is Vector3i) && this == (Vector3i)other;
	}
	public bool Equals(Vector3i other) {
		return this.X == other.X && this.Y == other.Y && this.Z == other.Z;
	}

	public override int GetHashCode() {
		return (this.X.GetHashCode() * 314159 + this.Y.GetHashCode()) * 23 + this.Z.GetHashCode();
	}

	public Vector3 ToVector3() {
		return new Vector3(this.X, this.Y, this.Z);
	}

	public override string ToString() {
		return "(" + this.X + ", " + this.Y + ", " + this.Z + ")";
	}

	public float Magnitude {
		get {
			return Mathf.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
		}
	}

	public static Vector3i zero {
		get {
			return new Vector3i(0, 0, 0);
		}
	}
}
