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
		protected List<HueLamp> lamps = new List<HueLamp>();
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
			DiscoverBridges(onFinished, HueErrorInfo.LogError);
		}

		public void DiscoverBridges(Action onFinished, Action<HueErrorInfo> errorCallback)
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
			UpdateLights(onFinished, HueErrorInfo.LogError);
		}

		public void UpdateLights(Action onFinished, Action<HueErrorInfo> errorCallback)
		{
			DiscoverLights(
				(lamps) =>
				{
					this.lamps = lamps;
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
			UpdateGroups(onFinished, HueErrorInfo.LogError);
		}

		public void UpdateGroups(Action onFinished, Action<HueErrorInfo> errorCallback)
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

		public void DiscoverLights(Action<List<HueLamp>> lampsCallback, Action<HueErrorInfo> errorCallback)
		{
			StartCoroutine(DiscoverLightsEnumerator(lampsCallback, errorCallback));
		}

		public void DiscoverGroups(Action<List<HueGroup>> groupsCallBack, Action<HueErrorInfo> errorCallback)
		{
			StartCoroutine(DiscoverGroupsEnumerator(groupsCallBack, errorCallback));
		}

		//public void CreateUser(Action onFinished = null, Action<HueErrorInfo> errorCallback = null)
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

		public void CreateUser(string applicationName, string deviceName, Action onFinished = null, Action<HueErrorInfo> errorCallback = null)
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

		//public void CreateUser(Action<string> generatedUsername, Action<HueErrorInfo> errorCallback)
		//{
		//	CreateUser(currentBridge.applicationName, currentBridge.deviceName, generatedUsername, errorCallback);
		//}

		public void CreateUser(string applicationName, string deviceName, Action<string> generatedUsername, Action<HueErrorInfo> errorCallback)
		{
			StartCoroutine(CreateUserEnumerator(applicationName, deviceName, generatedUsername, errorCallback));
		}

		/// <summary>
		/// Deletes the lamp, careful with this one
		/// </summary>
		/// <param name="lampName">Lamp name.</param>
		/// <param name="successCallback">Success callback.</param>
		/// <param name="errorCallback">Error callback.</param>
		public void DeleteLamp(string id, Action<HueErrorInfo> errorCallback = null)
		{
			StartCoroutine(DeleteLampEnumerator(id, errorCallback));
		}

		/// <summary>
		/// Deletes a group, careful with this one
		/// </summary>
		/// <param name="lampName">Lamp name.</param>
		/// <param name="successCallback">Success callback.</param>
		/// <param name="errorCallback">Error callback.</param>
		public void DeleteGroup(string id, Action<HueErrorInfo> errorCallback = null)
		{
			StartCoroutine(DeleteGroupEnumerator(id, errorCallback));
		}

		// Gets a lamp's current data from the bridge and updates the client's data about it.
		public void UpdateLampFromBridge(string id, HueLamp lampToUpdate, Action<HueErrorInfo> errorCallback = null)
		{
			StartCoroutine(UpdateLampFromBridgeEnumerator(id, lampToUpdate, errorCallback));
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

		void ProcessLights(JSON response, Action<List<HueLamp>> lampsCallback)
		{
			Debug.LogFormat("Lights: {0}", response);

			var list = new List<HueLamp>();
			foreach (var id in response.getKeyList())
			{
				var lightJson = response.getJSON(id);
				//var lightDict = kv.Value as Dictionary<string, object>;
				var lamp = new HueLamp(id);
				ProcessLampUpdate(lightJson, lamp);
				list.Add(lamp);
			}
			lampsCallback(list);
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

		void ProcessLampUpdate(JSON json, HueLamp lampToUpdate)
		{
			lampToUpdate.name = json.getString(HueKeys.NAME, "");
			lampToUpdate.modelID = json.getString(HueKeys.MODEL_ID, "");
			lampToUpdate.type = json.getString(HueKeys.TYPE, "");
			lampToUpdate.softwareVersion = json.getString(HueKeys.SOFTWARE_VERSION, "");
			var stateJson = json.getJSON(HueKeys.STATE);
			lampToUpdate.lampState = GetStateFromJSON(stateJson);
		}

		#endregion

		#region Request Enumerators

		IEnumerator GetBridgesEnumerator(Action<List<HueBridgeInfo>> bridgesCallback, Action<HueErrorInfo> errorCallback = null)
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
			Action<HueErrorInfo> errorCallback = null
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

		IEnumerator DeleteLampEnumerator(string id, Action<HueErrorInfo> errorCallback = null)
		{
			string url = string.Format("{0}/lights/{1}", BaseURLWithUserName, id);
			yield return DeleteEnumerator(url, errorCallback);
		}

		IEnumerator DeleteGroupEnumerator(string id, Action<HueErrorInfo> errorCallback = null)
		{
			string url = string.Format("{0}/groups/{1}", BaseURLWithUserName, id);
			yield return DeleteEnumerator(url, errorCallback);
		}

		IEnumerator DeleteEnumerator(string url, Action<HueErrorInfo> errorCallback = null)
		{
			var www = new WWWWrapper(url, method: HTTPMethod.DELETE);
			yield return www.waitToFinish();
			isValidJsonResponse(www, errorCallback);	// Just check for errors.
		}


		IEnumerator UpdateLampFromBridgeEnumerator(string id, HueLamp lampToUpdate, Action<HueErrorInfo> errorCallback = null)
		{
			string url = string.Format("{0}/lights/{1}", BaseURLWithUserName, id);

			var www = new WWWWrapper(url);
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback))
			{
				ProcessLampUpdate(www.responseJSON, lampToUpdate);
			}
		}

		IEnumerator DiscoverLightsEnumerator(Action<List<HueLamp>> lampsCallback, Action<HueErrorInfo> errorCallback)
		{
			string url = string.Format("{0}/{1}/lights", BaseURL, currentBridge.userName);

			var www = new WWWWrapper(url);
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback))
			{
				ProcessLights(www.responseJSON, lampsCallback);
			}
		}

		IEnumerator DiscoverGroupsEnumerator(Action<List<HueGroup>> groupsCallback, Action<HueErrorInfo> errorCallback)
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
			Action<HueErrorInfo> errorCallback = null)
		{
			yield return www.waitToFinish();

			if (isValidJsonResponse(www, errorCallback) && successCallback != null)
			{
				successCallback(www.responseText);
			}
		}

		#endregion

		#region Helper

		private bool isValidJsonResponse(WWWWrapper www, System.Action<HueErrorInfo> errorCallback)
		{
			JSON response = www.responseJSON;

			if (response == null)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(www.error, null));
				}
				return false;
			}
			else if (!response.isValid)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(null, www.responseText));
				}
				return false;
			}
			else
			{
				if (response.hasKey("rootArray"))
				{
					foreach (JSON json in response.getJsonArray("rootArray"))
					{
						if (json.hasKey(HueKeys.ERROR))
						{
							if (errorCallback != null)
							{
								errorCallback(new HueErrorInfo(json));
							}
							return false;
						}
					}
				}
				else if (response.hasKey(HueKeys.ERROR))
				{
					if (errorCallback != null)
					{
						errorCallback(new HueErrorInfo(response));
					}
					return false;
				}
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
		public List<HueLamp> Lights
		{
			get
			{
				return lamps;
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

		HueLampState GetStateFromJSON(JSON json)
		{
			var state = new HueLampState();
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

		public void SendRequest(WWWWrapper request, Action<string> successCallback, Action<HueErrorInfo> errorCallback = null)
		{
			StartCoroutine(SendRequestEnumerator(request, successCallback, errorCallback));
		}

		#endregion
	}
}