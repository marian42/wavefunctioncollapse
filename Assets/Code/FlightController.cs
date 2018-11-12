using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightController : MonoBehaviour {

	[Range(0f, 20f)]
	public float Velocity;

	public const KeyCode OnOffKey = KeyCode.M;

	public void OnEnable() {
		this.GetComponent<FirstPersonController>().enabled = false;
		this.transform.position = this.transform.position - Vector3.up * this.transform.position.y + Vector3.up * (GameObject.FindObjectOfType<MapGenerator>().Height * MapGenerator.BlockSize + 2f);
		var cameraTransform = this.transform.GetChild(0);
		cameraTransform.rotation = Quaternion.Euler(cameraTransform.rotation.eulerAngles - Vector3.right * cameraTransform.rotation.eulerAngles.x + Vector3.right * 24f);
	}

	void Update () {
		var direction = this.transform.forward + Vector3.down * this.transform.forward.y;
		this.transform.position += direction.normalized * this.Velocity * Time.deltaTime;

		if (Input.GetKeyDown(FlightController.OnOffKey)) {
			this.enabled = false;
			this.GetComponent<FirstPersonController>().enabled = true;
		}

		this.Velocity = Mathf.Clamp(this.Velocity + Input.GetAxis("Move Y") * Time.deltaTime * 2f, 0f, 20f);
	}
}
