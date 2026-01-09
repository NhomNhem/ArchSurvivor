//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChocDino.UIFX;

namespace ChocDino.UIFX.Demos
{
	public class AutoTransformer : MonoBehaviour
	{
		[SerializeField] float _speed = 4f;
		[SerializeField] float _maxSpinSpeed = 30f;
		[SerializeField] float _moveRange = 500f;
		[SerializeField] Transform _xform1 = null;
		[SerializeField] Transform _xform2 = null;
		[SerializeField] Transform _xform3 = null;

		private float _rotationTime = 0f;

		void Update()
		{
			if (Time.timeScale > 0f)
			{
				if (_xform1) Animate(_xform1, GetTime(0f));
				//Animate(_xform2, GetTime(0.5f));
				//Animate(_xform3, GetTime(1.0f));
				if (_xform2) Rotation(_xform2);
				if (_xform3) Scale(_xform3);
			}
		}

		float GetTime(float offset)
		{
			float duration = 1.5f;
			float delay = 0.2f * _speed;
			float t = Mathf.PingPong(Time.time * _speed, (duration + delay));
			t /= (duration);
			t = Mathf.Clamp01(t);
			return t;
		}
		
		Lerper lerper;
		Lerper _scaleAnim;

		void Animate(Transform xform, float t)
		{
			if (lerper == null)
			{
				lerper = new Lerper();
				
				lerper.Set(0f).Delay(0.5f).Lerp(0f, _moveRange, 1f).Delay(1f).Lerp(_moveRange, -_moveRange, 1f).Delay(1f).Lerp(-_moveRange, _moveRange, 1f).Delay(0.5f).Lerp(_moveRange, 0f, 1f);
			}

			if (lerper.Do(Time.deltaTime * _speed))
			{
				lerper.Play();
			}

			float x = lerper.result;

			//float t1 = 1f-EaseExpo(1f-t);
			//float t2 = 1f-EaseCubic(1f-t);
			//float t3 = DemoUtils.InOutExpo(t);
			//float scale = Mathf.Lerp(0.8f, 1.2f, t2);
			//float angle = Mathf.Lerp(90f, 0f, t3);
			//float y = Mathf.Lerp(1200f, 0f, t3);
			xform.localPosition = new Vector3(x, 0f, 0f);
			//xform.gameObject.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.Lerp(Color.clear, Color.white, t);
			//_xform2.localPosition = new Vector3(x, 0f, 0f);
			//_xform3.localPosition = new Vector3(x, 0f, 0f);
			//xform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
			//xform.localScale = new Vector3(scale, scale, scale);
			//xform.GetChild(0).localScale = new Vector3(scale, scale, scale);
			//_xform2.localScale = new Vector3(scale, scale, 1f);
		}

		void Rotation(Transform xform)
		{
			float rotationSpeed = Mathf.Sin(Time.time * 2.5f);
			_rotationTime += Time.deltaTime * rotationSpeed * 80f * _maxSpinSpeed;
			xform.localRotation = Quaternion.Euler(0f, 0f, _rotationTime);
		}

		void Scale(Transform xform)
		{
			float t = GetTime(0f);
			t = DemoUtils.InOutExpo(t);

			/*if (_scaleAnim == null)
			{
				_scaleAnim = new Lerper();
				_scaleAnim.Set(0f).Delay(0.5f).Lerp(0f, 2000f, 1f).Delay(0.25f).Lerp(2000f, 0f, 1f).Delay(0.25f).Lerp(0f, -360f, 1f).Delay(0.5f).Lerp(-360f, 2000f, 1f);
			}

			if (_scaleAnim.Do(Time.deltaTime * _speed))
			{
				_scaleAnim.Play();
			}*/

			float scale = Mathf.Lerp(0.2f, 1f, t);

			//scale = _scaleAnim.result;

			xform.localScale = Vector3.one * scale;
			//xform.localPosition = new Vector3(0f, 0f, scale);
		}

		private class Lerper
		{
			public Lerper()
			{
				_modes = new List<Mode>();
				_durations = new List<float>();
				_lerpSrc= new List<float>();
				_lerpDst = new List<float>();
				_set = new List<float>();
			}

			public Lerper Set(float value)
			{
				_modes.Add(Mode.Set);
				_set.Add(value);
				_durations.Add(0f);
				return this;
			}

			public Lerper Delay(float time)
			{
				_modes.Add(Mode.Delay);
				_durations.Add(time);
				return this;
			}

			public Lerper Lerp(float src, float dst, float time)
			{
				_modes.Add(Mode.Lerp);
				_durations.Add(time);
				_lerpSrc.Add(src);
				_lerpDst.Add(dst);
				return this;
			}

			public void Play()
			{
				_accumTime = 0f;
				_index = 0;
				_time = 0f;
				_lerpCount = 0;
				_delayCount = 0;
				_setCount = 0;
			}

			public bool Do(float deltaTime)
			{
				float startTime = _time;
				float endTime = _time + deltaTime;
				
				bool doNext = false;
				do
				{
					doNext = false;
					if (_index < _modes.Count)
					{
						Mode mode = _modes[_index];
						switch (mode)
						{
							case Mode.Set:
								doNext = DoSet();
								break;
							case Mode.Delay:
								doNext = DoDelay();
								break;
							case Mode.Lerp:
								doNext = DoLerp();
								break;
						}
					}
				} 
				while(doNext);

				_time = endTime;

				return (_index >= _modes.Count);
			}

			bool DoSet()
			{
				result = _set[_index];
				{
					//Debug.Log("fin SET");
					// Next
					_accumTime += 0f;
					_setCount++;
					_index++;
					return true;
				}
			}

			bool DoDelay()
			{
				float startTime = _accumTime;
				float endTime = startTime + _durations[_index];
				//Debug.Log("delay " + _time + " "+ endTime);
				if (_time >= endTime)
				{
					//Debug.Log("fin DELAY");
					// Next
					_accumTime += _durations[_index];
					_delayCount++;
					_index++;
					return true;
				}
				return false;
			}

			bool DoLerp()
			{
				float startTime = _accumTime;
				float endTime = startTime + _durations[_index];

				float t = Mathf.Clamp01((_time - startTime) / _durations[_index]);
				t = DemoUtils.InOutExpo(t);
				result = Mathf.Lerp(_lerpSrc[_lerpCount], _lerpDst[_lerpCount], t);

				if (_time >= endTime)
				{
					//Debug.Log("fin LERP");
					// Next
					_accumTime += _durations[_index];
					_lerpCount++;
					_index++;
					return true;
				}
				return false;
			}

			public float result;

			private float _time;

			enum Mode
			{
				Set,
				Delay,
				Lerp,
			}
			List<Mode> _modes;
			List<float> _durations;
			List<float> _set;
			List<float> _lerpSrc;
			List<float> _lerpDst;
			int _index;
			int _lerpCount;
			int _delayCount;
			int _setCount;
			float _accumTime;
		}
	}
}