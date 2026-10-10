using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SuikodenThreeRunes;

// R161 : restauration manuelle des emplacements de rune, appliquee une fois au chargement d'une partie.
//
// Config [Restauration] Runes_a_restaurer, entrees separees par ';' :
//     chara:rune1/rune2/rune3
// Une rune est un ID (ex. 4) ou un nom (ex. "air", "Rune de l'air"); "-" ou "0" = vide.
// Securite : une entree n'est appliquee que si la rune actuellement equipee du personnage est celle de
// l'emplacement 1 de l'entree, et que ses emplacements 2/3 sont vides (on n'ecrase jamais un choix recent).
// Les entrees appliquees sont retirees de la config; les autres restent pour un prochain chargement.
internal static class RuneRestore
{
	internal static void Apply(GAME_WORK game)
	{
		string text = Plugin.RestoreRunes?.Value ?? string.Empty;
		LogCharacterTable(game);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (string text2 in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (!TryApplyEntry(game, text2))
			{
				list.Add(text2);
			}
		}
		string value = string.Join("; ", list);
		if (value != text)
		{
			Plugin.RestoreRunes.Value = value;
		}
	}

	private static bool TryApplyEntry(GAME_WORK game, string entry)
	{
		string[] array = entry.Split(':', 2, StringSplitOptions.TrimEntries);
		if (array.Length != 2 || !int.TryParse(array[0], out var result))
		{
			Plugin.ModLog.LogWarning((object)("R161 restauration : entree illisible '" + entry + "' (format chara:rune1/rune2/rune3)."));
			return false;
		}
		string[] array2 = array[1].Split(new char[2] { '/', ',' }, StringSplitOptions.TrimEntries);
		byte[] array3 = new byte[3];
		for (int i = 0; i < 3 && i < array2.Length; i++)
		{
			if (!TryParseRune(array2[i], out array3[i]))
			{
				Plugin.ModLog.LogWarning((object)("R161 restauration : rune inconnue '" + array2[i] + "' dans '" + entry + "'."));
				return false;
			}
		}
		PLAYER_BASE? player = FindPlayer(game, result);
		if (player == null || player.monsyo_data == null)
		{
			Plugin.ModLog.LogWarning((object)$"R161 restauration : personnage {result} introuvable dans cette partie.");
			return false;
		}
		if (!RuneSlotService.TryRestoreSlots(player, array3, out string reason))
		{
			Plugin.ModLog.LogWarning((object)$"R161 restauration ignoree pour le personnage {result} : {reason}");
			return false;
		}
		Plugin.ModLog.LogInfo((object)$"R161 restauration appliquee : personnage {result} -> {RuneSlotService.GetRuneName(array3[0])} / {RuneSlotService.GetRuneName(array3[1])} / {RuneSlotService.GetRuneName(array3[2])}.");
		return true;
	}

	private static PLAYER_BASE? FindPlayer(GAME_WORK game, int charaNo)
	{
		Il2CppReferenceArray<PLAYER_BASE> player_base = game.player_base;
		if (player_base == null)
		{
			return null;
		}
		for (int i = 0; i < ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base).Length; i++)
		{
			PLAYER_BASE val = ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base)[i];
			if (val != null && val.chara_no == charaNo)
			{
				return val;
			}
		}
		return null;
	}

	private static bool TryParseRune(string token, out byte runeId)
	{
		runeId = 0;
		if (token.Length == 0 || token == "-")
		{
			return true;
		}
		if (int.TryParse(token, out var result))
		{
			if (result < 0 || result >= 34)
			{
				return false;
			}
			runeId = (byte)result;
			return true;
		}
		string text = Normalize(token);
		for (byte b = 1; b < 34; b++)
		{
			string text2 = Normalize(RuneSlotService.GetRuneName(b));
			if (text2.Length > 0 && text2 == text)
			{
				runeId = b;
				return true;
			}
		}
		return false;
	}

	// "Rune de l'air" -> "air"; minuscules, sans accents.
	private static string Normalize(string value)
	{
		string text = value.Normalize(NormalizationForm.FormD);
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in text)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
			{
				stringBuilder.Append(char.ToLowerInvariant(c));
			}
		}
		string text2 = stringBuilder.ToString().Replace('’', '\'').Trim();
		string[] array = new string[4] { "rune de l'", "rune d'", "rune de ", "rune " };
		foreach (string text3 in array)
		{
			if (text2.StartsWith(text3, StringComparison.Ordinal))
			{
				text2 = text2.Substring(text3.Length);
				break;
			}
		}
		return text2.Trim();
	}

	// Liste des personnages qui portent une rune : aide a ecrire les entrees de restauration.
	private static void LogCharacterTable(GAME_WORK game)
	{
		try
		{
			Il2CppReferenceArray<PLAYER_BASE> player_base = game.player_base;
			if (player_base == null)
			{
				return;
			}
			List<string> list = new List<string>();
			for (int i = 0; i < ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base).Length; i++)
			{
				PLAYER_BASE val = ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base)[i];
				if (val != null && val.monsyo_data != null)
				{
					byte[] unlockedRuneIds = RuneSlotService.GetStoredRuneIds(val);
					if (unlockedRuneIds[0] != 0 || unlockedRuneIds[1] != 0 || unlockedRuneIds[2] != 0)
					{
						list.Add($"{val.chara_no}:{unlockedRuneIds[0]}/{unlockedRuneIds[1]}/{unlockedRuneIds[2]} (MAG {RuneSlotService.GetMagic(val)}, {RuneSlotService.GetRuneName(unlockedRuneIds[0])})");
					}
				}
			}
			Plugin.ModLog.LogInfo((object)("R161 personnages avec runes apres chargement : " + string.Join("; ", list)));
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("R161 liste des personnages impossible : " + ex.Message));
		}
	}
}
