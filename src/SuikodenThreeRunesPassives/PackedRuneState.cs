namespace SuikodenThreeRunesPassives;

internal readonly struct PackedRuneState
{
	internal readonly byte[] RuneIds;

	internal readonly int ActiveIndex;

	internal PackedRuneState(byte[] runeIds, int activeIndex)
	{
		RuneIds = runeIds;
		ActiveIndex = activeIndex;
	}
}
