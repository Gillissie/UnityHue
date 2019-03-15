using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
Unity's UI elements will fire the onValueChanged event even if the value is changed in code.
Sometimes that's a problem, so these extensions allow changing the value without firing the event.
*/

namespace gilligames
{
	public static class UIExtensions
	{
		static Slider.SliderEvent emptySliderEvent = new Slider.SliderEvent();
		public static void SetValue(this Slider instance, float value)
		{
			var originalEvent = instance.onValueChanged;
			instance.onValueChanged = emptySliderEvent;
			instance.value = value;
			instance.onValueChanged = originalEvent;
		}

		static Toggle.ToggleEvent emptyToggleEvent = new Toggle.ToggleEvent();
		public static void SetValue(this Toggle instance, bool value)
		{
			var originalEvent = instance.onValueChanged;
			instance.onValueChanged = emptyToggleEvent;
			instance.isOn = value;
			instance.onValueChanged = originalEvent;
		}

		static InputField.OnChangeEvent emptyInputFieldEvent = new InputField.OnChangeEvent();
		public static void SetValue(this InputField instance, string value)
		{
			var originalEvent = instance.onValueChanged;
			instance.onValueChanged = emptyInputFieldEvent;
			instance.text = value;
			instance.onValueChanged = originalEvent;
		}

		// TODO: Add more UI types here.
	}
}