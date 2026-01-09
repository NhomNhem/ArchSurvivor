//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.EventSystems;

namespace ChocDino.UIFX.Demos
{
	[RequireComponent(typeof(RectTransform))]
	public class FilterHoverStrength : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] FilterBase _filter = null;
		[SerializeField] FilterBase[] _filters = null;
		
		[Header("Speed")]
		[SerializeField] float _upSpeed = 8f;
		[SerializeField] float _downSpeed = 6f;

		[Header("Range")]
		[SerializeField, Range(0f, 1f)] float _minValue = 0f;
		[SerializeField, Range(0f, 1f)] float _maxValue = 1f;

		private bool _isOver = false;

		void Awake()
		{
			if (_filters == null || _filters.Length == 0)
			{
				if (_filter == null)
				{
					// Find the first enabled filter
					var filters = GetComponents<FilterBase>();
					foreach (var filter in filters)
					{
						if (filter.enabled)
						{
							_filter = filter;
							break;
						}
					}

					// If no filter is enabled then assign the first filter
					if (_filter == null && filters.Length > 0)
					{
						_filter = filters[0];
					}
				}
			}
			UpdateAnimation(true);
		}

		void Start()
		{
			UpdateAnimation(true);
		}

		void Update()
		{
			UpdateAnimation(false);
		}

		void UpdateAnimation(bool force)
		{
			if (!isActiveAndEnabled) return;
			
			float target = _minValue;
			float dampSpeed = _downSpeed;
			if (_isOver)
			{
				target = _maxValue;
				dampSpeed = _upSpeed;
			}

			if (_filters == null || _filters.Length == 0)
			{
				ApplyToFilter(_filter, dampSpeed, target, force);
			}
			else
			{
				foreach (var filter in _filters)
				{
					ApplyToFilter(filter, dampSpeed, target, force);
				}
			}
		}

		private static void ApplyToFilter(FilterBase filter, float dampSpeed, float target, bool force)
		{
			if (filter != null && filter.isActiveAndEnabled)
			{
				if (Mathf.Abs(filter.Strength - target) > 0.001f)
				{
					filter.Strength = MathUtils.DampTowards(filter.Strength, target, dampSpeed, Time.deltaTime);
				}
				else
				{
					force = true;
				}
				if (force)
				{
					filter.Strength = target;
				}
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			_isOver = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			_isOver = false;
		}
	}
}