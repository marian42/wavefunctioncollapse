using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class ModuleSet : ICollection<Module> {
	private const int bitsPerItem = 64;

	[SerializeField]
	private long[] data;

	private float entropy;
	private bool entropyOutdated = true;

	[SerializeField]
	private int count;

	public int Count {
		get {
			return this.count;
		}
	}

	public bool Full {
		get {
			return this.count == ModuleData.Current.Length;
		}
	}

	public float Entropy {
		get {
			if (this.entropyOutdated) {
				this.entropy = this.calculateEntropy();
				this.entropyOutdated = false;
			}
			return this.entropy;
		}
	}
	
	public ModuleSet(bool initializeFull = false) {
		this.data = new long[ModuleData.Current.Length / bitsPerItem + (ModuleData.Current.Length % bitsPerItem == 0 ? 0 : 1)];
		this.count = 0;

		if (initializeFull) {
			this.count = ModuleData.Current.Length;
			for (int i = 0; i < this.data.Length; i++) {
				this.data[i] = ~0;
			}
		}
	}

	public ModuleSet(ModuleSet source) {
		this.data = source.data.ToArray();
		this.count = source.count;
		this.entropy = source.Entropy;
		this.entropyOutdated = false;
	}

	public static ModuleSet FromEnumerable(IEnumerable<Module> source) {
		var result = new ModuleSet();
		foreach (var module in source) {
			result.Add(module);
		}
		return result;
	}

	public void Add(Module module) {
		int i = module.Index / bitsPerItem;
		long mask = (long)1 << (module.Index % bitsPerItem);

		long value = this.data[i];
	
		if ((value & mask) == 0) {
			this.data[i] = value | mask;
			this.count++;
			this.entropyOutdated = true;
		}
	}

	public bool Remove(Module module) {
		int i = module.Index / bitsPerItem;
		long mask = (long)1 << (module.Index % bitsPerItem);

		long value = this.data[i];
	
		if ((value & mask) != 0) {
			this.data[i] = value & ~mask;
			this.count--;
			this.entropyOutdated = true;
			return true;
		} else {
			return false;
		}
	}

	public bool Contains(Module module) {
		int i = module.Index / bitsPerItem;
		long mask = (long)1 << (module.Index % bitsPerItem);
		return (this.data[i] & mask) != 0;
	}

	public bool Contains(int index) {
		int i = index / bitsPerItem;
		long mask = (long)1 << (index % bitsPerItem);
		return (this.data[i] & mask) != 0;
	}

	public void Clear() {
		this.count = 0;
		this.entropyOutdated = true;
		for (int i = 0; i < this.data.Length; i++) {
			this.data[i] = 0;
		}
	}

	/// <summary>
	/// Removes all modules that are not in the supplied set.
	/// Returns true if this set was changed.
	/// </summary>
	/// <param name="moduleSet"></param>
	/// <returns></returns>
	
	public bool Intersect(ModuleSet moduleSet) {
		bool changed = false;

		for (int i = 0; i < this.data.Length; i++) {
			long current = this.data[i];
			long mask = moduleSet.data[i];
			long updated = current & mask;

			if (current != updated) {
				changed = true;

				// TODO make count update faster
				long removed = current ^= updated;
				while (removed != 0) {
					this.count--;
					removed &= removed - 1;
				}
			}
			this.data[i] = updated;
		}

		return changed;
	}

	public bool IsReadOnly {
		get {
			return false;
		}
	}

	public void CopyTo(Module[] array, int arrayIndex) {
		foreach (var item in this) {
			array[arrayIndex] = item;
			arrayIndex++;
		}
	}

	public IEnumerator<Module> GetEnumerator() {
		int index = 0;
		foreach (long value in this.data) {
			if (value == 0) {
				index += bitsPerItem;
				continue;
			}
			for (int j = 0; j < bitsPerItem; j++) {
				if ((value & ((long)1 << j)) != 0) {
					yield return ModuleData.Current[index];
				}
				index++;
				if (index >= ModuleData.Current.Length) {
					yield break;
				}
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return (IEnumerator)this.GetEnumerator();
	}

	public void PrintDebug() {
		var s = this.count + ": " + string.Join("-", this.data.Select(l => Convert.ToString(l, 2)).ToArray()) + "   --   " + string.Join(", ", this.Select(m => m.Index.ToString()).ToArray());
		Debug.Log(s);
	}

	private float calculateEntropy() {
		float total = 0;
		float entropySum = 0;
		foreach (var module in this) {
			total += module.Prototype.Probability;
			entropySum += module.Prototype.Probability * Mathf.Log(module.Prototype.Probability);
		}
		return -1f / total * entropySum + Mathf.Log(total);
	}
}
