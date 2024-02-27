using VladislavMang.Merging._3D;
using UnityEngine;

namespace VladislavMang.Merging.Example._3D
{
	[CreateAssetMenu(menuName = "MergingExample/UnitData")]
	public class UnitData : ScriptableObject, I3dMergingItemData<Unit>
	{
		[SerializeField] private int _id;
		[SerializeField] private Unit _prefab;
		[SerializeField] private UnitData _mergeResultItemData;

		public int Id => _id;
		public Unit Prefab => _prefab;
		public I3dMergingItemData<Unit> MergeResultItemData => _mergeResultItemData;


		// Some other unit data...
	}
}
