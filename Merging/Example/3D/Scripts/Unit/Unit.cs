using UnityEngine;
using VladislavMang.Merging._3D;

namespace VladislavMang.Merging.Example._3D
{
	public class Unit : MonoBehaviour, I3dMergingItem<Unit, UnitData>
	{
		[Header("Data")]
		[SerializeField] private UnitData _data;

		public UnitData Data => _data;

		public bool CanMerge { get; set; }

		private UnitMergeHandler _mergeHandler;
		public IItemMergeHandler MergeHandler => _mergeHandler;

		public Vector3 Position => transform.position;
		public Vector3 MergeTargetPoint { get; set; }

		private void Awake()
		{
			_mergeHandler = GetComponent<UnitMergeHandler>();
		}
	}
}
