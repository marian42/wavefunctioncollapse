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
	private float timeInAir = 0f;

	void Start () {
		this.characterController = this.GetComponent<CharacterController>();
		this.cameraTransform = this.GetComponentInChildren<Camera>().transform;
		Cursor.visible = false;
	}
	
	void Update () {
		bool touchesGround = this.onGround();
		float runMultiplier = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;
		this.characterController.Move(this.transform.forward * Input.GetAxis("Vertical") * Time.deltaTime * this.MovementSpeed * runMultiplier + this.transform.right * Input.GetAxis("Horizontal") * Time.deltaTime * this.MovementSpeed * runMultiplier);
		this.transform.rotation = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * Time.deltaTime * this.LookSensitivity, Vector3.up) * this.transform.rotation;
		this.cameraTilt = Mathf.Clamp(this.cameraTilt - Input.GetAxis("Mouse Y") * this.LookSensitivity * Time.deltaTime, -90f, 90f);
		this.cameraTransform.localRotation = Quaternion.AngleAxis(this.cameraTilt, Vector3.right);

		if (touchesGround) {
			this.timeInAir = 0;
		} else {
			this.timeInAir += Time.deltaTime;
		}

		if (touchesGround && this.verticalSpeed < 0) {
			this.verticalSpeed = 0;
		} else {
			this.verticalSpeed -= 9.18f * Time.deltaTime;
		}
		if (this.timeInAir < 0.5f && Input.GetKeyDown(KeyCode.Space)) {
			this.timeInAir = 0.5f;
			this.verticalSpeed = this.JumpStrength;
		}
		if (Input.GetKey(KeyCode.LeftControl)) {
			this.verticalSpeed = 2f;
		}
		this.characterController.Move(Vector3.up * Time.deltaTime * this.verticalSpeed);
	}

	private bool onGround() {
		var ray = new Ray(this.transform.position, Vector3.down);
		return Physics.SphereCast(ray, this.characterController.radius, this.characterController.height / 2 - this.characterController.radius + 0.1f);
	}
}
