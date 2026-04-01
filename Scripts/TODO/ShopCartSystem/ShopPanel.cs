using UnityEngine;

using SPACE_UTIL;

// ============================================================
//  ShopPanel.cs
//  Root controller for the shop UI.  Handles open / close.
//
//  Attach to the root Canvas GameObject that contains both
//  the catalogue panel (left) and the cart panel (right).
//
//  Pressing _openKey toggles the whole panel.
//  Time.timeScale is paused while the shop is open — remove
//  the timeScale lines if you prefer real-time browsing.
// ============================================================
public class ShopPanel : MonoBehaviour
{
	[Header("Open / Close")]
	[SerializeField] KeyCode _openKey = KeyCode.B;

	[Header("Root panel objects to show/hide")]
	[SerializeField] GameObject _go_shopRoot;   // the whole shop canvas group

	// ── State ─────────────────────────────────────────────────
	bool _isOpen = false;

	// ── Unity ─────────────────────────────────────────────────
	private void Awake()
	{
		Debug.Log(C.method(this));
		SetOpen(false);
	}

	private void Update()
	{
		if (Input.GetKeyDown(_openKey))
			Toggle();
	}

	// ── API ───────────────────────────────────────────────────
	public void Toggle() => SetOpen(!_isOpen);

	public void SetOpen(bool open)
	{
		_isOpen = open;
		_go_shopRoot.SetActive(open);
		// optional: pause game while shopping
		Time.timeScale = open ? 0f : 1f;
		Debug.Log($"[ShopPanel] {(open ? "opened" : "closed")}".colorTag("cyan"));
	}

	public void Close() => SetOpen(false);
}
