using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MiniJSON;
using gilligames;

namespace UnityHue
{
	/// <summary>
	/// Stores information about the error that occured when performing an operation
	/// with Unity Hue. Can either be an error with the webrequest (webrequest error field)
	/// with the hue api (errorcode, address, description) or with unexpected json (failingJson)
	/// </summary>
	[System.Serializable]
	public class HueErrorInfo
	{
		public string webrequestError;
		/// <summary>
		/// The error code, in case this was a hue api error. A list with error codes can be
		/// found here: http://www.developers.meethue.com/documentation/error-messages
		/// </summary>
		public int errorCode;
		public string address;
		public string description;
		public string failingJson;

		public HueErrorInfo(string webrequestError, string failingJson = null)
		{
			this.webrequestError = webrequestError;
			this.failingJson = failingJson;
		}

		public HueErrorInfo(JSON jsonObject)
		{
			var errorObject = jsonObject.getJSON("error");

			if (errorObject.isValid)
			{
				if (errorObject.hasKey(HueKeys.TYPE))
				{
					this.errorCode = errorObject.getInt(HueKeys.TYPE, 0);
				}
				if (errorObject.hasKey(HueKeys.ADDRESS))
				{
					this.address = errorObject.getString(HueKeys.ADDRESS, "");
				}
				if (errorObject.hasKey(HueKeys.DESCRIPTION))
				{
					this.description = errorObject.getString(HueKeys.DESCRIPTION, "");
				}
			}
		}

		/// <summary>
		/// Standard way of handling the error. Simply logs all 
		/// error information to the console
		/// </summary>
		/// <param name="error">Error.</param>
		public static void LogError(HueErrorInfo error)
		{
			Debug.LogWarning(error.ToString());
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine("Unity Hue encountered an error with the following details: ").
			AppendLine("Webrequest Error : " + webrequestError).
			AppendLine("Error Code : " + errorCode.ToString()).
			AppendLine("Adress : " + address).
			AppendLine("Description : " + description).
			AppendLine("Non-Decoding JSON : " + failingJson);
			return builder.ToString();
		}

		public static bool JsonContainsErrorKey(object json)
		{
			return JsonHelper.UnravelJson(json, HueKeys.ERROR) != null;
		}

		public bool IsRequestError
		{
			get
			{
				return !string.IsNullOrEmpty(webrequestError);
			}
		}

		public bool IsJsonDecodeError
		{
			get
			{
				return !string.IsNullOrEmpty(failingJson);
			}
		}

		public bool IsHueAPIError
		{
			get
			{
				return !string.IsNullOrEmpty(description);
			}
		}
	}
}