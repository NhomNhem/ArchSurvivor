//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	internal struct AboutButton
	{
		public GUIContent title;
		public string url;

		public AboutButton(string title, string url)
		{
			this.title = new GUIContent(title);
			this.url = url;
		}
	}

	internal struct AboutSection
	{
		public GUIContent title;
		public AboutButton[] buttons;

		public AboutSection(string title)
		{
			this.title = new GUIContent(title);
			this.buttons = null;
		}
	}

	internal class AboutToolbar
	{
		private int _selectedIndex = -1;
		private AboutInfo[] _infos;

		public AboutToolbar(AboutInfo[] infos)
		{
			_infos = infos;
		}

		internal void OnGUI()
		{
			if (!Preferences.ShowHeaderAboutButtons)
			{
				return;
			}

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			for (int i = 0; i < _infos.Length; i++)
			{
				if (_infos[i].Visible())
				{
					if (_infos[i].OnHeaderGUI())
					{
						if (i != _selectedIndex)
						{
							if (_selectedIndex >= 0)
							{
								_infos[_selectedIndex].isExpanded =  false;
							}
							_selectedIndex = i;
						}
					}
				}
			}
			GUILayout.EndHorizontal();
			if (_selectedIndex >= 0)
			{
				_infos[_selectedIndex].OnGUI();
			}
		}
	}

	/// <summary>
	/// A customisable UI window that displays basic information about the asset/component and has
	/// categories of buttons that link to documentation and support
	/// </summary>
	internal class AboutInfo
	{
		private string iconPath;
		private GUIContent title;
		internal AboutSection[] sections;
		private GUIContent icon;
		private GUIContent buttonLabelOpen;
		private GUIContent buttonLabelClosed;
		private System.Predicate<bool> showAction;

		internal bool isExpanded = false;
		private static GUIStyle paddedBoxStyle;
		private static GUIStyle richBoldLabelStyle;
		private static GUIStyle richButtonStyle;
		private static GUIStyle titleStyle;

		public AboutInfo(string buttonLabel, string title, string iconPath, System.Predicate<bool> showAction = null)
		{
			this.title = new GUIContent(title);
			this.iconPath = iconPath;
			this.buttonLabelOpen = new GUIContent("▼ " + buttonLabel);
			this.buttonLabelClosed = new GUIContent("► " + buttonLabel);
			this.showAction = showAction;
			sections = null;
		}

		internal bool Visible()
		{
			if (this.showAction !=  null)
			{
				return showAction.Invoke(true);
			}
			return true;
		}

		internal bool OnHeaderGUI()
		{
			isExpanded = GUILayout.Toggle(isExpanded, isExpanded?this.buttonLabelOpen:this.buttonLabelClosed, GUI.skin.button);
			return isExpanded;
		}

		internal void OnGUI()
		{
			if (paddedBoxStyle == null)
			{
				paddedBoxStyle = new GUIStyle(GUI.skin.window);
				paddedBoxStyle.padding = new RectOffset(8, 8, 16, 16);
			}
			if (titleStyle == null)
			{
				titleStyle = new GUIStyle(EditorStyles.largeLabel);
				titleStyle.wordWrap = true;
				titleStyle.richText = true;
				titleStyle.alignment = TextAnchor.UpperCenter;
			}
			if (richBoldLabelStyle == null)
			{
				richBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
				richBoldLabelStyle.richText = true;
				richBoldLabelStyle.alignment = TextAnchor.UpperCenter;
				richBoldLabelStyle.margin.top = 20;
				richBoldLabelStyle.margin.bottom = 10;
				richBoldLabelStyle.wordWrap = true;
				richBoldLabelStyle.fontSize = 14;
			}
			if (richButtonStyle == null)
			{
				richButtonStyle = new GUIStyle(GUI.skin.button);
				richButtonStyle.richText = true;
				richButtonStyle.stretchWidth = true;
			}
			
			if (this.icon == null)
			{
				this.icon = new GUIContent(Resources.Load<Texture2D>(this.iconPath));
			}

			if (isExpanded)
			{
				GUILayout.BeginVertical(paddedBoxStyle);

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(this.icon);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.Label(this.title, titleStyle);

				foreach (AboutSection section in this.sections)
				{
					GUILayout.Label(section.title, richBoldLabelStyle);
					GUILayout.BeginVertical();
					if (section.buttons != null)
					{
						foreach (AboutButton button in section.buttons)
						{
							if (GUILayout.Button(button.title, richButtonStyle))
							{
								Application.OpenURL(button.url);
							}
						}
					}
					GUILayout.EndVertical();
				}
				
				GUILayout.EndVertical();
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}
		}
	}
}
