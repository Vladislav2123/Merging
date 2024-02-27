using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VladislavMang.Merging.Example._3D
{
	public class UnitsController : MonoBehaviour
	{
		private enum SpawningType { Random, Slots }

		[Header("Spawning")]
		[SerializeField] SpawningType _spawningType;
		[SerializeField] private Button _spawnButton;
		[Header("Random Spawning")]
		[SerializeField] private Transform _spawnPoint;
		[SerializeField] private float _spawnRadius;
		[SerializeField] private UnitData _spawningUnitData;
		[Header("Slots Spawning")]
		[SerializeField] private List<UnitSlot> _slots;

		[Header("Merging")]
		[SerializeField] private UnitsMergeController3DExample _mergeController;

		private void Awake()
		{
			_slots.ForEach(slot => slot.gameObject.SetActive(_spawningType == SpawningType.Slots));

			_mergeController.Slots = _slots.Select(slot => slot as I3dMergingItemSlot<Unit, UnitData>).ToList();
			_mergeController.UseSlots = _spawningType == SpawningType.Slots;

			_spawnButton.onClick.AddListener(TrySpawnUnit);
		}

		private void Start()
		{
			_mergeController.Enabled = true;
		}

		private void TrySpawnUnit()
		{
			Unit newUnit = null;
			Vector3 spawnPoint = Vector3.zero;

			switch (_spawningType)
			{
				case SpawningType.Random:
					spawnPoint = _spawnPoint.position + Random.insideUnitSphere * _spawnRadius;
					spawnPoint.y = _spawnPoint.position.y;
					newUnit = Instantiate(_spawningUnitData.Prefab, spawnPoint, Quaternion.identity);
					break;

				case SpawningType.Slots:
					var emptySlot = _slots.FirstOrDefault(slot => slot.IsEmpty);
					if (emptySlot == null) return;
					spawnPoint = emptySlot.transform.position;
					newUnit = Instantiate(_spawningUnitData.Prefab, spawnPoint, Quaternion.identity);
					emptySlot.Unit = newUnit;
					break;
			}

			newUnit.CanMerge = true;
			_mergeController.AvailableItems.Add(newUnit);
		}

		private void OnDrawGizmos()
		{
			if (_spawnPoint == null) return;

			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(_spawnPoint.position, _spawnRadius);
		}
	}
}
