#if UNITY_EDITOR
//#define SIMULATE_SLOW_CONNECTION
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/*
Common wrapper for the UnityWebRequest class to support retries and a timeout period.
After waitToFinish(), request will be null if the request timed out.
*/

namespace gilligames
{
	public class WWWWrapper
	{
		public const int TIMEOUT_SECONDS = 10;
		public const int MAX_ATTEMPTS = 3;
		private const float SECONDS_BETWEEN_TRIES = 1.0f;

		private static List<WWWWrapper> queue = new List<WWWWrapper>(); // Only one request is handled at a time, in order.

		private UnityWebRequest request;
		private UnityWebRequestAsyncOperation requestOperation;

		private string url;
		private string body = "";
		private Dictionary<string, string> headers = null;
		private int attempts = 0;
		private GameTimer timeoutTimer = null;
		private bool isInQueue = true;
		private bool isPut = false;

		// Convenience getter.
		public string responseText
		{
			get
			{
				if (request == null)
				{
					return "";
				}
				return request.downloadHandler.text;
			}
		}

		// Convenience getter.
		public JSON responseJSON
		{
			get { return new JSON(responseText); }
		}

		public string error
		{
			get { return request.error; }
		}

		public WWWWrapper(string url, Dictionary<string, object> bodyDict, Dictionary<string, string> headers = null, bool isPut = false)
		{
			initCommon(url, JSON.createJsonString(bodyDict), headers, isPut);
		}

		public WWWWrapper(string url, string body = "", Dictionary<string, string> headers = null, bool isPut = false)
		{
			initCommon(url, body, headers, isPut);
		}

		private void initCommon(string url, string body, Dictionary<string, string> headers, bool isPut)
		{
			Debug.LogFormat("WWWWrapper request, url: {0}, body: {1}", url, body);

			this.url = url;
			this.body = body;
			this.headers = headers;
			this.isPut = isPut;
			timeoutTimer = new GameTimer(TIMEOUT_SECONDS);

			if (url == "")
			{
				string stack = UnityEngine.StackTraceUtility.ExtractStackTrace().Replace("\n", " -> ");
				Debug.LogFormat("url is empty. called from: {0}", stack);
				return;
			}

			queue.Add(this);

			if (queue.Count == 1)
			{
				startTry();
			}
		}

		private void startTry()
		{
			isInQueue = false;
			attempts++;

			if (body != "")
			{
				// We can't use the shortcut UnityWebRequest.Post() because it url-encodes the body contents like a retard.
				request = new UnityWebRequest(url);
				request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
				request.method = (isPut ? UnityWebRequest.kHttpVerbPUT : UnityWebRequest.kHttpVerbPOST);
			}
			else
			{
				request = UnityWebRequest.Get(url);
			}

			if (headers == null)
			{
				headers = new Dictionary<string, string>();
			}

			if (!headers.ContainsKey("Content-Type"))
			{
				headers.Add("Content-Type", "application/json");
			}

			foreach (var header in headers)
			{
				request.SetRequestHeader(header.Key, header.Value);
			}

			request.downloadHandler = new DownloadHandlerBuffer();
			requestOperation = request.SendWebRequest();

			timeoutTimer.reset();
		}

		public IEnumerator waitToFinish()
		{
			if (url == "")
			{
				yield break;
			}

			while (isInQueue)
			{
				yield return null;
			}

			while (request != null && !request.isDone)
			{
				if (timeoutTimer.isExpired)
				{
					request.Dispose();

					if (attempts == MAX_ATTEMPTS)
					{
						// Reached max attempts, so bail with a warning logged.
						request = null;
						
						Debug.LogWarningFormat("UnityWebRequest timed out after {0} attempts of {1} seconds each: {2}", MAX_ATTEMPTS, TIMEOUT_SECONDS, url);
						yield break;
					}

					yield return new WaitForSeconds(SECONDS_BETWEEN_TRIES);

					startTry();
					Debug.LogWarningFormat("Attempt {0} of WWW request {1}", attempts, url);
				}
				yield return null;
			}

			if (requestOperation != null)
			{
				// Wait for the response buffer to be full before trying to use it.
				yield return requestOperation;
			}

#if SIMULATE_SLOW_CONNECTION
			yield return new WaitForSeconds(3.0f);
#endif

			Debug.LogFormat("WWWWrapper response: {0}", responseText);

			queue.Remove(this);

			if (queue.Count > 0)
			{
				// Start the next one.
				queue[0].startTry();
			}
		}
	}
}