using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace UnityHue.Examples
{
	public class HueDemo : MonoBehaviour
	{
		public HueInfoStorer storer;
		public GameObject hueUIRepresentationPrefab;
		public GameObject createUserScreen;
		public Text createUserText;
		public Button createUserButton;
		public RectTransform lightMenu;
		public string applicationName = "UHue", deviceName = "MyHue";


		void Awake()
		{
			//Either the ip and username are stored in the monobehaviour or it was succesfully restored from player prefs
			if ((HueBridge.instance.CurrentBridge != null && HueBridge.instance.CurrentBridge.HasIP && HueBridge.instance.CurrentBridge.HasUsername) ||
				storer.Restore())
			{
				HueBridge.instance.UpdateLights(OnLightsRetrieved, HandleLightsError);
			}
			else
			{
				//We have to discover the bridges
				HueBridge.instance.DiscoverBridges(OnBridgesDiscovered);
			}
		}
		public void OnLightsRetrieved()
		{
			Debug.Log("Retrieved lights");
			createUserScreen.SetActive(false);
			foreach (var light in HueBridge.instance.Lights)
			{
				GameObject representation = Instantiate(hueUIRepresentationPrefab, lightMenu) as GameObject;
				representation.GetComponent<HueUIRepresentation>().Initialize(light);
			}
			storer.Save();
		}

		public void OnBridgesDiscovered()
		{
			createUserScreen.SetActive(true);
			if (HueBridge.instance.bridges.Count < 1)
			{
				createUserText.text = "Couldn't find any bridges in your Network";
				createUserButton.gameObject.SetActive(false);
				Debug.LogWarning("Failed to find Bridges in your Network");
			}
			else
			{
				createUserButton.gameObject.SetActive(true);
			}
		}

		public void RegisterApp()
		{
			HueBridge.instance.CreateUser(applicationName, deviceName, () => HueBridge.instance.UpdateLights(OnLightsRetrieved), OnRegistrationError);
			createUserButton.gameObject.SetActive(false);
		}

		public void HandleLightsError(List<HueErrorInfo> errors)
		{
			bool isRequestError = false;

			foreach (HueErrorInfo error in errors)
			{
				Debug.LogWarning(error);
				isRequestError |= error.IsRequestError;
			}

			if (!isRequestError)
			{
				return;
			}

			Debug.Log("Connecting to a previously stored hue failed, trying to discover new bridges");
			HueBridge.instance.DiscoverBridges(OnBridgesDiscovered);
		}

		public void OnRegistrationError(List<HueErrorInfo> errors)
		{
			HueErrorInfo error = errors[0];

			if (error.errorCode == 101)
			{
				createUserText.text = "The Link Button on the Bridge wasn't pressed. Press it and try again";
				createUserButton.gameObject.SetActive(true);
			}
			else
			{
				HueErrorInfo.LogErrors(errors);
			}
		}
	}
}