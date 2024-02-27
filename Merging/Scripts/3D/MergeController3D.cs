using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEditor.Graphs;
using UnityEngine;

namespace VladislavMang.Merging._3D
{
	public abstract class MergeController3D<TItem, TItemData> : MonoBehaviour, IMergeController<TItem, TItemData>
		where TItem : MonoBehaviour, I3dMergingItem<TItem, TItemData>
		where TItemData : I3dMergingItemData<TItem>
	{
		private enum DraggingType { Lerp, MoveTowards }

		[Header("Selection")]
		[SerializeField, Range(0.99f, 1f)] private float _itemGrabbingTreshold;
		[SerializeField, Range(0.99f, 1f)] private float _mergeTreshold;
		[Header("Dragging")]
		[SerializeField] private DraggingType _draggingType;
		[SerializeField] private float _draggingSpeed;
		[SerializeField] private float _draggingMaxDistance;
		[SerializeField] private LayerMask _draggingFloorLayers;
		[Header("Debug")]
		[SerializeField] private bool _debug;

		private MergeInputHandler _inputHandler;
		private Camera _camera;

		private Action<TItem, Vector3> _dragGrabbedItemAction;
		private Action _refreshGrabbedItemTargetPointAction;

		public Action<TItemData> OnStartMerge { get; set; }
		public Action<TItem> OnMergedEvent { get; set; }
		public Action<TItem> OnItemGrabbed { get; set; }
		public Action<TItem> OnItemDropped { get; set; }

		public List<TItem> AvailableItems { get; set; } = new List<TItem>();
		private List<TItem> SuitableToMergeItems { get; set; } = new List<TItem>();

		public bool ItemGrabbed => GrabbedMergingItem != null;

		private bool _enabled;
		public bool Enabled
		{
			get => _enabled;
			set
			{
				_enabled = value;
				if (_enabled == false && ItemGrabbed)
				{
					GrabbedMergingItem = null;
				}
			}
		}

		private TItem _grabbedMergingItem;
		public TItem GrabbedMergingItem
		{
			get => _grabbedMergingItem;
			private set
			{
				if (_grabbedMergingItem == value) return;

				if (value != null)
				{
					OnItemGrabbed?.Invoke(value);
				}
				else
				{
					OnItemDropped?.Invoke(_grabbedMergingItem);

					if (_debug) Debug.Log($"[{gameObject.name}] {_grabbedMergingItem.gameObject.name} dropped.");
				}

				_grabbedMergingItem = value;
			}
		}

		#region Slots
		public List<I3dMergingItemSlot<TItem, TItemData>> Slots { get; set; }

		private bool _useSlots;
		public bool UseSlots
		{
			get => _useSlots;
			set
			{
				_useSlots = value;

				InitGetDraggingPointFunc();
			}
		}

		private I3dMergingItemSlot<TItem, TItemData> CurrentSlot { get; set; }
		private I3dMergingItemSlot<TItem, TItemData> SelectedSlot { get; set; }
		#endregion

		protected virtual void Awake()
		{
			_camera = Camera.main;
			_inputHandler = FindObjectOfType<MergeInputHandler>();

			switch (_draggingType)
			{
				case DraggingType.Lerp:
					_dragGrabbedItemAction = DragGrabbedItemLerp;
					break;
				case DraggingType.MoveTowards:
					_dragGrabbedItemAction = DragGrabbedItemMoveTowards;
					break;
			}

			if (_inputHandler == null) throw new NullReferenceException($"[{gameObject.name} (MergeController)] Merge Input Handler not found!");

			_inputHandler.OnPressedEvent += TryGrabItem;
			_inputHandler.OnReleasedEvent += TryMergeOrDropItem;
		}

		private void Start()
		{
			if (_refreshGrabbedItemTargetPointAction == null) InitGetDraggingPointFunc();
		}

		private void TryGrabItem()
		{
			if (Enabled == false) return;
			if (AvailableItems.Count == 0) return;

			Ray ray = _camera.ScreenPointToRay(_inputHandler.PointerPosition);
			var orderedItemsDotsMap = GetItemsDotsMap(ray.direction, AvailableItems)
				.Where(pair => pair.Key.CanMerge)
				.OrderByDescending(map => map.Value);

			var nearestItem = orderedItemsDotsMap.First();

			if (nearestItem.Value < _itemGrabbingTreshold) return;

			GrabbedMergingItem = nearestItem.Key;
			GrabbedMergingItem.MergeHandler.OnGrabbed();
			if (UseSlots) CurrentSlot = Slots.FirstOrDefault(slot => slot.Item == GrabbedMergingItem);

			StartCoroutine(ItemDraggingRoutine(GrabbedMergingItem));
			SuitableToMergeItems = AvailableItems
				.Where(item => item != GrabbedMergingItem && 
				item.Data.Equals(GrabbedMergingItem.Data) && 
				item.Data.MergeResultItemData != null).ToList();
			SuitableToMergeItems.ForEach(item => item.MergeHandler.OnSelectToMerge());

			if (_debug) Debug.Log($"[{gameObject.name}] {GrabbedMergingItem.gameObject.name} grabbed. (dot: {orderedItemsDotsMap.Last().Value})");
		}

		private void TryMergeOrDropItem()
		{
			if (Enabled == false) return;
			if (GrabbedMergingItem == null) return;

			SuitableToMergeItems.ForEach(item => item.MergeHandler.OnRejectToMerge());

			if(UseSlots)
			{
				if(SelectedSlot.IsEmpty == false)
				{
					if(SelectedSlot.Item != GrabbedMergingItem)
					{
						if(CanMerge(GrabbedMergingItem, SelectedSlot.Item, out var resultItemData))
						{
							StartCoroutine(SlotsMergeRoutine(GrabbedMergingItem, SelectedSlot.Item, SelectedSlot, resultItemData));
							return;
						}
						else GrabbedMergingItem.MergeTargetPoint = CurrentSlot.Point;
					}
				}
				else
				{
					SelectedSlot.Item = GrabbedMergingItem;
					CurrentSlot.Item = null;
				}
			}
			else if (SuitableToMergeItems.Count > 0)
			{
				Ray ray = _camera.ScreenPointToRay(_inputHandler.PointerPosition);
				var orderedItemsDotsMap = GetItemsDotsMap(ray.direction, SuitableToMergeItems)
					.OrderByDescending(map => map.Value);

				var nearestItem = orderedItemsDotsMap.First();

				if (nearestItem.Value >= _mergeTreshold && 
					CanMerge(GrabbedMergingItem, nearestItem.Key, out var resultItemData))
				{
					StartCoroutine(DefaultMergeRoutine(GrabbedMergingItem, nearestItem.Key, resultItemData));
					return;
				}
			}

			GrabbedMergingItem.MergeHandler.OnDropped();
			GrabbedMergingItem = null;
		}

		#region Merging
		public bool CanMerge(TItem firstItem, TItem secondItem, out TItemData resultItemData)
		{
			if (CanMerge(firstItem, secondItem))
			{
				resultItemData = (TItemData)firstItem.Data.MergeResultItemData;
				return true;
			}

			resultItemData = default;
			return false;
		}

		public bool CanMerge(TItem firstItem, TItem secondItem)
		{
			return Equals(firstItem.Data, secondItem.Data) && firstItem.Data.MergeResultItemData != null;
		}

		private IEnumerator DefaultMergeRoutine(TItem firstItem, TItem secondItem, TItemData resultItemData)
		{
			OnStartMerge?.Invoke(resultItemData);

			firstItem.CanMerge = false;
			secondItem.CanMerge = false;

			AvailableItems.Remove(firstItem);
			AvailableItems.Remove(secondItem);

			GrabbedMergingItem = null;

			var firstItemRoutine = StartCoroutine(firstItem.MergeHandler.MergeEffectRoutine());
			var secondItemRoutine = StartCoroutine(secondItem.MergeHandler.MergeEffectRoutine());

			yield return firstItemRoutine;
			yield return secondItemRoutine;

			var newItem = Instantiate(resultItemData.Prefab, secondItem.Position, Quaternion.identity);

			Destroy(firstItem.gameObject);
			Destroy(secondItem.gameObject);

			newItem.MergeHandler.OnSpawned();
			newItem.CanMerge = true;
			AvailableItems.Add(newItem);

			OnMergedEvent?.Invoke(newItem);
		}

		private IEnumerator SlotsMergeRoutine(TItem firstItem, TItem secondItem, I3dMergingItemSlot<TItem, TItemData> slot, TItemData resultItemData)
		{
			OnStartMerge?.Invoke(resultItemData);

			firstItem.CanMerge = false;
			secondItem.CanMerge = false;

			AvailableItems.Remove(firstItem);
			AvailableItems.Remove(secondItem);

			GrabbedMergingItem = null;
			CurrentSlot.Item = null;

			var firstItemRoutine = StartCoroutine(firstItem.MergeHandler.MergeEffectRoutine());
			var secondItemRoutine = StartCoroutine(secondItem.MergeHandler.MergeEffectRoutine());

			yield return firstItemRoutine;
			yield return secondItemRoutine;

			Destroy(firstItem.gameObject);
			Destroy(secondItem.gameObject);

			var newItem = Instantiate(resultItemData.Prefab, slot.Point, Quaternion.LookRotation(slot.ForwardDirection));
			newItem.MergeHandler.OnSpawned();
			newItem.CanMerge = true;
			AvailableItems.Add(newItem);
			slot.Item = newItem;

			OnMergedEvent?.Invoke(newItem);
		}
		#endregion

		protected virtual void Update()
		{
			if (Enabled == false) return;
			if (ItemGrabbed == false) return;

			_refreshGrabbedItemTargetPointAction.Invoke();
		}

		#region Getting Dragging Point

		private Ray _draggingRay;

		private void InitGetDraggingPointFunc()
		{
			if (UseSlots) _refreshGrabbedItemTargetPointAction = GetDraggingSlot;
			else _refreshGrabbedItemTargetPointAction = GetDraggingPointDefault;
		}

		private Vector3 _lastFramePointerPosition = Vector3.zero;
		private bool CheckInputChanged()
		{
			var result = _lastFramePointerPosition != _inputHandler.PointerPosition;
			_lastFramePointerPosition = _inputHandler.PointerPosition;

			return result;
		}

		private RaycastHit _draggingHitInfo;
		private RaycastHit RefreshDraggingHitInfo()
		{
			_draggingRay = _camera.ScreenPointToRay(_inputHandler.PointerPosition);
			Physics.Raycast(_draggingRay, out _draggingHitInfo, _draggingMaxDistance, _draggingFloorLayers);
			return _draggingHitInfo;
		}

		private Vector3 _itemDraggingPoint = Vector3.zero;
		private void GetDraggingPointDefault()
		{
			if (CheckInputChanged() == false) return;

			RefreshDraggingHitInfo();
			GrabbedMergingItem.MergeTargetPoint = _draggingHitInfo.point;
		}

		private void GetDraggingSlot()
		{
			if (Slots.Count == 0)
				throw new NullReferenceException($"[{gameObject.name} (MergeController)] GetDraggingPointWithSnapping: there is no any snapping points");
			if (CheckInputChanged() == false) return;

			RefreshDraggingHitInfo();
			SelectedSlot = Slots.OrderBy(slot => Vector3.Distance(slot.Point, _draggingHitInfo.point)).First();
			GrabbedMergingItem.MergeTargetPoint = SelectedSlot.Point;
		}
		#endregion

		#region Dragging
		private IEnumerator ItemDraggingRoutine(TItem item)
		{
			while (item != null && (GrabbedMergingItem == item || item.transform.position != item.MergeTargetPoint))
			{
				_dragGrabbedItemAction?.Invoke(item, item.MergeTargetPoint);

				yield return null;
			}	

			yield return null;
		}

		private void DragGrabbedItemLerp(TItem item, Vector3 targetPoint)
		{
			item.transform.position =
				Vector3.Lerp(item.transform.position, targetPoint, _draggingSpeed * Time.deltaTime);
		}

		private void DragGrabbedItemMoveTowards(TItem item, Vector3 targetPoint)
		{
			item.transform.position =
				Vector3.MoveTowards(item.transform.position, targetPoint, _draggingSpeed * Time.deltaTime);
		}
		#endregion

		#region Helpers
		private Vector3 _direction;
		private Dictionary<TItem, float> GetItemsDotsMap(Vector3 referenceDirection, List<TItem> availableItems)
		{
			Dictionary<TItem, float> map = new Dictionary<TItem, float>(availableItems.Count);

			foreach (var item in availableItems)
			{
				_direction = (item.transform.position - _camera.transform.position).normalized;
				map[item] = Vector3.Dot(referenceDirection, _direction);
			}

			return map;
		}
		#endregion
	}
}