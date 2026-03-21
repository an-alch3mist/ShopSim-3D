using UnityEngine;
using UnityEngine.InputSystem;

using SPACE_UTIL;

[DefaultExecutionOrder(-40)] // just after INITManager, InputSystem Init
public class GameStoreShopSim : MonoBehaviour
{
	[SerializeField] InputActionAsset _IA;
	public static InputActionAsset IA;
	[SerializeField] POIRegistry poiRegistry;
	private void Awake()
	{
		Debug.Log(C.method(this));
		GameStoreShopSim.IA = this._IA;
		poiRegistry.Init();
	}
}
