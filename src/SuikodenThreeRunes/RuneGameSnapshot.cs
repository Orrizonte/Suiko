using System.Collections.Generic;

namespace SuikodenThreeRunes;

internal sealed class RuneGameSnapshot
{
	public readonly Dictionary<int, RuneStateSnapshot> Players = new Dictionary<int, RuneStateSnapshot>();

	public int MultiRuneCount;
}
