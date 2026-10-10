namespace SuikodenThreeRunes;

internal sealed class RuneState
{
	public readonly byte[] RuneIds = new byte[3];

	public int ActiveIndex;

	public byte SoulEaterLevel;
}
