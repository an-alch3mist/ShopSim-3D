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
	// radial interaction meny that appears when an object has multiple interations
	public class InteractionWheelUI : MonoBehaviour
	{
		[SerializeField] GameObject _pfInteractionButton;
		[SerializeField] Transform _holderTransform;
		[SerializeField] TextMeshProUGUI _objectNameTxt;
		[SerializeField] KeyCode _closeInteractionKey = KeyCode.Escape;

		#region Unity Life Cycle
		private void Update()
		{
			if (INPUT.K.InstantDown(this._closeInteractionKey) || INPUT.K.InstantDown(KeyCode.E))
				this.CloseWheel();
		}
		#endregion

		public void OpenWheel()
		{
			this.gameObject.SetActive(true);
		}
		private void CloseWheel()
		{
			this.ClearInteractionWheel();
			this.gameObject.SetActive(false);
		}
		//
		List<GameObject> BUTTON_OBJ = new List<GameObject>();
		Dictionary<Button, IInteractable> buttonInteractable_MAP = new Dictionary<Button, IInteractable>();
		public void PopulateInteractionWheel(IInteractable interactable)
		{
			this._objectNameTxt.text = interactable.GetObjectName();

			interactable.GetInteractions().forEach( so_interaction =>
			{
				Button button = GameObject.Instantiate(this._pfInteractionButton, this._holderTransform).gc<Button>();
				TextMeshPro tm = button.Q().downCompoGf<TextMeshPro>();

				this.BUTTON_OBJ.Add(button.gameObject);

				button.onClick.AddListener(() =>
				{
					this.SelectInteraction(so_interaction, interactable);
				});
				this.buttonInteractable_MAP[button] = interactable;			});
		}
		public void ClearInteractionWheel()
		{
			foreach (var kvp in this.buttonInteractable_MAP)
				kvp.Key.onClick.RemoveAllListeners();
			this.buttonInteractable_MAP.Clear();

			foreach (var button in this.BUTTON_OBJ)
				GameObject.Destroy(button);
			this.BUTTON_OBJ.Clear();
		}


		#region private API
		private void SelectInteraction(SO_Interaction selectedInteraction, IInteractable interactable = null)
		{
			if (interactable != null)
				interactable.Interact(selectedInteraction);
			this.CloseWheel();
		}
		
		#endregion
	}
}