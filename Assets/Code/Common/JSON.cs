﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Google.Apis.Json;


/*
This class acts as a wrapper for a JSON parser (currently Google's opensource one)

The "key" input argument is always dotted notation of the data hierarchy, such as "data.player.name".

----------------------------------------------------------------------------------------------------------	
Properties
----------------------------------------------------------------------------------------------------------	

	string	jsonString	// The original string used to create the JSON object.
	bool	isValid		// Whether the jsonString contains valid JSON, and therefor if the JSON object has any data.

----------------------------------------------------------------------------------------------------------	
Get single values
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	JSON			getJSON				(string key);
	int				getInt				(string key, int default);
	long			getLong				(string key, long default);
	float			getFloat			(string key, float default);
	double			getDouble			(string key, double default);
	decimal			getDecimal			(string key, decimal default);
	string			getString			(string key, string default);
	bool			getBool				(string key, bool default);

----------------------------------------------------------------------------------------------------------	
Get one-dimensional arrays
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	JSON[]			getJsonArray			(string key)
	string[]		getStringArray			(string key)
	int[]			getIntArray				(string key)
	long[]			getLongArray			(string key)
	float[]			getFloatArray			(string key)
	List<string>	getKeyList				()
	List<JSON>		getRetardedJsonList		(string key)

----------------------------------------------------------------------------------------------------------	
Get two-dimensional arrays
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	List<List<int>>		getIntListList			(string key)
	List<List<string>>	getStringListList		(string key)

----------------------------------------------------------------------------------------------------------	
Get dictionaries
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	Dictionary<int, string>		getIntStringDict	(string key)
	Dictionary<int, int>		getIntIntDict		(string key)
	Dictionary<string, string>	getStringStringDict	(string key)
	Dictionary<string, int>		getStringIntDict	(string key)
	Dictionary<string, long>	getStringLongDict	(string key)
	Dictionary<string, JSON>	getStringJSONDict	(string key)

----------------------------------------------------------------------------------------------------------	
Other functionality
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	bool		hasKey				(string key)
	string		ToString			()

STATIC METHODS:
	string		createJsonString	(string name, object value)
	string		sanitizeString		(string input)

*/
namespace gilligames
{
	public class JSON
	{
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Properties
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public string jsonString { get; private set; }
		public bool isValid { get; private set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Constructors
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public JSON(string inputString)
		{
			inputString = inputString.Trim();

			// The json can't be an array at the root. It needs to be an object with a key/value pair,
			// so if the input string is a root array, create a wrapper object named rootArray.
			if (inputString.StartsWith("[") && inputString.EndsWith("]"))
			{
				inputString = string.Format("{{ \"rootArray\":{0} }}", inputString);
			}

			jsonString = inputString;

			if (jsonString == null)
			{
				// Prevent Trim() error below.
				jsonString = "";
			}

			if (jsonString == "")
			{
				string stack = UnityEngine.StackTraceUtility.ExtractStackTrace().Replace("\n", " -> ");
				Debug.LogError("Null or empty JSON sent. Stack: " + stack);
				isValid = false;
				return;
			}
			
			try
			{
				jsonDict = JsonReader.Parse(jsonString) as JsonDictionary;
				
				if (jsonDict == null)
				{
					Debug.LogErrorFormat("Invalid JSON: {0}", jsonString);
					isValid = false;
					return;
				}
			}
			catch
			{
				Debug.LogErrorFormat("Invalid JSON: {0}", jsonString);
				isValid = false;
				return;
			}

			// Please never ever commit this line uncommented!
			//Debug.Log ("Just parsed json string " + jsonString);

			isValid = true;
		}

		// This constructor is only used internally,
		// since we don't use JsonDictionary objects outside of this class.
		private JSON(JsonDictionary jsonDict)
		{
			this.jsonDict = jsonDict;
			isValid = true;
		}
		
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Create custom byte array representation of current json
		////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// public enum Format
		// {
		// 	UTF8,									// UTF8 is smaller...
		// 	UNICODE									// Unicode is faster
		// }
		// 
		// public byte[] toBinary(Format format = Format.UTF8, string extraInfoString = "")
		// {
		// 	var writer = new JsonBinaryWriter();
		// 	return writer.jsonToBinary(this.jsonDict, format, extraInfoString);
		// }
		
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Get single values
		////////////////////////////////////////////////////////////////////////////////////////////////////

		// Public method to simply get a sub-JSON object from the JSON object.
		public JSON getJSON(string key)
		{
			if (key == "")
			{
				return this;
			}

			object obj = getObject(key);

			if (obj is JsonDictionary)
			{
				return new JSON((JsonDictionary)obj);
			}

			return null;
		}

		// Public method to simply get string data from the JSON object.
		public string getString(string key, string defaultVal, bool useDefaultIfEmpty = true)
		{
			object obj = getObject(key);

			if (obj is string)
			{
				string str = (string)obj;

				if (string.IsNullOrEmpty(str) && useDefaultIfEmpty)
				{
					return defaultVal;
				}

				return str;
			}
			else if (obj is bool)
			{
				return ((bool)obj).ToString();
			}

			return defaultVal;
		}

		// Public method to simply get int data from the JSON object.
		public int getInt(string key, int defaultVal)
		{
			object obj = getObject(key);

			if (obj is string)
			{
				// Use the float parser instead of the int parser since "2.0" will fail on the int parser, which is retarded.
				float floatVal = getFloat(key, float.MaxValue);

				if (floatVal == float.MaxValue)
				{
					return defaultVal;
				}

				return Mathf.RoundToInt(floatVal);
			}

			return defaultVal;
		}

		// Public method to simply get long data from the JSON object.
		public long getLong(string key, long defaultVal)
		{
			object obj = getObject(key);

			if (obj is string)
			{
				// Use the float parser instead of the int parser since "2.0" will fail on the int parser, which is retarded.
				double doubleVal = getDouble(key, double.MaxValue);

				if (doubleVal == double.MaxValue)
				{
					return defaultVal;
				}

				return Common.roundToLong(doubleVal);
			}

			return defaultVal;
		}

		// Public method to simply get boolean data from the JSON object.
		public bool getBool(string key, bool defaultVal)
		{
			object obj = getObject(key);

			if (obj is bool)
			{
				return (bool)obj;
			}
			else if (obj is string)
			{
				string s = (string)obj;
				if (s.Length > 0)
				{
					switch (s[0])
					{
						case '0':
						case 'f':
						case 'F':
							return false;

						case '1':
						case '2':
						case '3':
						case '4':
						case '5':
						case '6':
						case '7':
						case '8':
						case '9':
						case 't':
						case 'T':
							return true;
					}
				}
			}

			return defaultVal;
		}

		// Public method to simply get float data from the JSON object.
		public float getFloat(string key, float defaultVal)
		{
			object obj = getObject(key);

			if (obj is string)
			{
				float value;
				if (float.TryParse((string)obj, out value))
				{
					return value;
				}
			}

			return defaultVal;
		}

		// Public method to simply get double data from the JSON object.
		public double getDouble(string key, double defaultVal)
		{
			object obj = getObject(key);

			if (obj is string)
			{
				double value;
				if (double.TryParse((string)obj, out value))
				{
					return value;
				}
			}

			return defaultVal;
		}

		// Public method to simply get double data from the JSON object.
		public decimal getDecimal(string key, decimal defaultVal)
		{
			object obj = getObject(key);

			if (obj is string)
			{
				decimal value;
				if (decimal.TryParse((string)obj, out value))
				{
					return value;
				}
			}

			return defaultVal;
		}

		// Public method to simply get a dictionary value from the JSON object.
		// Returns null if the value isn't a dictionary.
		public Dictionary<string, object> getDict(string key)
		{
			object obj = getObject(key);

			if (obj is Dictionary<string, object>)
			{
				return obj as Dictionary<string, object>;
			}

			return null;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Get one-dimensional arrays
		////////////////////////////////////////////////////////////////////////////////////////////////////

		// Gathers an array of sub-JSON objects from the JSON
		public JSON[] getJsonArray(string key)
		{
			List<object> list = getObject(key) as List<object>;

			if (list == null)
			{
				return new JSON[0];
			}

			JSON[] data = new JSON[list.Count];
			int i = 0;

			foreach (object obj in list)
			{
				if (obj is JsonDictionary)
				{
					data[i] = new JSON((JsonDictionary)obj);
				}
				else
				{
					data[i] = null;
				}
				i++;
			}

			return data;
		}

		// Gathers a string array from the JSON
		public string[] getStringArray(string key)
		{
			List<object> list = getObject(key) as List<object>;

			if (list == null)
			{
				return new string[0];
			}

			string[] data = new string[list.Count];
			int i = 0;

			foreach (object obj in list)
			{
				if (obj is int)
				{
					data[i] = ((int)obj).ToString();
				}
				else if (obj is bool)
				{
					data[i] = ((bool)obj).ToString();
				}
				else if (obj is float)
				{
					data[i] = ((float)obj).ToString();
				}
				else if (obj is string)
				{
					data[i] = (string)obj;
				}
				else
				{
					data[i] = "";
				}
				i++;
			}

			return data;
		}

		// Gathers an integer array from the JSON
		public int[] getIntArray(string key)
		{
			List<object> list = getObject(key) as List<object>;

			if (list == null)
			{
				return new int[0];
			}

			int[] data = new int[list.Count];
			int i = 0;

			foreach (object obj in list)
			{
				if (obj is string)
				{
					int value;
					if (int.TryParse((string)obj, out value))
					{
						data[i] = value;
					}
					else
					{
						data[i] = 0;
					}
				}
				else if (obj is bool)
				{
					data[i] = (bool)obj ? 1 : 0;
				}
				else
				{
					data[i] = 0;
				}
				i++;
			}

			return data;
		}
		
		// Gathers an array of long ints from the JSON
		public long[] getLongArray(string key)
		{
			List<object> list = getObject(key) as List<object>;

			if (list == null)
			{
				return new long[0];
			}

			long[] data = new long[list.Count];
			int i = 0;

			foreach (object obj in list)
			{
				if (obj is string)
				{
					long value;
					if (long.TryParse((string)obj, out value))
					{
						data[i] = value;
					}
					else
					{
						data[i] = 0;
					}
				}
				else if (obj is bool)
				{
					data[i] = (bool)obj ? 1L : 0L;
				}
				else
				{
					data[i] = 0L;
				}
				i++;
			}

			return data;
		}

		// Gathers an float array from the JSON
		public float[] getFloatArray(string key)
		{
			List<object> list = getObject(key) as List<object>;

			if (list == null)
			{
				return new float[0];
			}

			float[] data = new float[list.Count];
			int i = 0;

			foreach (object obj in list)
			{
				if (obj is string)
				{
					float value;
					if (float.TryParse((string)obj, out value))
					{
						data[i] = value;
					}
					else
					{
						data[i] = 0f;
					}
				}
				else if (obj is bool)
				{
					data[i] = (bool)obj ? 1f : 0f;
				}
				else
				{
					data[i] = 0f;
				}
				i++;
			}

			return data;
		}

		// Gathers an double array from the JSON
		public double[] getDoubleArray(string key)
		{
			List<object> list = getObject(key) as List<object>;

			if (list == null)
			{
				return new double[0];
			}

			double[] data = new double[list.Count];
			int i = 0;

			foreach (object obj in list)
			{
				if (obj is string)
				{
					double value;
					if (double.TryParse((string)obj, out value))
					{
						data[i] = value;
					}
					else
					{
						data[i] = 0f;
					}
				}
				else if (obj is bool)
				{
					data[i] = (bool)obj ? 1.0 : 0.0;
				}
				else
				{
					data[i] = 0.0;
				}
				i++;
			}

			return data;
		}

		// Returns all the keys that are in the current JSON object.
		public List<string> getKeyList()
		{
			return new List<string>(jsonDict.Keys);
		}
		
		// Gets a list of JSON objects that are sub-objects of the given key,
		// but are keyed themselves on data that we don't know in advance,
		// so we must get the key list first and build it the hard way.
		// I strongly recommend against this json format but backend doesn't listen to my advice.
		public List<JSON> getRetardedJsonList(string key)
		{
			List<JSON> list = new List<JSON>();
			JSON json = getJSON(key);
			if (json != null)
			{
				List<string> keys = json.getKeyList();
				foreach (string subKey in keys)
				{
					list.Add(json.getJSON(subKey));
				}
			}
			return list;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Get two-dimensional arrays
		////////////////////////////////////////////////////////////////////////////////////////////////////

		// Returns a list of embedded arrays of integers, where the embedded array is a simple array with no key/name pairings.
		// Example of what raw json string looks like:
		// "cards_picked":[
		//    [
		//       "10",
		//       "15",
		//       "15"
		//    ],
		//    [
		//       "40",
		//       "55",
		//       "70"
		//    ]
		// ]
		public List<List<int>> getIntListList(string key)
		{
			List<List<int>> list = new List<List<int>>();

			List<object> outerObjects = getObject(key) as List<object>;

			if (outerObjects == null)
			{
				return new List<List<int>>();
			}

			for (int i = 0; i < outerObjects.Count; i++)
			{
				List<int> inner = new List<int>();
				list.Add(inner);

				List<object> innerObjects = outerObjects[i] as List<object>;
				for (int j = 0; j < innerObjects.Count; j++)
				{
					inner.Add(int.Parse(innerObjects[j] as string));
				}
			}

			return list;
		}
		
		// Returns a list of embedded arrays of strings, where the embedded array is a simple array with no key/name pairings.
		public List<List<string>> getStringListList(string key)
		{
			List<List<string>> list = new List<List<string>>();

			List<object> outerObjects = getObject(key) as List<object>;

			if (outerObjects == null)
			{
				return new List<List<string>>();
			}

			for (int i = 0; i < outerObjects.Count; i++)
			{
				List<string> inner = new List<string>();
				list.Add(inner);

				List<object> innerObjects = outerObjects[i] as List<object>;
				for (int j = 0; j < innerObjects.Count; j++)
				{
					inner.Add(innerObjects[j] as string);
				}
			}

			return list;
		}
		
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Get dictionaries
		////////////////////////////////////////////////////////////////////////////////////////////////////

		/*
		Returns a List of keys and values that embeded in a JSON object.
		"initial_reel_sets": {
				"1": "elvira02_reelset_foreground"
		},
		*/
		public Dictionary<int, string> getIntStringDict(string key = "")
		{
			Dictionary<int,string> returnVal = new Dictionary<int,string>();

			JSON dictionaryJson = getJSON(key);
			if (dictionaryJson != null)
			{
				foreach (string subKey in dictionaryJson.getKeyList())
				{
					int intKey;
					if (int.TryParse(subKey, out intKey))
					{
						returnVal[intKey] = dictionaryJson.getString(subKey, "");
					}
				}
			}

			return returnVal;
		}

		/*
		Returns a List of keys and values that embeded in a JSON object.
		"some_key": {
				"1": "500"
		},
		*/
		public Dictionary<int, int> getIntIntDict(string key = "")
		{
			Dictionary<int, int> returnVal = new Dictionary<int, int>();

			JSON dictionaryJson = getJSON(key);
			if (dictionaryJson != null)
			{
				foreach (string subKey in dictionaryJson.getKeyList())
				{
					int intKey;
					if (int.TryParse(subKey, out intKey))
					{
						returnVal[intKey] = dictionaryJson.getInt(subKey, 0);
					}
				}
			}

			return returnVal;
		}

		public Dictionary<string, string> getStringStringDict(string key = "")
		{
			Dictionary<string, string> returnVal = new Dictionary<string, string>();
			
			JSON dictionaryJson = getJSON(key);
			
			if (dictionaryJson != null)
			{
				foreach (KeyValuePair<string, object> kvp in dictionaryJson.jsonDict)
				{
					returnVal[kvp.Key] = kvp.Value.ToString();				
				}
			}
			
			return returnVal;
		}

		public Dictionary<string, float> getStringFloatDict(string key = "")
		{
			Dictionary<string, float> returnVal = new Dictionary<string, float>();

			JSON dictionaryJson = getJSON(key);
			if (dictionaryJson != null)
			{
				foreach (string subKey in dictionaryJson.getKeyList())
				{
					returnVal[subKey] = dictionaryJson.getFloat(subKey, 0.0f);
				}
			}

			return returnVal;
		}

		public Dictionary<string, double> getStringDoubleDict(string key = "")
		{
			Dictionary<string, double> returnVal = new Dictionary<string, double>();

			JSON dictionaryJson = getJSON(key);
			if (dictionaryJson != null)
			{
				foreach (string subKey in dictionaryJson.getKeyList())
				{
					returnVal[subKey] = dictionaryJson.getDouble(subKey, 0.0);
				}
			}

			return returnVal;
		}

		public Dictionary<string, int> getStringIntDict(string key = "")
		{
			Dictionary<string, int> returnVal = new Dictionary<string, int>();
			
			JSON dictionaryJson = getJSON(key);
			if (dictionaryJson != null)
			{
				foreach (string subKey in dictionaryJson.getKeyList())
				{
					returnVal[subKey] = dictionaryJson.getInt(subKey, 0);
				}
			}
			
			return returnVal;
		}

		public Dictionary<string, long> getStringLongDict(string key = "")
		{
			Dictionary<string, long> returnVal = new Dictionary<string, long>();
			
			JSON dictionaryJson = getJSON(key);
			if (dictionaryJson != null)
			{
				foreach (string subKey in dictionaryJson.getKeyList())
				{
					returnVal[subKey] = dictionaryJson.getLong(subKey, 0L);
				}
			}
			
			return returnVal;
		}

		public Dictionary<string, JSON> getStringJSONDict(string key = "")
		{
			Dictionary<string, JSON> returnVal = new Dictionary<string, JSON>();

			JSON dictionaryJson = getJSON(key);
			if (dictionaryJson != null)
			{
				foreach (string subKey in dictionaryJson.getKeyList())
				{
					returnVal[subKey] = dictionaryJson.getJSON(subKey);
				}
			}

			return returnVal;
		}

		public Dictionary<string, object> getStringObjectDict(string key = "")
		{
			Dictionary<string, object> returnVal = new Dictionary<string, object>();

			JSON dictionaryJson = getJSON(key);
			if (dictionaryJson != null)
			{
				foreach (string subKey in dictionaryJson.getKeyList())
				{
					returnVal[subKey] = dictionaryJson.getObject(subKey);
				}
			}

			return returnVal;
		}
		
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Other functionality
		////////////////////////////////////////////////////////////////////////////////////////////////////

		// Creates a JSON string out of any (supported) type.
		public static string createJsonString(object value, string name = "")
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			buildJsonString(builder, name, value);
			return builder.ToString();
		}

		// Sanitize a string for use of quotes
		public static string sanitizeString(string input)
		{
			if (input == null)
			{
				return null;
			}

			// Escape backslashes, quotes and newlines to prevent JSON weirdness:
			return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
		}

		// Does the given key exist
		public bool hasKey(string key)
		{
			return getObject(key) != null;
		}

		// Returns the string representation of this JSON object.
		public override string ToString()
		{
			if (isValid)
			{
				return createJsonString(jsonDict);
			}
			return "";
		}

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// Private stuff
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private JsonDictionary jsonDict;

		// Gets an object at the given key, central to all JSON class functionality.
		// Only used internally. DO NOT make public. Use getJSON if you need to get sub-JSON.
		private object getObject(string key)
		{
			if (jsonDict == null)
			{
				return null;
			}

			string[] parts = key.Split('.');
			JsonDictionary currentDict = jsonDict;

			for (int i = 0; i < parts.Length - 1; i++)
			{
				if (currentDict != null && currentDict.ContainsKey(parts[i]))
				{
					currentDict = currentDict[parts[i]] as JsonDictionary;
				}
				else
				{
					return null;
				}
			}
			
			// We need to check currentDict for null before trying to use it, because
			// it's possible for a JSON object to be null even when there is a key.
			// This happens with an empty array in the JSON string like this:
			// "parameters": []

			if (currentDict != null && currentDict.ContainsKey(parts[parts.Length - 1]))
			{
				return currentDict[parts[parts.Length - 1]];
			}

			return null;
		}
		
		// Builds a JSON string out of any supported types, appending it to the given StringBuilder.
		// Called internally by createJsonString().
		private static void buildJsonString(System.Text.StringBuilder builder, string name, object value)
		{
			if (!string.IsNullOrEmpty(name))
			{
				// If the name passed in is empty or null, then assume the user wants just the array
				// and don't append it as a property name.
				builder.Append("\"");
				builder.Append(name);
				builder.Append("\":");
			}

			if (value == null)
			{
				builder.Append("null");
			}
			else if (value is int)
			{
				builder.Append(value.ToString());
			}
			else if (value is long)
			{
				builder.Append(value.ToString());
			}
			else if (value is float)
			{
				builder.Append(string.Format("{0:0.000}", (float)value));
			}
			else if (value is double)
			{
				builder.Append(string.Format("{0:0.000}", (double)value));
			}
			else if (value is decimal)
			{
				builder.Append(value.ToString());
			}
			else if (value is bool)
			{
				builder.Append((bool)value ? "true" : "false");
			}
			else if (value is string)
			{
				builder.Append(string.Format("\"{0}\"", sanitizeString(value as string)));
			}
			else if (value is IList)
			{
				buildArray(builder, value as IList);
			}
			else if (value is IDictionary)
			{
				buildDictionary(builder, value as IDictionary);
			}
			else if (value is JSON)
			{
				buildJsonString(builder, "", (value as JSON).jsonDict);
			}
			else
			{
				Debug.LogError("JSON.buildJsonString() Error with value type: " + value.GetType().ToString()); 
			}
		}
			
		// Builds a JSON string from an object that implements IList. Called internally by buildJsonString().
		private static void buildArray(System.Text.StringBuilder builder, IList list)
		{
			builder.Append("[");
			bool first = true;
			foreach (var value in list)
			{
				// Comma-separate pairs by inserting comma before every pair but the first.
				if (first)
				{
					first = false;
				}
				else
				{
					builder.Append(",");
				}
				buildJsonString(builder, "", value);
			}

			builder.Append("]");
		}

		// Builds a JSON string from an object that implements IDictionary. Called internally by buildJsonString().
		private static void buildDictionary(System.Text.StringBuilder builder, IDictionary dict)
		{
			builder.Append("{");
			bool first = true;
			foreach (DictionaryEntry p in dict)
			{
				// Comma-separate pairs by inserting comma before every pair but the first. 
				if (first)
				{
					first = false;
				}
				else
				{
					builder.Append(",");
				}
				buildJsonString(builder, p.Key.ToString(), p.Value);
			}

			builder.Append("}");
		}
	}
}
