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
	/// Class for accessing and modifying a single Hue lamp
	/// </summary>
	[System.Serializable]
	public class HueLamp
	{
		public string name;
		public string id;
		public string type;
		public string modelID;
		public string softwareVersion;
		public HueLampState lampState;

		public HueLamp(string id, string name)
		{
			this.id = id;
			this.name = name;
		}

		public HueLamp(string id)
		{
			this.id = id;
		}

		public void SetColor(Color color, params KeyValuePair<string, object>[] additionalParameters)
		{
			SetColor(color, null, null, additionalParameters);
		}

		public void SetColor(Color color, Action<string> successCallback,
		                     Action<HueErrorInfo> errorCallback,
		                     params KeyValuePair<string, object>[] additionalParameters
		                    )
		{
			int hue, bri, sat;
			HueParameters.ColorValues(color, out hue, out sat, out bri);
			var list = new List<KeyValuePair<string, object>>(additionalParameters);
			list.Add(new KeyValuePair<string, object>(HueKeys.HUE, hue));
			list.Add(new KeyValuePair<string, object>(HueKeys.BRIGHTNESS, bri));
			list.Add(new KeyValuePair<string, object>(HueKeys.SATURATION, sat));

			SetState(successCallback, errorCallback, list.ToArray());
		}

		public void UpdateLampFromBridge(Action<HueErrorInfo> errorCallback = null)
		{
			HueBridge.instance.UpdateLampFromBridge(id, this, errorCallback);
		}

		public void SetState()
		{
			SetState(StateToParameters(this.lampState));
		}

		public void SetState(params KeyValuePair<string, object>[] parameters)
		{
			SetState(null, null, parameters);
		}

		public void SetState(HueLampState state)
		{
			SetState(null, null, StateToParameters(state));
		}

		public void SetState(HueLampState state, Action<string> successCallback,
			Action<HueErrorInfo> errorCallback)
		{
			SetState(successCallback, errorCallback, StateToParameters(state));
		}

		/// <summary>
		/// Sets the state of the lamp according to the supplied JsonParameters
		/// A JsonParameter consists of a key (the name of the parameter as specified
		/// by the hue api i.e "bri" for brightness) and a value to which that parameter
		/// should be set. 
		/// </summary>
		/// <param name="successCallback">Success callback.</param>
		/// <param name="errorCallback">Error callback.</param>
		/// <param name="parameters">Parameters.</param>
		public void SetState(Action<string> successCallback,
		                     Action<HueErrorInfo> errorCallback,
		                     params KeyValuePair<string, object>[] parameters
		                    )
		{
			string url = string.Format("{0}/lights/{1}/state", HueBridge.instance.BaseURLWithUserName, id);

			var body = new Dictionary<string, object>();
			foreach (var kvp in parameters)
			{
				body.Add(kvp.Key, kvp.Value);
			}

			var www = new WWWWrapper(url, body, method: HTTPMethod.PUT);

			HueBridge.instance.SendRequest(www, successCallback, errorCallback);
		}

		public void SetName(string lampName, Action<string> successCallback = null, Action<HueErrorInfo> errorCallback = null)
		{
			string url = string.Format("{0}/lights/{1}", HueBridge.instance.BaseURLWithUserName, id);

			var body = new Dictionary<string, object>()
			{
				{ HueKeys.NAME, lampName }
			};

			var www = new WWWWrapper(url, body, method: HTTPMethod.PUT);

			HueBridge.instance.SendRequest(www, successCallback, errorCallback);
		}

		public static KeyValuePair<string, object>[] StateToParameters(HueLampState state)
		{
			var list = new List<KeyValuePair<string, object>>();
			list.Add(new KeyValuePair<string, object>(HueKeys.ON, state.on));
			list.Add(new KeyValuePair<string, object>(HueKeys.BRIGHTNESS, state.brightness));
			list.Add(new KeyValuePair<string, object>(HueKeys.HUE, state.hue));
			list.Add(new KeyValuePair<string, object>(HueKeys.SATURATION, state.saturation));
			list.Add(new KeyValuePair<string, object>(HueKeys.ALERT, state.alert));
			list.Add(new KeyValuePair<string, object>(HueKeys.EFFECT, state.effect));
			list.Add(new KeyValuePair<string, object>(HueKeys.TRANSITION, state.transitionTime));
			return list.ToArray();
		}

		/// <summary>
		/// Deletes the lamp.
		/// </summary>
		public void Delete()
		{
			HueBridge.instance.DeleteLamp(id, HueErrorInfo.LogError);
		}

		/// <summary>
		/// Maps an RGB color with (1, 1, 1) being complete white
		/// into hue HSV color space with hue going from 0 to 65535,
		/// saturation from 0 to 254 and brightness from 1 to 254.
		/// Keep in mind that the Hue API only accepts ints for those
		/// values.
		/// </summary>
		/// <returns>The HS vfrom RG.</returns>
		/// <param name="rgb">Rgb.</param>
		public static Vector3 HueHSVfromRGB(Color rgb)
		{
			float brightness, hue, saturation;
			Color.RGBToHSV(rgb, out hue, out saturation, out brightness);
			hue *= 65535f;
			saturation *= 254f;
			brightness *= 254f;
			return new Vector3(hue, saturation, Mathf.Max(brightness, 1f));
		}
	}
}
