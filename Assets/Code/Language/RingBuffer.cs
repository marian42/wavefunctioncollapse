using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingBuffer<T> {
	public readonly int Size;

	public Action<T> OnOverflow;

	public int Count {
		get;
		private set;
	}

	// Includes discarded items
	public int TotalCount {
		get;
		private set;
	}

	private T[] buffer;
	private int position;

	public RingBuffer(int size) {
		this.Size = size;
		this.buffer = new T[size];
		this.Count = 0;
		this.position = 0;
	}

	public void Push(T item) {
		this.position = (this.position + 1) % this.Size;
		if (this.buffer[this.position] != null && this.OnOverflow != null) {
			this.OnOverflow(this.buffer[this.position]);
		}
		this.buffer[this.position] = item;
		this.Count++;
		if (this.Count > this.Size) {
			this.Count = this.Size;
		}
		this.TotalCount++;
	}

	public T Peek() {
		if (this.Count == 0) {
			throw new System.InvalidOperationException();
		}
		return this.buffer[this.position];
	}

	public T Pop() {
		if (this.Count == 0) {
			throw new System.InvalidOperationException();
		}
		T result = this.buffer[this.position];
		this.buffer[this.position] = default(T);

		this.position = (this.position + this.Size - 1) % this.Size;
		this.Count--;
		this.TotalCount--;

		return result;
	}

	public bool Any() {
		return this.Count != 0;
	}
}
