namespace SuikodenThreeRunesPassives;

internal struct RuneSwapState
{
	internal PLAYER_BASE? Player;

	internal byte OriginalId;

	internal byte OriginalLevel;

	internal bool Changed;

	internal int PresentedRuneId;
}
