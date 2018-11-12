using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ModuleSet : ICollection<Module> {
	private const int bitsPerItem = 64;

	private long[] data;

	public int Count {
		get;
		private set;
	}
	
	public ModuleSet(bool initializeFull = false) {
		this.data = new long[Module.All.Length / bitsPerItem + (Module.All.Length % bitsPerItem == 0 ? 0 : 1)];
		this.Count = 0;

		if (initializeFull) {
			this.Count = Module.All.Length;
			for (int i = 0; i < this.data.Length; i++) {
				this.data[i] = ~0;
			}
		}
	}

	public ModuleSet(ModuleSet source) {
		this.data = source.data.ToArray();
		this.Count = source.Count;
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
			this.Count++;
		}
	}

	public bool Remove(Module module) {
		int i = module.Index / bitsPerItem;
		long mask = (long)1 << (module.Index % bitsPerItem);

		long value = this.data[i];
	
		if ((value & mask) != 0) {
			this.data[i] = value & ~mask;
			this.Count--;
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
		this.Count = 0;
		for (int i = 0; i < this.data.Length; i++) {
			this.data[i] = 0;
		}
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
		Debug.Log(arrayIndex + ", " + Module.All.Length);
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
					yield return Module.All[index];
				}
				index++;
				if (index >= Module.All.Length) {
					yield break;
				}
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return (IEnumerator)this.GetEnumerator();
	}

	public void PrintDebug() {
		var s = this.Count + ": " + string.Join("-", this.data.Select(l => Convert.ToString(l, 2)).ToArray()) + "   --   " + string.Join(", ", this.Select(m => m.Index.ToString()).ToArray());
		Debug.Log(s);
	}
}
