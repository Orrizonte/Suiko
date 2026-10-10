namespace SuikodenThreeRunes;

internal sealed class RuneStateSnapshot
{
	public readonly byte[] RuneIds = new byte[3];

	public int ActiveIndex;

	public byte SoulEaterLevel;

	// Valeur de monsyo_exp en jeu avant la copie de sauvegarde, restauree juste apres.
	public ushort NativeExp;
}
