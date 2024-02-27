using System;
using System.Collections.Generic;

namespace VladislavMang.Merging
{
    public interface IMergeController<TMergingItem, TMergingItemData>
    {
        Action<TMergingItem> OnItemGrabbed { get; set; }
        Action<TMergingItem> OnItemDropped { get; set; }
        Action<TMergingItemData> OnStartMerge { get; set; }
        Action<TMergingItem> OnMergedEvent { get; set; }

        List<TMergingItem> AvailableItems { get; set; }

        TMergingItem GrabbedMergingItem { get; }
        bool Enabled { get; set; }
        bool ItemGrabbed { get; }

    }
}
