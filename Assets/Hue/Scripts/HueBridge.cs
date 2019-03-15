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
		public List<HueBridgeInfo> Bridges { get; private set; }

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
				x =>
				{
					Bridges = x;
					if (Bridges.Count > 0)
						currentBridge = Bridges[0];
					if (onFinished != null)
						onFinished();
				},
				errorCallback));
		}

		public void UpdateLights(Action onFinished = null)
		{
			UpdateLights(onFinished, HueErrorInfo.LogError);
		}

		public void UpdateLights(Action onFinished, Action<HueErrorInfo> errorCallback)
		{
			DiscoverLights(x =>
			{
				lamps = x;
				if (onFinished != null)
					onFinished();
			}, errorCallback);
		}

		public void UpdateGroups(Action onFinished = null)
		{
			UpdateGroups(onFinished, HueErrorInfo.LogError);
		}

		public void UpdateGroups(Action onFinished, Action<HueErrorInfo> errorCallback)
		{
			DiscoverGroups(x =>
			{
				groups = x;
				if (onFinished != null)
					onFinished();
			}, errorCallback);
		}

		public void DiscoverLights(Action<List<HueLamp>> lampsCallback, Action<HueErrorInfo> errorCallback)
		{
			StartCoroutine(DiscoverLightsEnumerator(lampsCallback, errorCallback));
		}

		public void DiscoverGroups(Action<List<HueGroup>> groupsCallBack, Action<HueErrorInfo> errorCallback)
		{
			StartCoroutine(DiscoverGroupsEnumerator(groupsCallBack, errorCallback));
		}

		public void CreateUser(Action onFinished = null, Action<HueErrorInfo> errorCallback = null)
		{
			CreateUser((x) =>
			{
				currentBridge.userName = x;
				if (onFinished != null)
					onFinished();
			}, errorCallback);
		}

		public void CreateUser(string applicationName, string deviceName, Action onFinished = null, Action<HueErrorInfo> errorCallback = null)
		{
			CreateUser(applicationName, deviceName,
				(x) =>
				{
					currentBridge.userName = x;
					if (onFinished != null)
					{
						onFinished();
					}
				}, errorCallback);
		}

		public void CreateUser(Action<string> generatedUsername, Action<HueErrorInfo> errorCallback)
		{
			CreateUser(currentBridge.applicationName, currentBridge.deviceName, generatedUsername, errorCallback);
		}

		public void CreateUser(string applicationName, string deviceName, Action<string> generatedUsername,
			Action<HueErrorInfo> errorCallback)
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

		public void UpdateLamp(string id, HueLamp lampToUpdate, Action<HueErrorInfo> errorCallback = null)
		{
			StartCoroutine(UpdateLampEnumerator(id, lampToUpdate, errorCallback));
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
			return new StoredHueInfo(currentBridge, Bridges);
		}

		public void RestoreHueState(StoredHueInfo savedState)
		{
			if (savedState == null)
				return;
			Bridges = savedState.allBridges;
			currentBridge = savedState.current;
		}

		#endregion

		#region Private Functions

		void ProcessBridges(string jsonResponse, Action<List<HueBridgeInfo>> ipCallback)
		{
			// The bridges JSON response doesn't have an outer object, but the JSON class needs one, so we add it before parsing here.
			jsonResponse = string.Format("{{ \"bridges\": {0} }}", jsonResponse);

			Debug.LogFormat("Bridges: {0}", jsonResponse);

			var response = new JSON(jsonResponse);

			var list = new List<HueBridgeInfo>();

			if (response.isValid)
			{
				var responseList = response.getJsonArray("bridges");
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
			}
			ipCallback(list);
		}

		void ProcessLights(string jsonResponse, Action<List<HueLamp>> lampsCallback, Action<HueErrorInfo> errorCallback)
		{
			Debug.LogFormat("Lights: {0}", jsonResponse);

			var response = new JSON(jsonResponse);

			if (!response.isValid)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(null, jsonResponse));
				}
				return;
			}

			if (response.hasKey("error"))
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(response));
				}
				return;
			}

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

		void ProcessGroups(string jsonResponse, Action<List<HueGroup>> groupCallBack, Action<HueErrorInfo> errorCallback)
		{
			Debug.LogFormat("Groups: {0}", jsonResponse);
			var response = new JSON(jsonResponse);
			if (!response.isValid)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(null, jsonResponse));
				}
				return;
			}

			if (response.hasKey(HueKeys.ERROR))
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(response));
				}
				return;
			}

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

		void ProcessLampUpdate(string jsonResponse, HueLamp lampToUpdate, Action<HueErrorInfo> errorCallback)
		{
			var response = new JSON(jsonResponse);
			if (!response.isValid)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(null, jsonResponse));
				}
				return;
			}

			if (response.hasKey(HueKeys.ERROR))
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(response));
				}
				return;
			}
			ProcessLampUpdate(response, lampToUpdate);
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

		IEnumerator GetBridgesEnumerator(Action<List<HueBridgeInfo>> ipCallback, Action<HueErrorInfo> errorCallback = null)
		{
			UnityWebRequest bridgesWebRequest = UnityWebRequest.Get(hueDiscoveryServer);

			yield return bridgesWebRequest.Send();

			if (bridgesWebRequest.isNetworkError)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(bridgesWebRequest.error, null));
				}
			}
			else
			{
				ProcessBridges(bridgesWebRequest.downloadHandler.text, ipCallback);
			}
		}

		IEnumerator CreateUserEnumerator(string applicationName, string deviceName,
			Action<string> generatedUserName, Action<HueErrorInfo> errorCallback = null)
		{
			var body = new Dictionary<string, object>()
			{
				{ HueKeys.DEVICE_TYPE, string.Format("{0}#{1}", applicationName, deviceName) }
			};

			var www = new WWWWrapper(BaseURL, body);
			yield return www.waitToFinish();

			JSON response = www.responseJSON;

			if (response == null)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(www.error, null));
				}
			}
			else
			{
//				Debug.LogFormat("Create User: {0}", jsonResponse);
				if (response.hasKey(HueKeys.ERROR))
				{
				    if (errorCallback != null)
					{
						errorCallback(new HueErrorInfo(response));
					}
				}
				else
				{
					var userName = response.getString(HueKeys.USER_NAME, "");
					if (userName != "")
					{
						generatedUserName(userName);
					}
				}
			}
		}

		IEnumerator DeleteLampEnumerator(string id, Action<HueErrorInfo> errorCallback = null)
		{
			string url = BaseURLWithUserName + "/lights/" + id;
			UnityWebRequest deleteRequest = UnityWebRequest.Delete(url);
			yield return deleteRequest.Send();
			if (deleteRequest.isNetworkError && errorCallback != null)
			{
				errorCallback(new HueErrorInfo(deleteRequest.error, null));
			}
		}
		IEnumerator DeleteGroupEnumerator(string id, Action<HueErrorInfo> errorCallback = null)
		{
			string url = BaseURLWithUserName + "/groups/" + id;
			UnityWebRequest deleteRequest = UnityWebRequest.Delete(url);
			yield return deleteRequest.Send();
			if (deleteRequest.isNetworkError && errorCallback != null)
			{
				errorCallback(new HueErrorInfo(deleteRequest.error, null));
			}
		}

		IEnumerator UpdateLampEnumerator(string id, HueLamp lampToUpdate,
			Action<HueErrorInfo> errorCallback = null)
		{
			string url = BaseURLWithUserName + "/lights/" + id;
			UnityWebRequest stateRequest = UnityWebRequest.Get(url);
			yield return stateRequest.Send();
			if (stateRequest.isNetworkError)
			{
				if (errorCallback != null)
					errorCallback(new HueErrorInfo(stateRequest.error, null));
			}
			else
			{
				ProcessLampUpdate(stateRequest.downloadHandler.text, lampToUpdate, errorCallback);
			}
		}

		IEnumerator DiscoverLightsEnumerator(Action<List<HueLamp>> lampsCallback, Action<HueErrorInfo> errorCallback)
		{
			UnityWebRequest lightsRequest = UnityWebRequest.Get(BaseURL + "/" + currentBridge.userName + "/lights");
			yield return lightsRequest.Send();

			if (lightsRequest.isNetworkError)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(lightsRequest.error, null));
				}
			}
			else
			{
				ProcessLights(lightsRequest.downloadHandler.text, lampsCallback, errorCallback);
			}
		}
		IEnumerator DiscoverGroupsEnumerator(Action<List<HueGroup>> groups, Action<HueErrorInfo> errorCallback)
		{
			UnityWebRequest groupsRequest = UnityWebRequest.Get(BaseURL + "/" + currentBridge.userName + "/groups");
			yield return groupsRequest.Send();

			if (groupsRequest.isNetworkError)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(groupsRequest.error, null));
				}
			}
			else
			{
				ProcessGroups(groupsRequest.downloadHandler.text, groups, errorCallback);
			}
		}

		IEnumerator SendRequestEnumerator(WWWWrapper request, Action<string> successCallback,
			Action<HueErrorInfo> errorCallback = null)
		{
			yield return request.waitToFinish();

			if (request.responseJSON == null)
			{
				if (errorCallback != null)
				{
					errorCallback(new HueErrorInfo(request.error, null));
				}
			}
			else
			{
				if (successCallback != null)
				{
					successCallback(request.responseText);
				}
			}
		}

		#endregion

		#region Helper

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