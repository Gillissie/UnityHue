﻿using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using gilligames;

namespace UnityHue
{
	[System.Serializable]
	public class HueGroup
	{
		public string name;
		public string id;

		public HueGroup(string name, string id)
		{
			this.name = name;
			this.id = id;
		}

		public void SetState(params KeyValuePair<string, object>[] parameters)
		{
			SetState(null, null, parameters);
		}

		public void SetName(string newGroupName, Action<string> successCallback = null,
			Action<HueErrorInfo> errorCallback = null)
		{
			ModifyGroup(successCallback, errorCallback, newGroupName);
		}

		public void SetLights(Action<string> successCallback,
			Action<HueErrorInfo> errorCallback, params HueLamp[] lamps)
		{
			ModifyGroup(successCallback, errorCallback, null, lamps);
		}

		/// <summary>
		/// Modifies the group name and lights array both at once 
		/// If you just want to modify one property set newGroupName
		/// to null or don't supply any hue lamps (See SetName or SetLights)
		/// </summary>
		/// <param name="successCallback">Success callback.</param>
		/// <param name="errorCallback">Error callback.</param>
		/// <param name="newGroupName">New group name.</param>
		/// <param name="lamps">Lamps.</param>
		public void ModifyGroup(Action<string> successCallback, Action<HueErrorInfo> errorCallback,
			string newGroupName = null, params HueLamp[] lamps)
		{
			string url = HueBridge.instance.BaseURLWithUserName + "/groups/" + id;
			var list = new List<string>();
			foreach (var item in lamps)
			{
				list.Add(item.id);
			}

			var body = new Dictionary<string, object>();
			if (!string.IsNullOrEmpty(newGroupName))
			{
				body[HueKeys.NAME] = newGroupName;
			}
			if (list.Count > 0)
			{
				body[HueKeys.LIGHTS] = list;
			}

			var www = new WWWWrapper(url, body, method: HTTPMethod.PUT);

			HueBridge.instance.SendRequest(www, successCallback, errorCallback);
		}

		public void SetState(Action<string> successCallback,
		                     Action<HueErrorInfo> errorCallback,
		                     params KeyValuePair<string, object>[] parameters
		                    )
		{
			string url = string.Format("{0}/groups/{1}/action", HueBridge.instance.BaseURLWithUserName, id);

			var body = new Dictionary<string, object>();
			foreach (var kvp in parameters)
			{
				body.Add(kvp.Key, kvp.Value);
			}

			var www = new WWWWrapper(url, body, method: HTTPMethod.PUT);

			HueBridge.instance.SendRequest(www, successCallback, errorCallback);
		}

		/// <summary>
		/// Deletes the group.
		/// </summary>
		public void Delete()
		{
			HueBridge.instance.DeleteGroup(id, HueErrorInfo.LogError);
		}
		public static void CreateHueGroup(Action<string> succesCallback, Action<HueErrorInfo> errorCallback,
			string groupName, params HueLamp[] lamps)
		{
			var list = new List<string>();
			foreach (var item in lamps)
			{
				list.Add(item.id);
			}
			CreateHueGroup(succesCallback, errorCallback, groupName, list);
		}

		public static void CreateHueGroup(Action<string> sucessCallback, Action<HueErrorInfo> errorCallback,
			string groupName, List<string> ids)
		{
			string url = HueBridge.instance.BaseURLWithUserName + "/groups";

			var body = new Dictionary<string, object>()
			{
				{ HueKeys.NAME, groupName },
				{ HueKeys.LIGHTS, ids }
			};

			var www = new WWWWrapper(url, body);

			//UnityWebRequest stateRequest = UnityWebrequestHelper.NonURLEncodedPost(url,
				//JsonHelper.CreateJsonParameterString(
				//	new JsonParameter(HueKeys.NAME, groupName),
				//	new JsonParameter(HueKeys.LIGHTS, ids as object)
				//));
			
			HueBridge.instance.SendRequest(www, sucessCallback, errorCallback);
		}
	}
}