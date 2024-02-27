using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace VladislavMang.Merging
{
	public class MergeInputHandler : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
	{
		public event Action OnPressedEvent;
		public event Action OnReleasedEvent;

		private bool _isPressed;
		public bool IsPressed
		{
			get => _isPressed;
			set
			{
				_isPressed = value;
				if (value) OnPressedEvent?.Invoke();
				else OnReleasedEvent?.Invoke();
			}
		}

		public Vector3 PointerPosition => Input.mousePosition;

		public void OnPointerDown(PointerEventData eventData)
		{
			IsPressed = true;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			IsPressed = false;
		}
	}
}
