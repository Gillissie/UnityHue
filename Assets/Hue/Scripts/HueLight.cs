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
	/// Class for accessing and modifying a single Hue light
	/// </summary>
	[System.Serializable]
	public class HueLight
	{
		public string name;
		public string id;
		public string type;
		public string modelID;
		public string softwareVersion;
		public HueLightState state;
		public bool isDimmable = true;
		public bool isColor = false;

		public HueLight(string id, string name)
		{
			this.id = id;
			this.name = name;
		}

		public HueLight(string id)
		{
			this.id = id;
		}

		public void SetColor(Color color, params KeyValuePair<string, object>[] additionalParameters)
		{
			SetColor(color, null, null, additionalParameters);
		}

		public void SetColor(Color color, Action<string> successCallback,
		                     Action<List<HueErrorInfo>> errorCallback,
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

		public void UpdateLightFromBridge(Action<List<HueErrorInfo>> errorCallback = null)
		{
			HueBridge.instance.UpdateLightFromBridge(id, this, errorCallback);
		}

		public void SetState()
		{
			SetState(this.StateToParameters());
		}

		public void CopyState(HueLight fromLight)
		{
			SetState(fromLight.StateToParameters());
		}

		public void SetState(params KeyValuePair<string, object>[] parameters)
		{
			SetState(null, null, parameters);
		}

		/// <summary>
		/// Sets the state of the light according to the supplied JsonParameters
		/// A JsonParameter consists of a key (the name of the parameter as specified
		/// by the hue api i.e "bri" for brightness) and a value to which that parameter
		/// should be set. 
		/// </summary>
		/// <param name="successCallback">Success callback.</param>
		/// <param name="errorCallback">Error callback.</param>
		/// <param name="parameters">Parameters.</param>
		public void SetState(
			Action<string> successCallback,
			Action<List<HueErrorInfo>> errorCallback,
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

		public void SetName(string lightName, Action<string> successCallback = null, Action<List<HueErrorInfo>> errorCallback = null)
		{
			string url = string.Format("{0}/lights/{1}", HueBridge.instance.BaseURLWithUserName, id);

			var body = new Dictionary<string, object>()
			{
				{ HueKeys.NAME, lightName }
			};

			var www = new WWWWrapper(url, body, method: HTTPMethod.PUT);

			HueBridge.instance.SendRequest(www, successCallback, errorCallback);
		}

		public KeyValuePair<string, object>[] StateToParameters()
		{
			var list = new List<KeyValuePair<string, object>>();
			list.Add(new KeyValuePair<string, object>(HueKeys.ON, state.on));
			list.Add(new KeyValuePair<string, object>(HueKeys.ALERT, "none"));
			list.Add(new KeyValuePair<string, object>(HueKeys.TRANSITION, state.transitionTime));

			if (isDimmable)
			{
				list.Add(new KeyValuePair<string, object>(HueKeys.BRIGHTNESS, state.brightness));
			}
			if (isColor)
			{
				list.Add(new KeyValuePair<string, object>(HueKeys.EFFECT, state.effect));
				list.Add(new KeyValuePair<string, object>(HueKeys.HUE, state.hue));
				list.Add(new KeyValuePair<string, object>(HueKeys.SATURATION, state.saturation));
			}

			return list.ToArray();
		}

		/// <summary>
		/// Deletes the light.
		/// </summary>
		public void Delete()
		{
			HueBridge.instance.DeleteLight(id, HueErrorInfo.LogErrors);
		}

		private static Color fullBrightNonColor = new Color32(255, 255, 225, 255);

		// Returns the RGB color of this light.
		public Color getRGB()
		{
			if (isColor)
			{
				return Color.HSVToRGB((float)state.hue / 65535f, (float)state.saturation / 254, (float)state.brightness / 254);
			}

			// Not a color bulb, but it does have a warm tint.
			float brightMultiplier = (float)state.brightness / 254.0f;
			return new Color(fullBrightNonColor.r * brightMultiplier, fullBrightNonColor.g * brightMultiplier, fullBrightNonColor.b * brightMultiplier);
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
