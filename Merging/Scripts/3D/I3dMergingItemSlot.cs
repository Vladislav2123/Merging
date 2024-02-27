using UnityEngine;
using VladislavMang.Merging._3D;

namespace VladislavMang.Merging
{
    public interface I3dMergingItemSlot<TItem, TItemData> 
        where TItem : MonoBehaviour, I3dMergingItem<TItem, TItemData>
        where TItemData : I3dMergingItemData<TItem>
    {
        TItem Item { get; set; }
        public Vector3 Point { get; }
        public Vector3 ForwardDirection { get; }
        bool IsEmpty { get; }
    }
}
