using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
	[Range(1f, 10f)]
	public float MovementSpeed = 2f;

	[Range(1, 500f)]
	public float LookSensitivity = 200f;

	[Range(1, 500f)]
	public float MouseSensitivity = 3;

	private CharacterController characterController;
	private Transform cameraTransform;

	private float cameraTilt = 0f;

	void Start() {
		this.characterController = this.GetComponent<CharacterController>();
		this.cameraTransform = this.GetComponentInChildren<Camera>().transform;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Confined;
	}

	void Update() {
		Vector3 movementVector = this.cameraTransform.forward * Input.GetAxis("Move Y") + this.cameraTransform.right * Input.GetAxis("Move X");
		if (movementVector.sqrMagnitude > 1) {
			movementVector.Normalize();
		}
		if (Input.GetAxisRaw("Jetpack") > 0.1f) {
			movementVector += Vector3.up * 0.8f;
		}
		if (Input.GetAxisRaw("Run") > 0.1f) {
			movementVector += Vector3.down * 0.8f;
		}
		this.characterController.Move(movementVector * Time.deltaTime * this.MovementSpeed);

		this.transform.localRotation = Quaternion.AngleAxis(Input.GetAxis("Mouse Look X") * this.MouseSensitivity + Input.GetAxis("Look X") * this.LookSensitivity * Time.deltaTime, Vector3.up) * this.transform.rotation;
		this.cameraTilt = Mathf.Clamp(this.cameraTilt - Input.GetAxis("Mouse Look Y") * this.MouseSensitivity - Input.GetAxis("Look Y") * this.LookSensitivity * Time.deltaTime, -90f, 90f);
		this.cameraTransform.localRotation = Quaternion.AngleAxis(this.cameraTilt, Vector3.right);

		if (Input.GetKeyDown(FlightController.OnOffKey)) {
			this.enabled = false;
			this.GetComponent<FirstPersonController>().enabled = true;
		}
	}

	void OnEnable() {
		this.GetComponent<FirstPersonController>().enabled = false;
	}
}
