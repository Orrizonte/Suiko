using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace SuikodenThreeRunes;

// R174 : verification en jeu de la competence "Resistance magique" du mod Competences (fr.suikodenhd.skills).
// Les degats magiques natifs sont lus AVANT le mod Competences, puis APRES : un bandeau affiche, pour chaque sort
// qui touche un personnage, les degats du jeu, les degats finaux et la Resistance magique de la cible.
// Option [Diagnostic] Verifier_resistance_magique.
internal static class SkillsMagicDiagnostic
{
	internal const string SkillsGuid = "fr.suikodenhd.skills";

	private static int _nativeDamage = int.MinValue;

	private static int _lines;

	private static bool _lookupDone;

	private static MethodInfo? _getEffectPercent;

	private static MethodInfo? _getCharacterName;

	private static Type? _skillKindType;

	internal static bool Enabled => Plugin.CheckMagicResistance != null && Plugin.CheckMagicResistance.Value;

	internal static void Before(int result)
	{
		_nativeDamage = result;
	}

	internal static void After(string source, int attacker, int target, int result)
	{
		int nativeDamage = _nativeDamage;
		_nativeDamage = int.MinValue;
		if (!Enabled || nativeDamage == int.MinValue)
		{
			return;
		}
		try
		{
			PLAYER_BASE? targetPlayer = GetBattlePlayer(target);
			if (targetPlayer == null)
			{
				return;
			}
			PLAYER_BASE? attackerPlayer = GetBattlePlayer(attacker);
			string name = GetName(targetPlayer);
			int resistance = GetSkillPercent(targetPlayer, 7);
			string text = (attackerPlayer != null) ? ("sort allie (" + GetName(attackerPlayer) + ")") : "sort ennemi";
			string text2;
			if (resistance <= 0)
			{
				text2 = $"{name} : {nativeDamage} degats ({text}) - pas de Resistance magique";
			}
			else if (result < nativeDamage)
			{
				text2 = $"{name} : {nativeDamage} -> {result} degats ({text}) - Resistance magique {resistance}% APPLIQUEE";
			}
			else
			{
				text2 = $"{name} : {nativeDamage} -> {result} degats ({text}) - Resistance magique {resistance}% NON appliquee";
			}
			RuneController.Notify("Verification : " + text2, 4f);
			if (_lines < 200)
			{
				_lines++;
				Plugin.ModLog.LogInfo((object)$"R174 RESISTANCE MAGIQUE [{source}] lanceur={attacker} cible={target} : {text2}.");
			}
		}
		catch (Exception ex)
		{
			if (_lines < 200)
			{
				_lines++;
				Plugin.ModLog.LogWarning((object)("R174 verification resistance magique impossible : " + ex.Message));
			}
		}
	}

	private static PLAYER_BASE? GetBattlePlayer(int index)
	{
		BATTLE_WORK battle_work = BattleBase.battle_work;
		Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<P_BATTLE_DATA> val = ((battle_work != null) ? battle_work.p_battle_data : null);
		if (val == null || index < 0 || index >= val.Length)
		{
			return null;
		}
		return val[index]?.player_data?.player_base;
	}

	private static void Lookup()
	{
		if (_lookupDone)
		{
			return;
		}
		_lookupDone = true;
		Type? type = AppDomain.CurrentDomain.GetAssemblies().Select((Assembly a) => a.GetType("SuikodenSkills.SkillService", throwOnError: false)).FirstOrDefault((Type? t) => t != null);
		_skillKindType = AppDomain.CurrentDomain.GetAssemblies().Select((Assembly a) => a.GetType("SuikodenSkills.SkillKind", throwOnError: false)).FirstOrDefault((Type? t) => t != null);
		if (type != null)
		{
			BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			_getEffectPercent = type.GetMethod("GetEffectPercent", bindingAttr);
			_getCharacterName = type.GetMethod("GetCharacterName", bindingAttr);
		}
		Plugin.ModLog.LogInfo((object)("R174 verification resistance magique : mod Competences " + ((_getEffectPercent != null && _skillKindType != null) ? "trouve." : "introuvable (pourcentages non lisibles).")));
	}

	private static int GetSkillPercent(PLAYER_BASE player, int kind)
	{
		Lookup();
		if (_getEffectPercent == null || _skillKindType == null)
		{
			return 0;
		}
		return (int)_getEffectPercent.Invoke(null, new object[2] { player, Enum.ToObject(_skillKindType, kind) })!;
	}

	private static string GetName(PLAYER_BASE player)
	{
		Lookup();
		try
		{
			if (_getCharacterName?.Invoke(null, new object[1] { player }) is string text && text.Length > 0)
			{
				return text;
			}
		}
		catch
		{
		}
		return "Personnage " + player.chara_no;
	}
}

[HarmonyPatch(typeof(BattleBase), "calc_magic_damage")]
[HarmonyBefore(SkillsMagicDiagnostic.SkillsGuid)]
internal static class SkillsMagicDiagnosticBefore1
{
	[HarmonyPostfix]
	[HarmonyPriority(800)]
	private static void Postfix(int __result)
	{
		SkillsMagicDiagnostic.Before(__result);
	}
}

[HarmonyPatch(typeof(BattleBase), "calc_magic_damage")]
[HarmonyAfter(SkillsMagicDiagnostic.SkillsGuid)]
internal static class SkillsMagicDiagnosticAfter1
{
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(int attack, int guard, int __result)
	{
		SkillsMagicDiagnostic.After("calc_magic_damage", attack, guard, __result);
	}
}

[HarmonyPatch(typeof(BattleBase), "DamageHantei")]
[HarmonyBefore(SkillsMagicDiagnostic.SkillsGuid)]
internal static class SkillsMagicDiagnosticBefore2
{
	[HarmonyPostfix]
	[HarmonyPriority(800)]
	private static void Postfix(int __result)
	{
		SkillsMagicDiagnostic.Before(__result);
	}
}

[HarmonyPatch(typeof(BattleBase), "DamageHantei")]
[HarmonyAfter(SkillsMagicDiagnostic.SkillsGuid)]
internal static class SkillsMagicDiagnosticAfter2
{
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(int kogeki_mokuhyo, int player_no, int __result)
	{
		SkillsMagicDiagnostic.After("DamageHantei", player_no, kogeki_mokuhyo, __result);
	}
}

[HarmonyPatch(typeof(BattleBase), "DamageHantei2")]
[HarmonyBefore(SkillsMagicDiagnostic.SkillsGuid)]
internal static class SkillsMagicDiagnosticBefore3
{
	[HarmonyPostfix]
	[HarmonyPriority(800)]
	private static void Postfix(int __result)
	{
		SkillsMagicDiagnostic.Before(__result);
	}
}

[HarmonyPatch(typeof(BattleBase), "DamageHantei2")]
[HarmonyAfter(SkillsMagicDiagnostic.SkillsGuid)]
internal static class SkillsMagicDiagnosticAfter3
{
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(int kogeki_mokuhyo, int player_no, int __result)
	{
		SkillsMagicDiagnostic.After("DamageHantei2", player_no, kogeki_mokuhyo, __result);
	}
}
