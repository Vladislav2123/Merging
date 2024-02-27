using System.Collections;

namespace VladislavMang.Merging
{
    public interface IItemMergeHandler
    {
        IEnumerator MergeEffectRoutine();
        void OnSpawned();
        void OnGrabbed();
        void OnDropped();
        void OnSelectToMerge();
        void OnRejectToMerge();
    }
}
