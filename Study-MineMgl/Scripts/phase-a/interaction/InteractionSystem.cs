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
	// handles player to world interaction via raycast
	public class InteractionSystem : MonoBehaviour
	{
		[SerializeField] Camera _playerCam;
		[SerializeField] float interactionRange = 2.5f;
		[SerializeField] LayerMask _interactionLayerMask;

		[SerializeField] InteractionWheelUI _interactionWheelUI;
		[SerializeField] KeyCode _interactKey = KeyCode.E;

		#region Unity Life Cycle
		private void Update()
		{
			if (INPUT.K.InstantDown(this._interactKey))
				this.TryInteract();
		}
		#endregion
		public void TryInteract()
		{
			if (Singleton<UIManager>.Ins != null && Singleton<UIManager>.Ins.IsInAnyMenu())
				return;

			var obj = this.GetLookedAtObject();
			if (obj == null)
				return;

			if (this._interactionWheelUI != null)
				this._interactionWheelUI.ClearInteractionWheel();
			var INTERACTABLE = obj.GetComponentsInParent<IInteractable>();

			if (INTERACTABLE == null)
				return;
			if (INTERACTABLE.Length == 0)
				return;

			if((INTERACTABLE.Length ==1) && !INTERACTABLE[0].ShouldUseInteractionWheel())
			{
				List<SO_Interaction> INTERACTION = INTERACTABLE[0].GetInteractions();
				if (INTERACTION.Count > 0)
					INTERACTABLE[0].Interact(INTERACTION[0]);
			}
			else
			{
				if (this._interactionWheelUI == null)
					return;
				this._interactionWheelUI.gameObject.SetActive(true);
				INTERACTABLE.forEach(interactable =>
				{
					this._interactionWheelUI.PopulateInteractionWheel(interactable);
				});
			}
		}
		public GameObject GetLookedAtObject()
		{
			Ray ray = new Ray(this._playerCam.transform.position, this._playerCam.transform.forward);
			if (Physics.Raycast(ray, out RaycastHit hit, this.interactionRange, this._interactionLayerMask))
				return hit.collider.gameObject;
			return null;
		}
	}
}