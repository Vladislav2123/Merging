using DG.Tweening;
using System.Collections;
using UnityEngine;

namespace VladislavMang.Merging.Example._3D
{
	public class UnitMergeHandler : MonoBehaviour, IItemMergeHandler
    {
		[SerializeField] private Transform _model;
		[SerializeField] private GameObject _selectionView;
		[Header("Grabbing")]
		[SerializeField] private float _mergingHeight;
		[SerializeField] private float _mergePickupAnimationDuration;
		[Header("Merge")]
		[SerializeField] private float _mergeAnimationDuration;
		[Header("Spawn")]
		[SerializeField] private float _spawnAnimationDuration;

		public void OnSpawned()
		{
			_model.localScale = Vector3.zero;
			_model.DOScale(Vector3.one, _spawnAnimationDuration);
		}

		public void OnGrabbed()
		{
			_model.DOLocalMoveY(_mergingHeight, _mergePickupAnimationDuration);
		}
		public void OnDropped()
		{
			_model.DOLocalMoveY(0, _mergePickupAnimationDuration);
		}

		public void OnSelectToMerge()
		{
			_selectionView.gameObject.SetActive(true);
		}

		public void OnRejectToMerge()
		{
			_selectionView.gameObject.SetActive(false);
		}

		public IEnumerator MergeEffectRoutine()
		{
			var sequense = DOTween.Sequence();

			yield return sequense.Append(_model.DOScale(Vector3.zero, _mergeAnimationDuration)).WaitForCompletion();
		}
	}
}
