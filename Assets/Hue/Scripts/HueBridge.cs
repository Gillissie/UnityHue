using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using gilligames;

using MiniJSON;

namespace UnityHue
{
	/// <summary>
	/// Singleton that interfaces with the Hue API. Generally the HueBridge Component
	/// doesn't have to be present in a scene but can bootstrap itself
	/// </summary>
	[AddComponentMenu("Unity Hue/Hue Bridge")]
	public class HueBridge : UnitySingleton<HueBridge>
	{
		[Tooltip("Used for discovery of all the bridges in your network" +
			" (that have contacted a philipps server in the past.)")]
		[SerializeField]
		private string hueDiscoveryServer = "https://www.meethue.com/api/nupnp";
		[SerializeField]
		private HueBridgeInfo currentBridge;
		[SerializeField]
		protected List<HueLight> lights = new List<HueLight>();
		[SerializeField]
		protected List<HueGroup> groups = new List<HueGroup>();
		public List<HueBridgeInfo> bridges { get; private set; }

		void Awake()
		{
			DontDestroyOnLoad(this);
		}
		#region Public Functions

		/// <summary>
		/// Start discovery of bridges in the current network
		/// OnFinished will be called after a sucessful response 
		/// (even if the response is that zero bridges are in the network).
		/// If there are more than zero bridges in the Network the first one
		/// will be assigned to currentBridge, the full list is accesible under
		/// the Bridges property.
		/// </summary>
		/// <param name="onFinished">On finished.</param>
		public void DiscoverBridges(Action onFinished = null)
		{
			DiscoverBridges(onFinished, HueErrorInfo.LogErrors);
		}

		public void DiscoverBridges(Action onFinished, Action<List<HueErrorInfo>> errorCallback)
		{
			StartCoroutine(GetBridgesEnumerator(
				(bridges) =>
				{
					this.bridges = bridges;
					if (bridges.Count > 0)
					{
						currentBridge = bridges[0];
					}
					if (onFinished != null)
					{
						onFinished();
					}
				},
				errorCallback
			));
		}

		public void UpdateLights(Action onFinished = null)
		{
			UpdateLights(onFinished, HueErrorInfo.LogErrors);
		}

		public void UpdateLights(Action onFinished, Action<List<HueErrorInfo>> errorCallback)
		{
			DiscoverLights(
				(lights) =>
				{
					this.lights = lights;
					if (onFinished != null)
					{
						onFinished();
					}
				},
				errorCallback
			);
		}

		public void UpdateGroups(Action onFinished = null)
		{
			UpdateGroups(onFinished, HueErrorInfo.LogErrors);
		}

		public void UpdateGroups(Action onFinished, Action<List<HueErrorInfo>> errorCallback)
		{
			DiscoverGroups(
				(groups) =>
				{
					this.groups = groups;
					if (onFinished != null)
					{
						onFinished();
					}
				},
				errorCallback
			);
		}

		public void DiscoverLights(Action<List<HueLight>> lightsCallback, Action<List<HueErrorInfo>> errorCallback)
		{
			StartCoroutine(DiscoverLightsEnumerator(lightsCallback, errorCallback));
		}

		public void DiscoverGroups(Action<List<HueGroup>> groupsCallBack, Action<List<HueErrorInfo>> errorCallback)
		{
			StartCoroutine(DiscoverGroupsEnumerator(groupsCallBack, errorCallback));
		}

		//public void CreateUser(Action onFinished = null, Action<List<HueErrorInfo>> errorCallback = null)
		//{
		//	CreateUser(
		//		(userName) =>
		//		{
		//			Debug.LogFormat("created user: {0}", userName);
		//			currentBridge.userName = userName;
		//			if (onFinished != null)
		//			{
		//				onFinished();
		//			}
		//		},
		//		errorCallback
		//	);
		//}

		public void CreateUser(string applicationName, string deviceName, Action onFinished = null, Action<List<HueErrorInfo>> errorCallback = null)
		{
			CreateUser(
				applicationName,
				deviceName,
				(userName) =>
				{
					Debug.LogFormat("created user: {0}", userName);
					currentBridge.userName = userName;
					if (onFinished != null)
					{
						onFinished();
					}
				},
				errorCallback
			);
		}

		public void CreateUser(string applicationName, string deviceName, Action<string> generatedUsername, Action<List<HueErrorInfo>> errorCallback)
		{
			StartCoroutine(CreateUserEnumerator(applicationName, deviceName, generatedUsername, errorCallback));
		}

		public void DeleteLight(string id, Action<List<HueErrorInfo>> errorCallback = null)
		{
			StartCoroutine(DeleteLightEnumerator(id, errorCallback));
		}

		public void DeleteGroup(string id, Action<List<HueErrorInfo>> errorCallback = null)
		{
			StartCoroutine(DeleteGroupEnumerator(id, errorCallback));
		}

		// Gets a light's current data from the bridge and updates the client's data about it.
		public void UpdateLightFromBridge(string id, HueLight lightToUpdate, Action<List<HueErrorInfo>> errorCallback = null)
		{
			StartCoroutine(UpdateLightFromBridgeEnumerator(id, lightToUpdate, errorCallback));
		}

		public string GetHueStateString()
		{
			return JsonUtility.ToJson(GetStorableHueState());
		}

		public void RestoreHueFromString(string jsonContent)
		{
			if (string.IsNullOrEmpty(jsonContent))
				return;
			RestoreHueState(JsonUtility.FromJson<StoredHueInfo>(jsonContent));
		}

		public StoredHueInfo GetStorableHueState()
		{
			return new StoredHueInfo(currentBridge, bridges);
		}

		public void RestoreHueState(StoredHueInfo savedState)
		{
			if (savedState == null)
				return;
			bridges = savedState.allBridges;
			currentBridge = savedState.current;
		}

		#endregion

		#region Private Functions

		void ProcessBridges(JSON response, Action<List<HueBridgeInfo>> bridgesCallback)
		{
			Debug.LogFormat("Bridges: {0}", response);
			
			var list = new List<HueBridgeInfo>();

			var responseList = response.getJsonArray("rootArray");
			foreach (var item in responseList)
			{
				var bridgeInfo = new HueBridgeInfo(item.getString("id", ""), item.getString("internalipaddress", ""));

				if (item.hasKey("macaddress"))
				{
					bridgeInfo.macAdress = item.getString("macaddress", "");
				}

				if (item.hasKey("name"))
				{
					bridgeInfo.name = item.getString("name", "");
				}

				list.Add(bridgeInfo);
			}

			bridgesCallback(list);
		}

		void ProcessLights(JSON response, Action<List<HueLight>> lightsCallback)
		{
			var list = new List<HueLight>();
			foreach (var id in response.getKeyList())
			{
				var lightJson = response.getJSON(id);

				Debug.LogFormat("Light {0}: {1}", id, lightJson);

				//var lightDict = kv.Value as Dictionary<string, object>;
				var light = new HueLight(id);
				ProcessLightUpdate(lightJson, light);
				list.Add(light);
			}
			lightsCallback(list);
		}

		void ProcessGroups(JSON response, Action<List<HueGroup>> groupCallBack)
		{
			Debug.LogFormat("Groups: {0}", response);

			var list = new List<HueGroup>();
			foreach (var id in response.getKeyList())
			{
				var groupJson = response.getJSON(id);
				string name = groupJson.getString(HueKeys.NAME, "");
				var group = new HueGroup(name, id);
				list.Add(group);
			}
			groupCallBack(list);
		}

		void ProcessLightUpdate(JSON json, HueLight lightToUpdate)
		{
			lightToUpdate.name = json.getString(HueKeys.NAME, "");
			lightToUpdate.modelID = json.getString(HueKeys.MODEL_ID, "");
			lightToUpdate.type = json.getString(HueKeys.TYPE, "");
			lightToUpdate.softwareVersion = json.getString(HueKeys.SOFTWARE_VERSION, "");

			lightToUpdate.isColor = (lightToUpdate.type == "Extended color light");
			lightToUpdate.isDimmable = (lightToUpdate.isColor || (lightToUpdate.type == "Dimmable light"));

			var stateJson = json.getJSON(HueKeys.STATE);
			lightToUpdate.state = GetStateFromJSON(stateJson);
		}

		#endregion

		#region Request Enumerators

		IEnumerator GetBridgesEnumerator(Action<List<HueBridgeInfo>> bridgesCallback, Action<List<HueErrorInfo>> errorCallback = null)
		{
			var www = new WWWWrapper(hueDiscoveryServer);
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback))
			{
				ProcessBridges(www.responseJSON, bridgesCallback);
			}
		}

		IEnumerator CreateUserEnumerator(
			string applicationName,
			string deviceName,
			Action<string> generatedUserName,
			Action<List<HueErrorInfo>> errorCallback = null
		)
		{
			var body = new Dictionary<string, object>()
			{
				{ HueKeys.DEVICE_TYPE, string.Format("{0}#{1}", applicationName, deviceName) }
			};

			var www = new WWWWrapper(BaseURL, body);
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback))
			{
				JSON response = www.responseJSON.getJsonArray("rootArray")[0].getJSON("success");
				var userName = response.getString(HueKeys.USER_NAME, "");
				if (userName != "")
				{
					generatedUserName(userName);
				}
			}
		}

		IEnumerator DeleteLightEnumerator(string id, Action<List<HueErrorInfo>> errorCallback = null)
		{
			string url = string.Format("{0}/lights/{1}", BaseURLWithUserName, id);
			yield return DeleteEnumerator(url, errorCallback);
		}

		IEnumerator DeleteGroupEnumerator(string id, Action<List<HueErrorInfo>> errorCallback = null)
		{
			string url = string.Format("{0}/groups/{1}", BaseURLWithUserName, id);
			yield return DeleteEnumerator(url, errorCallback);
		}

		IEnumerator DeleteEnumerator(string url, Action<List<HueErrorInfo>> errorCallback = null)
		{
			var www = new WWWWrapper(url, method: HTTPMethod.DELETE);
			yield return www.waitToFinish();
			isValidJsonResponse(www, errorCallback);	// Just check for errors.
		}


		IEnumerator UpdateLightFromBridgeEnumerator(string id, HueLight lightToUpdate, Action<List<HueErrorInfo>> errorCallback = null)
		{
			string url = string.Format("{0}/lights/{1}", BaseURLWithUserName, id);

			var www = new WWWWrapper(url);
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback))
			{
				ProcessLightUpdate(www.responseJSON, lightToUpdate);
			}
		}

		IEnumerator DiscoverLightsEnumerator(Action<List<HueLight>> lightsCallback, Action<List<HueErrorInfo>> errorCallback)
		{
			string url = string.Format("{0}/{1}/lights", BaseURL, currentBridge.userName);

			var www = new WWWWrapper(url);
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback))
			{
				ProcessLights(www.responseJSON, lightsCallback);
			}
		}

		IEnumerator DiscoverGroupsEnumerator(Action<List<HueGroup>> groupsCallback, Action<List<HueErrorInfo>> errorCallback)
		{
			string url = string.Format("{0}/{1}/groups", BaseURL, currentBridge.userName);

			var www = new WWWWrapper(url);
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback))
			{
				ProcessGroups(www.responseJSON, groupsCallback);
			}
		}

		IEnumerator SendRequestEnumerator(WWWWrapper www, Action<string> successCallback,
			Action<List<HueErrorInfo>> errorCallback = null)
		{
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback) && successCallback != null)
			{
				successCallback(www.responseText);
			}
		}

		#endregion

		#region Helper

		private bool isValidJsonResponse(WWWWrapper www, System.Action<List<HueErrorInfo>> errorCallback)
		{
			var errorInfos = new List<HueErrorInfo>();
			JSON response = www.responseJSON;

			if (response == null)
			{
				errorInfos.Add(new HueErrorInfo(www.error, null));
			}
			else if (!response.isValid)
			{
				errorInfos.Add(new HueErrorInfo(null, www.responseText));
			}
			else
			{
				if (response.hasKey("rootArray"))
				{
					foreach (JSON json in response.getJsonArray("rootArray"))
					{
						if (json.hasKey(HueKeys.ERROR))
						{
							errorInfos.Add(new HueErrorInfo(json));
						}
					}
				}
				else if (response.hasKey(HueKeys.ERROR))
				{
					if (response.hasKey(HueKeys.ERROR))
					{
						errorInfos.Add(new HueErrorInfo(response));
					}
				}
			}

			if (errorInfos.Count > 0 && errorCallback != null)
			{
				errorCallback(errorInfos);
				return false;
			}

			return true;
		}

		public string BaseURL
		{
			get
			{
				return "http://" + currentBridge.ip + "/api";
			}
		}
		public string BaseURLWithUserName
		{
			get
			{
				return "http://" + currentBridge.ip + "/api/" + currentBridge.userName;
			}
		}
		public List<HueLight> Lights
		{
			get
			{
				return lights;
			}
		}
		public List<HueGroup> Groups
		{
			get
			{
				return groups;
			}
		}
		public HueBridgeInfo CurrentBridge
		{
			get
			{
				return currentBridge;
			}
		}

		HueLightState GetStateFromJSON(JSON json)
		{
			var state = new HueLightState();
			state.on = json.getBool(HueKeys.ON, false);
			state.reachable = json.getBool(HueKeys.REACHABLE, false);
			state.hue = json.getInt(HueKeys.HUE, 0);
			state.brightness = json.getInt(HueKeys.BRIGHTNESS, 0);
			state.saturation = json.getInt(HueKeys.SATURATION, 0);
			state.colorMode = json.getString(HueKeys.COLOR_MODE, "");
			state.effect = json.getString(HueKeys.EFFECT, "none");
			state.alert = json.getString(HueKeys.ALERT, "none");
			return state;
		}

		public void SendRequest(WWWWrapper request, Action<string> successCallback, Action<List<HueErrorInfo>> errorCallback = null)
		{
			StartCoroutine(SendRequestEnumerator(request, successCallback, errorCallback));
		}

		#endregion
	}
}