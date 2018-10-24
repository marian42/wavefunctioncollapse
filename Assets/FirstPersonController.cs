using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour {

	[Range(1f, 5f)]
	public float MovementSpeed = 1f;

	[Range(1, 100f)]
	public float LookSensitivity = 10f;

	[Range(1, 100f)]
	public float JumpStrength = 2f;

	private CharacterController characterController;
	private Transform cameraTransform;

	private float cameraTilt = 0f;
	private float verticalSpeed = 0f;

	void Start () {
		this.characterController = this.GetComponent<CharacterController>();
		this.cameraTransform = this.GetComponentInChildren<Camera>().transform;
		Cursor.visible = false;
	}
	
	void Update () {
		this.characterController.Move(this.transform.forward * Input.GetAxis("Vertical") * Time.deltaTime * this.MovementSpeed + this.transform.right * Input.GetAxis("Horizontal") * Time.deltaTime * this.MovementSpeed);
		this.transform.rotation = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * Time.deltaTime * this.LookSensitivity, Vector3.up) * this.transform.rotation;
		this.cameraTilt = Mathf.Clamp(this.cameraTilt - Input.GetAxis("Mouse Y") * this.LookSensitivity * Time.deltaTime, -90f, 90f);
		this.cameraTransform.localRotation = Quaternion.AngleAxis(this.cameraTilt, Vector3.right);

		if (this.characterController.isGrounded) {
			this.verticalSpeed = 0;
		} else {
			this.verticalSpeed -= 9.18f * Time.deltaTime;
		}
		if (Input.GetKeyDown(KeyCode.Space)) {
			this.jump();
		}
		if (Input.GetKey(KeyCode.LeftShift)) {
			this.verticalSpeed = 2f;
		}
		this.characterController.Move(Vector3.up * Time.deltaTime * this.verticalSpeed);
	}

	private bool onGround() {
		var ray = new Ray(this.transform.position, Vector3.down);
		return Physics.Raycast(ray, this.characterController.height / 2 + 0.1f);
	}

	private void jump() {
		if (!this.onGround()) {
			return;
		}
		this.verticalSpeed = this.JumpStrength;
	}
}
