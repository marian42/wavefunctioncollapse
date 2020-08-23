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

	void OnEnable() {
		this.characterController = this.GetComponent<CharacterController>();
		this.cameraTransform = this.GetComponentInChildren<Camera>().transform;
		this.cameraTilt = this.cameraTransform.localRotation.eulerAngles.x;
	}

	void Update() {
		Vector3 movementVector = this.cameraTransform.forward * Input.GetAxis("Move Y")
			+ this.cameraTransform.right * Input.GetAxis("Move X")
			+ Vector3.up * (Input.GetAxis("Move Up/Down") + Input.GetAxisRaw("Jump"));

		if (movementVector.sqrMagnitude > 1) {
			movementVector.Normalize();
		}
		if (Input.GetAxisRaw("Run") > 0.1f) {
			movementVector *= 4;
		}
		this.characterController.Move(movementVector * Time.deltaTime * this.MovementSpeed);

		this.transform.localRotation = Quaternion.AngleAxis(Input.GetAxis("Mouse Look X") * this.MouseSensitivity + Input.GetAxis("Look X") * this.LookSensitivity * Time.deltaTime, Vector3.up) * this.transform.rotation;
		this.cameraTilt = Mathf.Clamp(this.cameraTilt - Input.GetAxis("Mouse Look Y") * this.MouseSensitivity - Input.GetAxis("Look Y") * this.LookSensitivity * Time.deltaTime, -90f, 90f);
		this.cameraTransform.localRotation = Quaternion.AngleAxis(this.cameraTilt, Vector3.right);
	}
}
