using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityHue;
using gilligames;

namespace UnityHue.Examples
{
	public class HueUIRepresentation : MonoBehaviour
	{
		public Image swatch;
		public Text nameText;
		public Toggle onToggle;
		public Slider hueSlider;
		public Slider brightnessSlider;
		public Slider saturationSlider;
		public Slider transitionTime;
		public Toggle colorLoopToggle;
		public GameObject[] colorEffectObjects;

		new private HueLight light;

		public void Initialize(HueLight light)
		{
			this.light = light;
			nameText.text = light.name + "\n" + light.type + "\n" + light.modelID;

			onToggle.SetValue(light.state.on);
			hueSlider.SetValue(light.state.hue);
			brightnessSlider.SetValue(light.state.brightness);
			saturationSlider.SetValue(light.state.saturation);
			transitionTime.SetValue(light.state.transitionTime);

			colorLoopToggle.SetValue(light.state.effect != "none");

			hueSlider.gameObject.SetActive(light.isColor);
			saturationSlider.gameObject.SetActive(light.isColor);
			brightnessSlider.gameObject.SetActive(light.isDimmable);

			if (!light.isColor)
			{
				foreach (GameObject button in colorEffectObjects)
				{
					button.SetActive(false);
				}
			}

			setSwatchColor();

			StartCoroutine(toggleWithSound());
		}

		public void SetState()
		{
			if (light == null)
			{
				return;
			}

			light.state.on = onToggle.isOn;
			light.state.brightness = (int)brightnessSlider.value;
			light.state.hue = (int)hueSlider.value;
			light.state.saturation = (int)saturationSlider.value;
			light.state.transitionTime = (int)transitionTime.value;

			light.SetState();
		}

		public void SetColorLoop(bool isOn)
		{
			if (light == null)
			{
				return;
			}

			light.state.effect = (isOn ? "colorloop" : "none");
		}

		public void SetBlink()
		{
			if (light == null)
			{
				return;
			}
			light.SetState(HueParameters.AlertParameter(HueKeys.LSELECT));
		}

		public void SetRacingColors(float timeOut = 1f)
		{
			if (light == null)
			{
				return;
			}
			StartCoroutine(RacingCountdown(timeOut));
		}

		IEnumerator RacingCountdown(float timeOut = 1f)
		{
			//Change the color instantly (no transition time)
			light.SetColor(Color.red, HueParameters.TransitionParameter(0));
			yield return new WaitForSeconds(timeOut);
			light.SetColor(Color.yellow, HueParameters.TransitionParameter(0));
			yield return new WaitForSeconds(timeOut);
			light.SetColor(Color.green, HueParameters.TransitionParameter(0));
		}

		public void brightnessSliderChanged(float value)
		{
			light.state.brightness = Mathf.RoundToInt(value);
			setSwatchColor();
		}

		public void hueSliderChanged(float value)
		{
			light.state.hue = Mathf.RoundToInt(value);
			setSwatchColor();
		}

		public void saturationSliderChanged(float value)
		{
			light.state.saturation = Mathf.RoundToInt(value);
			setSwatchColor();
		}

		public void onToggleChanged(bool isOn)
		{
			light.state.on = isOn;
			setSwatchColor();
		}

		private void setSwatchColor()
		{
			if (light == null)
			{
				swatch.color = Color.black;
			}
			else
			{
				swatch.color = light.getRGB();
			}
		}

		private IEnumerator toggleWithSound()
		{
			while (true)
			{
				onToggle.isOn = !onToggle.isOn;

				light.SetState(new KeyValuePair<string, object>(HueKeys.ON, onToggle.isOn));

				yield return new WaitForSeconds(5.0f);
			}
		}
	}
}