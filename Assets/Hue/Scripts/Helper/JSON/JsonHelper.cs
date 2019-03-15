using System.Collections;
using System.Collections.Generic;
using MiniJSON;

//namespace UnityHue {
//	public static class JsonHelper {
//		//finds the first occurence of an object in a dictionary with a supplied key
//		//a bit ugly but gets the job done quick most of the time
//		public static object UnravelJson(object obj, string keyToLookFor)
//		{
//			if(obj is List<object>)
//			{
//				var list = obj as List<object>;
//				foreach(var item in list)
//				{
//					var result = UnravelJson(item, keyToLookFor);
//					if(result != null)
//						return result;
//				}
//				return null;
//			}else if(obj is Dictionary<string, object>)
//			{
//				return RecurseThroughDictionary(obj as Dictionary<string, object>, keyToLookFor);
//			}else
//				return null;
//		}
//		public static object RecurseThroughDictionary (Dictionary<string, object> dict, string keyToLookFor)
//		{
//			if(dict.ContainsKey(keyToLookFor))
//				return dict[keyToLookFor];

//			foreach(var entry in dict)
//			{
//				var result = UnravelJson(entry.Value, keyToLookFor);
//				if(result != null)
//					return result;
//			}
//			return null;
//		}
//		public static void RecurseThroughDictionary (Dictionary<string, object> dict, string keyToLookFor,
//			ref List<object> results)
//		{
//			if(dict.ContainsKey(keyToLookFor))
//				results.Add(dict[keyToLookFor]);

//			foreach(var entry in dict)
//			{
//				UnravelJson(entry.Value, keyToLookFor, ref results);
//			}
//		}
//		/// <summary>
//		/// Gets a list of all the objects with the supplied keys somewhere in the json
//		/// </summary>
//		/// <param name="obj">Object.</param>
//		/// <param name="keyToLookFor">Key to look for.</param>
//		/// <param name="results">Results.</param>
//		public static void UnravelJson(object obj, string keyToLookFor, ref List<object> results)
//		{
//			if(obj is List<object>)
//			{
//				var list = obj as List<object>;
//				foreach(var item in list)
//				{
//					UnravelJson(item, keyToLookFor, ref results);
//				}
//			}else if(obj is Dictionary<string, object>)
//			{
//				RecurseThroughDictionary(obj as Dictionary<string, object>, keyToLookFor);
//			}
//		}

//		public static Dictionary<string, object> CreateJsonParameterDictionary(params JsonParameter[] parameters)
//		{
//			var dict = new Dictionary<string, object>();
//			foreach(var parameter in parameters)
//			{
//				dict.Add(parameter.parameterKey, parameter.parameterValue);
//			}
//			return dict;
//		}
//		public static string CreateJsonParameterString(params JsonParameter[] parameters)
//		{
//			return Json.Serialize(CreateJsonParameterDictionary(parameters));
//		}
			
//	}
//}
