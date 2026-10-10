namespace SuikodenThreeRunes;

internal sealed class RuneStateSnapshot
{
	public readonly byte[] RuneIds = new byte[3];

	public int ActiveIndex;

	public byte SoulEaterLevel;
}
