using UnityEngine;

namespace VladislavMang.Merging._3D
{
    public interface I3dMergingItemData<T> where T : MonoBehaviour
    {
        T Prefab { get; }
        I3dMergingItemData<T> MergeResultItemData { get; }
    }
}
