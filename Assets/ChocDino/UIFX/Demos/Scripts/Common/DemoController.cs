//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChocDino.UIFX.Demos
{
	public class DemoController : MonoBehaviour
	{
		[Header("Demos")]
		[SerializeField] GameObject _demosRoot = null;
		[SerializeField] GameObject _startDemo = null;

		[Header("UI")]
		[SerializeField] Button _buttonPrev = null;
		[SerializeField] Button _buttonNext = null;
		[SerializeField] Scrollbar _scrollbar = null;
		[SerializeField] Text _textTitle = null;
		[SerializeField] Text _textProgress = null;
		[SerializeField] Text _textFPS = null;

		private GameObject[] _demos;
		public GameObject CurrentDemo { get { return _demos[_demoIndex]; } }
		public int DemoCount { get { return _demos.Length; } }

		private int _demoIndex = 0;

		private const float FPSInterval = 0.25f;
		private int _fpsFrameCount;
		private float _fpsTimer;

		void Start()
		{
			// Gather demos
			_demos = new GameObject[_demosRoot.transform.childCount];
			for (int i = 0; i < _demosRoot.transform.childCount; i++)
			{
				_demos[i] = _demosRoot.transform.GetChild(i).gameObject;
			}

			// Select starting demo from URL
			#if UNITY_WEBGL
			{
				string demoName = DemoUtils.GetUrlParameter(Application.absoluteURL, "demo");
				if (!string.IsNullOrEmpty(demoName))
				{
					foreach (GameObject go in _demos)
					{
						var demoInfo = go.GetComponentInChildren<DemoInfo>(true);
						if (demoInfo.slug.StartsWith(demoName))
						{
							_startDemo = go;
							break;
						}
					}
				}
			}
			#endif
			
			// Validate properties
			if (_startDemo)
			{
				Debug.Assert(GetDemoIndex(_startDemo) >= 0);
			}

			// Setup UI
			if (_buttonNext) { _buttonNext.onClick.AddListener(OnButtonNextClick); }
			if (_buttonPrev) { _buttonPrev.onClick.AddListener(OnButtonPrevClick); }
			if (_scrollbar)
			{
				_scrollbar.numberOfSteps = DemoCount;
				_scrollbar.onValueChanged.AddListener(OnScrollBar);
			}

			if (_demos != null && _demos.Length > 0)
			{
				// Disable all demos
				foreach (GameObject go in _demos)
				{
					go.SetActive(false);
				}

				// Start first demo
				if (_startDemo)
				{
					ChangeDemo(GetDemoIndex(_startDemo));
				}
				else
				{
					ChangeDemo(0);
				}
			}
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Application.Quit();
			}

			UpdateFPS();
		}

		void ChangeDemo(int demoIndex)
		{
			CurrentDemo.SetActive(false);
			
			_demoIndex = demoIndex;
			var demoInfo = CurrentDemo.GetComponentInChildren<DemoInfo>(true);

			if (_textProgress)
			{
				_textProgress.text = string.Format("{0}/{1}", (_demoIndex + 1), DemoCount);
			}
			if (_textTitle)
			{
				_textTitle.text = string.Empty;
				if (demoInfo)
				{
					_textTitle.text = demoInfo.title;
				}
			}
			if (_scrollbar)
			{
				float newScrollbarValue = 0f;
				if (DemoCount > 1)
				{
					newScrollbarValue = _demoIndex / (float)(DemoCount - 1);
				}
				#if UNITY_2019_OR_NEWER
				_scrollbar.SetValueWithoutNotify(newScrollbarValue);
				#else
				_scrollbar.value = newScrollbarValue;
				#endif
			}

			CurrentDemo.SetActive(true);
		}

		void UpdateFPS()
		{
			_fpsTimer += Time.unscaledDeltaTime;
			if (_fpsTimer >= FPSInterval)
			{
				_fpsTimer %= FPSInterval;
				float fps = _fpsFrameCount / FPSInterval;
				_fpsFrameCount = 0;

				if (_textFPS) { _textFPS.text = string.Format("{0:F1} FPS", fps); }
			}
			_fpsFrameCount++;
		}

		void OnButtonPrevClick()
		{
			if (_demoIndex - 1 >= 0)
			{
				ChangeDemo(_demoIndex - 1);
			}
		}

		void OnButtonNextClick()
		{
			if (_demoIndex + 1 < DemoCount)
			{
				ChangeDemo(_demoIndex + 1);
			}
		}

		void OnScrollBar(float progress)
		{
			progress = Mathf.Clamp01(progress);
			int demo = Mathf.RoundToInt(progress * (DemoCount - 1));
			if (demo != _demoIndex)
			{
				ChangeDemo(demo);
			}
		}

		int GetDemoIndex(GameObject go)
		{
			int result = -1;
			for (int i = 0; i < _demos.Length; i++)
			{
				if (_demos[i] == go)
				{
					result = i;
					break;
				}
			}
			return result;
		}
	}
}