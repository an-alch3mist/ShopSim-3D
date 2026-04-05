using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

using SPACE_UTIL;

namespace SPACE_MineMGL
{
	public class SimplePlayerController : Singleton<SimplePlayerController>
	{
		[SerializeField] float _walkSpeed = 4f, _gravity = -10f;
		[SerializeField] float _mouseSensitivity = 2f;

		[SerializeField] Camera _playerCam;
		CharacterController cc;
		protected override void Awake()
		{
			Debug.Log(C.method(this));
			base.Awake();
			cc = this.gc<CharacterController>();
			Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;

			// ==== subscribe ====
			Debug.Log(C.method(null, "magenta", "subscribed HandleMenyStateChanged"));
			GameEvents.OnMenuStateChanged += this.HandleMenuStateChanged;
			// ==== subscribe ====
		}
		private void Update()
		{
			this.HandleCursorState();
			if (isAnyMenuOpen == false)
			{
				this.HandleLook();
				this.HandleMovement();
			}
		}
		private void OnDestroy()
		{
			// ==== un-subscribe ====
			Debug.Log(C.method(null, "magenta", "un-subscribed HandleMenyStateChanged"));
			GameEvents.OnMenuStateChanged -= this.HandleMenuStateChanged;
			// ==== un-subscribe ====
		}

		// caches menu state instead of pooling UIManager
		[SerializeField] bool isAnyMenuOpen;
		void HandleMenuStateChanged(bool isAnyMenuOpen)
		{
			this.isAnyMenuOpen = isAnyMenuOpen;
		}

		#region cursor lock, movement logic
		// locks unlocks cursor based on cached menu state from GameEvents
		void HandleCursorState()
		{
			if(this.isAnyMenuOpen == true)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
			else
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
		}

		float xRot, yRot;
		Vector3 vel;
		// rotates cam vertically, player horizontally
		void HandleLook()
		{
			float mouseX = Input.GetAxis("Mouse X") * this._mouseSensitivity;
			float mouseY = Input.GetAxis("Mouse Y") * this._mouseSensitivity;

			this.xRot -= mouseY;
			this.xRot = this.xRot.clamp(-80f, 80f);
			this._playerCam.transform.localRotation = Quaternion.Euler
			(
				Vector3.right * this.xRot
			);

			this.yRot += mouseX;
			this.transform.localRotation = Quaternion.Euler(Vector3.up * this.yRot);
		}
		// moves charater using keys WASD and applies gravity
		void HandleMovement()
		{
			float dt = Time.deltaTime;
			Vector3 move = (transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical")) * this._walkSpeed;
			this.cc.Move(move * dt);
			//
			if (this.cc.isGrounded && this.vel.y <= 0f) this.vel.y = -2f;
			this.vel.y += this._gravity * dt;
			this.cc.Move(this.vel * dt);
		} 
		#endregion
	}
}