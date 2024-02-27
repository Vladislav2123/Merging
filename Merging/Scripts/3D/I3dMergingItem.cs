using UnityEngine;

namespace VladislavMang.Merging._3D
{
	public interface I3dMergingItem<TItem, TData> 
		where TItem : MonoBehaviour, I3dMergingItem<TItem, TData>
		where TData : I3dMergingItemData<TItem>
	{
		TData Data { get; }
		IItemMergeHandler MergeHandler { get; }
		bool CanMerge { get; set; }
		Vector3 Position { get; }
		Vector3 MergeTargetPoint { get; set; }
	}
}
