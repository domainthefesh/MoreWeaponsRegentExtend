namespace MoreWeaponsRegentExtend.Scripts.Events;

internal readonly record struct ArmoryPage(int Start, int Count, int Index, int PageCount);

internal static class ArmoryPagination
{
    // One page reserves a Back button. Multiple pages also reserve a cycling page button.
    internal static ArmoryPage GetPage(int itemCount, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemCount);
        var capacity = itemCount <= 3 ? 3 : 2;
        var pageCount = Math.Max(1, (itemCount + capacity - 1) / capacity);
        if (index < 0 || index >= pageCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        var start = index * capacity;
        return new ArmoryPage(start, Math.Min(capacity, itemCount - start), index, pageCount);
    }
}
