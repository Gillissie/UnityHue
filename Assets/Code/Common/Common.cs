using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/*
Common, reusable functions.
*/

namespace gilligames
{
	public static class Common
	{
		public const int SECONDS_PER_MINUTE = 60;
		public const int SECONDS_PER_HOUR = SECONDS_PER_MINUTE * 60;
		public const int SECONDS_PER_DAY = SECONDS_PER_HOUR * 24;

		public static int getClientTimestamp()
		{
			return (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
		}

		// Instantiates a game object then finds a component of the given type to return.
		public static T instantiateWithComponent<T>(GameObject prefab, Transform parent, bool isActive = true)
		{
			GameObject go = GameObject.Instantiate(prefab, parent);
			go.SetActive(isActive);
			T comp = go.GetComponent<T>();
			if (comp == null)
			{
				Debug.LogWarningFormat("Component {0} not found on prefab {1}. Created object has been destroyed.", typeof(T).Name, prefab.name);
				GameObject.Destroy(go);
			}
			return comp;
		}

		// Standard function for creating an object from a template, typically for creating a layout object
		// at the same parent level as the template object. The template object is often inactive,
		// so we activate the new object.
		public static T createItemForLayout<T>(MonoBehaviour templateObject, List<T> itemsList) where T : MonoBehaviour
		{
			GameObject go = GameObject.Instantiate(templateObject.gameObject, templateObject.transform.parent);
			T item = go.GetComponent<T>();
			go.SetActive(true);
			if (itemsList != null)
			{
				itemsList.Add(item);
			}
			return item;
		}

		// Standard function for destroying all items in a list created using createItemForLayout().
		public static void destroyLayoutItems<T>(List<T> items) where T : MonoBehaviour
		{
			while (items.Count > 0)
			{
				GameObject.Destroy(items[0].gameObject);
				items.RemoveAt(0);
			}
		}

		public static EnumType parseEnum<EnumType>(string stringValue, EnumType defaultValue, bool warnIfNotFound = true)
		{
			EnumType returnValue = defaultValue;
			try
			{
				returnValue = (EnumType)System.Enum.Parse(typeof(EnumType), stringValue);
			}
			catch
			{
				if (warnIfNotFound)
				{
					Debug.LogWarningFormat("Invalid {0} enum: {1}", typeof(EnumType).Name, stringValue);
				}
			}

			return returnValue;
		}

		public static IEnumerable<T> GetEnumValues<T>()
		{
			return System.Enum.GetValues(typeof(T)).Cast<T>();	// Requires Linq.
		}

		public static float round(float val, int decimalPlaces)
		{
			return (float)System.Math.Round((float)val, decimalPlaces);
		}

		public static double round(double val, int decimalPlaces)
		{
			return System.Math.Round(val, decimalPlaces);
		}

		// Since Unity's Round function is a bit weird, here's a better one.
		public static int roundToInt(float val)
		{
			return Mathf.FloorToInt(val + 0.5f);
		}

		public static long roundToLong(double doubleVal)
		{
			return (long)(doubleVal + 0.5);
		}

		public static Color setAlpha(Color color, float alpha)
		{
			color.a = alpha;
			return color;
		}

		public static void setAlpha(this Image image, float alpha)
		{
			image.color = setAlpha(image.color, alpha);
		}

		public static void setAlpha(this SpriteRenderer sprite, float alpha)
		{
			sprite.color = setAlpha(sprite.color, alpha);
		}

		// Returns a value between min and max based on the current time
		// and the speed in a constant growth and decay function (sine)
		public static float pulsateBetween(float min, float max, float speed, float offset = 0f)
		{
			offset = Mathf.Clamp01(offset);

			return (FastTrig.sin(Time.time * speed + (offset * 2f * Mathf.PI)) + 1f) * 0.5f * (max - min) + min;
		}

		// Returns a solid color generated from some hex string, e.g. "0f0f0f"
		public static Color colorFromHex(string hex)
		{
			if (hex.Length == 6)
			{
				// Use full alpha if not specified.
				hex += "FF";
			}
			else if (hex.Length != 8)
			{
				Debug.LogWarning("Invalid hex string for color: " + hex);
				return Color.grey;
			}

			int colorBits = 0;

			try
			{
				colorBits = System.Convert.ToInt32(hex, 16);
			}
			catch
			{
				Debug.LogWarning("Invalid hex string for color: " + hex);
				return Color.grey;
			}

			float r = Mathf.Clamp01((float)((colorBits & 0xFF000000) >> 24) / 255f);
			float g = Mathf.Clamp01((float)((colorBits & 0x00FF0000) >> 16) / 255f);
			float b = Mathf.Clamp01((float)((colorBits & 0x0000FF00) >> 8) / 255f);
			float a = Mathf.Clamp01((float)(colorBits & 0x000000FF) / 255f);
			return new Color(r, g, b, a);
		}

		public static string colorToHex(Color color, bool doIncludeAlpha = false)
		{
			return string.Format("{0}{1}{2}{3}",
				Mathf.RoundToInt(color.r * 255.0f).ToString("X2"),
				Mathf.RoundToInt(color.g * 255.0f).ToString("X2"),
				Mathf.RoundToInt(color.b * 255.0f).ToString("X2"),
				doIncludeAlpha ? Mathf.RoundToInt(color.a * 255.0f).ToString("X2") : ""
			);
		}

		// Friendly string representation of time, such as "2:30" for 150 seconds.
		public static string secondsFormatted(int totalSeconds)
		{
			System.TimeSpan t = System.TimeSpan.FromSeconds(totalSeconds);

			if (t.Days > 0)
			{
				// (e.g. 4d:5:01:08)	(matches format from Flash version)
				return string.Format("{0}{1}:{2}:{3:00}:{4:00}",
					t.Days,
					"D",
					t.Hours,
					t.Minutes,
					t.Seconds
				);
			}
			else if (t.Hours > 0)
			{
				// (e.g. 5:01:08)
				return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
			}

			return string.Format("{0:00}:{1:00}", t.Minutes, t.Seconds);
		}

		/// <summary>
		/// Gets a value from a dictionary, or the default value if the key doesn't exist.
		/// </summary>
		/// <returns>The value.</returns>
		/// <param name="dict">Dict.</param>
		/// <param name="defaultValue">Default value if the key doesn't exist.</param>
		public static T getValueWithDefault<T>(this Dictionary<string, object> dict, string key, T defaultValue)
		{
			if (dict.ContainsKey(key))
			{
				try
				{
					return (T)dict[key];
				}
				catch
				{
					// Ignore the casting error and just return the default value.
				}
			}
			return defaultValue;
		}
		
		// Works in C#3/VS2008:
		// Returns a new dictionary of this ... others merged leftward.
		// Keeps the type of 'this', which must be default-instantiable.
		// Example: 
		//   result = map.MergeLeft(other1, other2, ...)
		public static T MergeLeft<T, K, V>(this T me, params IDictionary<K, V>[] others)
			where T : IDictionary<K, V>, new()
		{
			T newMap = new T();
			foreach (IDictionary<K, V> src in
				(new List<IDictionary<K, V>> { me }).Concat(others))
			{
				// ^-- echk. Not quite there type-system.
				foreach (KeyValuePair<K, V> p in src)
				{
					newMap[p.Key] = p.Value;
				}
			}
			return newMap;
		}
		
		// Since HashSet doesn't have a built-in AddRange, I made my own.
		public static void AddRange<T>(this HashSet<T> hashSet, T[] items)
		{
			foreach (T item in items)
			{
				hashSet.Add(item);
			}
		}

		public static void AddRange<T>(this HashSet<T> hashSet, List<T> items)
		{
			foreach (T item in items)
			{
				hashSet.Add(item);
			}
		}

		// Sets an object and all its children to a given layer.
		public static void setLayerRecursively(GameObject parent, int newLayer)
		{
			foreach (Transform tran in parent.GetComponentsInChildren<Transform>())
			{
				tran.gameObject.layer = newLayer;
			}
		}

		// Returns the string as all lower case with the first character capitalized.
		public static string capitalizeString(string text)
		{
			if (text.Length == 0)
			{
				return "";
			}

			if (text.Length == 1)
			{
				return text.ToUpper();
			}

			return string.Format("{0}{1}", text.Substring(0, 1).ToUpper(), text.Substring(1));
		}

		// Abbreviates a number.
		public static string abbreviateNumber(double value, int decimals = 2, bool doStripTrailingZeros = true, long minAbbreviatedValue = 10000L)
		{
			double abbrValue;
			double divisor;
			string letter = "";


			if (value < minAbbreviatedValue)
			{
				// Don't abbreviate this number that isn't big enough.
				return formatNumber(value, decimals, doStripTrailingZeros);
			}

			if (value < 1000000L)
			{
				// Less than a million, abbreviate as K.
				letter = "K";
				divisor = 1000.0;
			}
			else if (value < 1000000000L)
			{
				// Less than a billion, abbreviate as M.
				letter = "M";
				divisor = 1000000.0;
			}
			else
			{
				// A billion or higher, abbreviate as B.
				letter = "B";
				divisor = 1000000000.0;
			}

			abbrValue = (double)value / divisor;

			return string.Format("{0}{1}", formatNumber(abbrValue, decimals, doStripTrailingZeros), letter);
		}

		public static string formatNumber(int value)
		{
			return value.ToString("n0");
		}

		public static string formatNumber(long value)
		{
			return value.ToString("n0");
		}

		public static string formatNumber(float value, int decimalPrecision, bool doStripTrailingZeros = false)
		{
			string output = value.ToString(string.Format("n{0}", decimalPrecision));

			if (doStripTrailingZeros)
			{
				return stripTrailingZeros(output);
			}
			return output;
		}

		public static string formatNumber(float value)
		{
			return value.ToString(string.Format("n"));
		}

		public static string formatNumber(double value, int decimalPrecision, bool doStripTrailingZeros = false)
		{
			string output = value.ToString(string.Format("n{0}", decimalPrecision));

			if (doStripTrailingZeros)
			{
				return stripTrailingZeros(output);
			}
			return output;
		}

		public static string formatNumber(double value)
		{
			return value.ToString(string.Format("n"));
		}

		public static string formatNumber(decimal value)
		{
			return formatNumber((double)value);
		}

		// Strips trailing zeros after a decimal point.
		public static string stripTrailingZeros(string valueString)
		{
			// First see what the decimal character is.
			string decChar = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

			if (!valueString.Contains(decChar))
			{
				return valueString;
			}

			while ((valueString.EndsWith("0") && valueString.Contains(decChar)) || valueString.EndsWith(decChar))
			{
				valueString = valueString.Substring(0, valueString.Length - 1);
			}

			return valueString;
		}

		// Sets a trigger and clears all other pending triggers that are stuck.
		// This is a workaround for what should be considered a but in Unity,
		// but apparently it's by design. Unity keeps triggers triggered
		// when they are set at a point where they aren't needed,
		// so they end up triggering things later unexpectedly.
		public static void setTriggerClean(this Animator anim, string trigger)
		{
			foreach (AnimatorControllerParameter p in anim.parameters)
			{
				if (p.type == AnimatorControllerParameterType.Trigger)
				{
					anim.ResetTrigger(p.name);
				}
			}
			anim.SetTrigger(trigger);
		}

		// Returns a random integer inclusive of the max (unlike Unity's Random int function).
		static public int random(int min, int max)
		{
			if (min == max)
			{
				return min;
			}
			return Random.Range(min, max + 1);
		}

		// For consistency's sake, make a float-based Random function here too so all random numbers use Common.random().
		static public float random(float min, float max)
		{
			return Random.Range(min, max);
		}

		public static int getWeightedRandomIndex(List<int> weights)
		{
			int total = 0;

			foreach (int weight in weights)
			{
				total += weight;
			}

			int tally = 0;
			int chosen = random(1, total);	// Max value is inclusive from this function.

			for (int i = 0; i < weights.Count; i++)
			{
				tally += weights[i];
				if (chosen <= tally)
				{
					return i;
				}
			}

			// This should never happen.
			return -1;
		}

		// A version of Mathf.Lerp() that supports double types.
		public static double lerp(double a, double b, float t)
		{
			b -= a;
			return a + (double)t * b;
		}
		
		// A version of Mathf.Lerp() that supports long types.
		public static long lerp(long a, long b, float t)
		{
			b -= a;
			return a + (long)((double)t * b);
		}

		// Returns the UI canvas position of an object based on a given camera,
		// with 0,0 being the center of the screen and positive Y being the top, for UI purposes.
		public static Vector2 uiPositionOfWorld(Camera worldCamera, Camera uiCamera, RectTransform canvasRectTransform, Vector3 worldPosition, int offsetX = 0, int offsetY = 0)
		{
			// validate camera to ensure against bad implementations
			if (worldCamera == null)
			{
				Debug.LogError("The worldCamera passed in is Null.");
				return Vector2.zero;
			}

			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);

			return uiPositionOfScreen(screenPoint, uiCamera, canvasRectTransform, offsetX, offsetY);
		}

		// Returns the UI canvas position of a screen pixel point.
		public static Vector2 uiPositionOfScreen(Vector2 screenPoint, Camera theCamera, RectTransform canvasRectTransform, int offsetX = 0, int offsetY = 0)
		{
			Vector2 rectPoint;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPoint, theCamera, out rectPoint);

			return new Vector2(Mathf.Round(rectPoint.x + offsetX), Mathf.Round(rectPoint.y + offsetY));
		}

		public static void copyToClipboard(this string s)
		{
			TextEditor te = new TextEditor();
			te.text = s;
			te.SelectAll();
			te.Copy();
		}

		// Prepares a 3D object for UI masking by replacing the shaders with the maskable one,
		// and then enabling the clipping mask.
		// This works in conjunction with UI3DDepthObject and UI3DCanvas.
		public static void enable3DUIMask(MonoBehaviour component)
		{
			Renderer rend = component.GetComponent<Renderer>();
			Material[] mats = rend.materials;

			for (int i = 0; i < mats.Length; i++)
			{
				//				Material mat = new Material(mats[i]);
				Material mat = mats[i];

				// This line could be more dynamic by checking the existing shader name
				// and choosing one of the various shaders based on that.
				//mat.shader = Shader.Find("MODev/UI3D/Standard");

				mat.EnableKeyword("USE_CLIPPING_MASK");
				mat.EnableKeyword("USE_CLIPPING_MASK_ON");

				//if (mat.GetTexture(Shader.PropertyToID("_BumpMap")) != null)
				//{
				//	mat.EnableKeyword("USE_BUMP_MAP");
				//	mat.SetFloat("_Bumpiness", mats[i].GetFloat("_BumpScale"));
				//}

				//mat.SetFloat("_Glossiness", mats[i].GetFloat("_Glossiness"));
				//mat.SetFloat("_Metallic", mats[i].GetFloat("_Metallic"));

				mats[i] = mat;
			}

			rend.materials = mats;
		}

		public static string getBasicAuthHeader(string userName, string password)
		{
			return string.Format("Basic {0}", string.Format("{0}:{1}", userName, password).Base64Encode());
		}

		public static string Base64Encode(this string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(plainTextBytes);
		}

		public static string Base64Decode(this string base64EncodedData)
		{
			var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		}
		
		public static string md5(this string strToEncrypt)
		{
			System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
			byte[] bytes = ue.GetBytes(strToEncrypt);

			// encrypt bytes
			System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] hashBytes = md5.ComputeHash(bytes);

			// Convert the encrypted bytes back to a string (base 16)
			string hashString = "";

			for (int i = 0; i < hashBytes.Length; i++)
			{
				hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
			}

			return hashString.PadLeft(32, '0');
		}

		public static string getPlatformString()
		{
#if UNITY_WEBGL
			return "WebGL";
#elif UNITY_IOS
			return "iOS";
#elif UNITY_ANDROID
			return "Android";
#else
			return "";
#endif
		}
	}
}