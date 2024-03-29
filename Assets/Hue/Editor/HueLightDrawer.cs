﻿using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace UnityHue{
	[CustomPropertyDrawer(typeof(HueLight))]
	public class HueLightDrawer : PropertyDrawer {
		
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property, label, true);
			if (property.isExpanded)
			{
				
				if (GUI.Button(new Rect(position.xMin + 30f, position.yMax - 20f, position.width - 30f, 20f), "Set State"))
				{
					string id = property.FindPropertyRelative("id").stringValue;
					//pretty ugly but
					HueLight light = HueBridge.instance.Lights.Find(x => x.id == id);
					if (light != null)
					{
						light.SetState();
					}
				}
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.isExpanded)
				return EditorGUI.GetPropertyHeight(property) + 20f;
			return EditorGUI.GetPropertyHeight(property);
		}
	}
}
