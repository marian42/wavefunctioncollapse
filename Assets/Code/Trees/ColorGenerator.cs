using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ColorGenerator {
	public float Probability = 1;

	[Range(0f, 1f)]
	public float HueMin;
	[Range(0f, 1f)]
	public float HueMax;

	[Range(0f, 1f)]
	public float SaturationMin;
	[Range(0f, 1f)]
	public float SaturationMax;

	[Range(0f, 1f)]
	public float ValueMin;
	[Range(0f, 1f)]
	public float ValueMax;

	public Color GetColor() {
		return Color.HSVToRGB(
			Random.Range(this.HueMin, this.HueMax),
			Random.Range(this.SaturationMin, this.SaturationMax),
			Random.Range(this.ValueMin, this.ValueMax)
		);
	}
}
