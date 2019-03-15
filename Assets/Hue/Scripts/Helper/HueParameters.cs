using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityHue{
	/// <summary>
	/// Commonly used parameters for Hue API calls
	/// </summary>
	public static class HueParameters {

		/// <summary>
		/// Transforms a RGB into color into the corresponding hue, brightness and saturation
		/// parameters for the Hue light.
		/// </summary>
		/// <param name="color">Color.</param>
		/// <param name="hue">Hue.</param>
		/// <param name="saturation">Saturation.</param>
		/// <param name="brightness">Brightness.</param>
		public static void ColorValues(Color color, out int hue, out int saturation, out int brightness)
		{
			Vector3 hsv = HueLight.HueHSVfromRGB(color);
			hue = Mathf.RoundToInt(hsv.x);
			saturation = Mathf.RoundToInt(hsv.y);
			brightness = Mathf.RoundToInt(hsv.z);
		}

		public static KeyValuePair<string, object> LightOnParameter(bool on)
		{
			return new KeyValuePair<string, object>(HueKeys.ON, on);
		}	
		public static KeyValuePair<string, object> BrightnessParameter(int brightness)
		{
			return new KeyValuePair<string, object>(HueKeys.BRIGHTNESS, brightness);
		}
		public static KeyValuePair<string, object> HueParameter(int hue)
		{
			return new KeyValuePair<string, object>(HueKeys.HUE, hue);
		}
		public static KeyValuePair<string, object> SaturationParameter(int sat)
		{
			return new KeyValuePair<string, object>(HueKeys.SATURATION, sat);
		}
		/// <summary>
		/// Creates a transitiontime parameter. This sets the duration of the transition
		/// between the current and the new state as a multiple of 100 ms so the default
		/// transitionTime of 4 results in a 400ms transition
		/// </summary>
		/// <returns>The parameter.</returns>
		/// <param name="transitionTime">Transition time.</param>
		public static KeyValuePair<string, object> TransitionParameter(int transitionTime = 4)
		{
			return new KeyValuePair<string, object>(HueKeys.TRANSITION, transitionTime);
		}
		/// <summary>
		/// Creates an effect parameter. Options currently are "none" and "colorloop" cycling 
		/// through the hue range with current brightness and saturation
		/// </summary>
		/// <returns>The parameter.</returns>
		/// <param name="alertType">Alert type.</param>
		public static KeyValuePair<string, object> EffectParameter(string effectType = HueKeys.COLOR_LOOP)
		{
			return new KeyValuePair<string, object>(HueKeys.EFFECT, effectType);
		}
		/// <summary>
		/// Creates an alert parameter. Options currently are "none", "select" performing one 
		/// breath cycle and "lselect" performing breath cycles for 15 seconds
		/// </summary>
		/// <returns>The parameter.</returns>
		/// <param name="alertType">Alert type.</param>
		public static KeyValuePair<string, object> AlertParameter(string alertType = HueKeys.SELECT)
		{
			return new KeyValuePair<string, object>(HueKeys.ALERT, alertType);
		}
	}
}
