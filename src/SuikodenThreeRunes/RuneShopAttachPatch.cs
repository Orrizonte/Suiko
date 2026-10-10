using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SuikodenThreeRunes;

// R164 (experimental) : le jeu refuse d'associer une rune au heros (Tir, porteur de la Mangeuse d'ames) meme
// quand son emplacement actif est libre (monsyo_id=0, exp=0, level=0 dans le log) : la regle depend du
// personnage. Pendant les fonctions natives d'association (monsyo_tsukeru*), si le curseur est sur un porteur
// de rune fixe dont un emplacement libre est actif, son chara_no est remplace le temps de l'appel par celui
// d'un autre membre de l'equipe, puis restaure. La rune fixe reste en emplacement 1.
[HarmonyPatch]
internal static class RuneShopAttachPatch
{
	internal sealed class Swap
	{
		internal PLAYER_BASE? Player;

		internal int OriginalCharaNo;

		internal byte RuneBefore;
	}

	private static IEnumerable<MethodBase> TargetMethods()
	{
		string[] array = new string[5] { "monsyo_tsukeru", "monsyo_tsukeru01", "monsyo_tsukeru02", "monsyo_tsukeru03", "monsyo_tsukeru99" };
		foreach (string text in array)
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(h_monsyo_c), text, new Type[1] { typeof(int) }, (Type[])null);
			if ((object)methodInfo != null)
			{
				yield return methodInfo;
			}
		}
	}

	[HarmonyPrefix]
	private static void Prefix(MethodBase __originalMethod, out Swap? __state)
	{
		__state = null;
		if (Plugin.AttachToFixedRuneHolders == null || !Plugin.AttachToFixedRuneHolders.Value)
		{
			return;
		}
		try
		{
			h_monsyo_c.MONSYO_WORK monsyo_work = h_monsyo_c.monsyo_work;
			Il2CppReferenceArray<PLAYER_DATA> val = ((monsyo_work != null) ? monsyo_work.player_data : null);
			if (val == null)
			{
				return;
			}
			int index = monsyo_work.index;
			if (index < 0 || index >= ((Il2CppArrayBase<PLAYER_DATA>)(object)val).Length)
			{
				return;
			}
			PLAYER_DATA obj = ((Il2CppArrayBase<PLAYER_DATA>)(object)val)[index];
			PLAYER_BASE val2 = ((obj != null) ? obj.player_base : null);
			if (val2 == null || val2.monsyo_data == null || !RuneSlotService.IsShopFreedFixedHolder(val2))
			{
				return;
			}
			int num = -1;
			for (int i = 0; i < ((Il2CppArrayBase<PLAYER_DATA>)(object)val).Length; i++)
			{
				PLAYER_DATA obj2 = ((Il2CppArrayBase<PLAYER_DATA>)(object)val)[i];
				PLAYER_BASE val3 = ((obj2 != null) ? obj2.player_base : null);
				if (val3 != null && val3.chara_no != val2.chara_no && !RuneSlotService.IsShopFreedFixedHolder(val3) && !RuneSlotService.HasFixedActiveRune(val3))
				{
					num = val3.chara_no;
					break;
				}
			}
			if (num < 0)
			{
				return;
			}
			__state = new Swap
			{
				Player = val2,
				OriginalCharaNo = val2.chara_no,
				RuneBefore = val2.monsyo_data.monsyo_id
			};
			val2.chara_no = num;
		}
		catch (Exception ex)
		{
			__state = null;
			Plugin.ModLog.LogWarning((object)("R164 association rune fixe impossible : " + ex.Message));
		}
	}

	[HarmonyFinalizer]
	private static Exception? Finalizer(Exception? __exception, MethodBase __originalMethod, Swap? __state)
	{
		if (__state?.Player != null)
		{
			try
			{
				__state.Player.chara_no = __state.OriginalCharaNo;
				byte b = __state.Player.monsyo_data?.monsyo_id ?? 0;
				if (b != __state.RuneBefore)
				{
					Plugin.ModLog.LogInfo((object)$"R164 association : personnage {__state.OriginalCharaNo} a recu {RuneSlotService.GetRuneName(b)} dans son emplacement libre ({__originalMethod.Name}); sa rune fixe reste en emplacement 1.");
				}
			}
			catch (Exception ex)
			{
				Plugin.ModLog.LogError((object)("R164 restauration du personnage impossible : " + ex.Message));
			}
		}
		return __exception;
	}
}
