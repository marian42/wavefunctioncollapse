using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FirstPersonController))]
public class FlightController : MonoBehaviour {

	[Range(0f, 20f)]
	public float Velocity;

	public const KeyCode OnOffKey = KeyCode.M;

	private FirstPersonController firstPersonController;

	public void OnEnable() {
		this.firstPersonController = this.GetComponent<FirstPersonController>();
		this.firstPersonController.enabled = false;
		this.transform.position = this.transform.position - Vector3.up * this.transform.position.y + Vector3.up * (GameObject.FindObjectOfType<MapBehaviour>().Map.Height * InfiniteMap.BLOCK_SIZE + 2f);
		var cameraTransform = this.transform.GetChild(0);
		cameraTransform.rotation = Quaternion.Euler(cameraTransform.rotation.eulerAngles - Vector3.right * cameraTransform.rotation.eulerAngles.x + Vector3.right * 24f);
	}

	void Update () {
		var direction = this.transform.forward + Vector3.down * this.transform.forward.y;
		this.transform.position += direction.normalized * this.Velocity * Time.deltaTime;

		if (Input.GetKeyDown(FlightController.OnOffKey)) {
			this.enabled = false;
			this.firstPersonController.enabled = true;
		}

		this.Velocity = Mathf.Clamp(this.Velocity + Input.GetAxis("Move Y") * Time.deltaTime * 4f, 0f, 20f);
		this.transform.rotation = Quaternion.Euler(Vector3.up * Input.GetAxis("Look X") * Time.deltaTime * this.firstPersonController.LookSensitivity) * this.transform.rotation;
	}
}
