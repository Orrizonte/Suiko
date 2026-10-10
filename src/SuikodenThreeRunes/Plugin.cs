using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SuikodenThreeRunes;

[BepInPlugin("fr.suikodenhd.threerunes", "Suikoden I - Trois runes selon la MAG", "0.9.22-r171")]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
	public const string Guid = "fr.suikodenhd.threerunes";

	public const string Name = "Suikoden I - Trois runes selon la MAG";

	public const string Version = "0.9.22-r171";

	private const string SupportedGameAssemblySha256 = "e093f175eaf3734d293ca0a236c652dd7fcc72ea5fa10d7b21103edf12f11c3a";

	internal static ManualLogSource ModLog;

	internal static ConfigEntry<int> SecondSlotMag;

	internal static ConfigEntry<int> ThirdSlotMag;

	internal static ConfigEntry<bool> ShowOverlay;

	internal static ConfigEntry<bool> ShowShopSlots;

	internal static ConfigEntry<string> RestoreRunes;

	internal static ConfigEntry<bool> AttachToFixedRuneHolders;

	private Harmony? _harmony;

	public override void Load()
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		ModLog = ((BasePlugin)this).Log;
		if (IsSupportedGameBuild())
		{
			SecondSlotMag = ((BasePlugin)this).Config.Bind<int>("Emplacements", "MAG_Deuxieme_Rune", 60, "Statistique MAG minimale pour ouvrir le deuxieme emplacement (0 a 255).");
			ThirdSlotMag = ((BasePlugin)this).Config.Bind<int>("Emplacements", "MAG_Troisieme_Rune", 120, "Statistique MAG minimale pour ouvrir le troisieme emplacement (0 a 255).");
			ShowOverlay = ((BasePlugin)this).Config.Bind<bool>("Affichage", "Afficher_Indication", true, "Affiche brievement la rune active, les trois emplacements et la MAG.");
			ShowShopSlots = ((BasePlugin)this).Config.Bind<bool>("Affichage", "Afficher_3_Emplacements_Boutique", true, "Affiche les trois emplacements et leur selection dans la boutique de runes.");
			AttachToFixedRuneHolders = ((BasePlugin)this).Config.Bind<bool>("Boutique", "Associer_porteur_rune_fixe", true, "Experimental : permet d'associer une rune dans un emplacement libre d'un porteur de rune fixe (Tir et la Mangeuse d'ames). La rune fixe reste en emplacement 1.");
			RestoreRunes = ((BasePlugin)this).Config.Bind<string>("Restauration", "Runes_a_restaurer", "", "Restauration manuelle appliquee une fois au chargement d'une partie. Format : chara:rune1/rune2/rune3 separes par ';' (ID ou nom de rune, '-' = vide). Une entree n'est appliquee que si la rune equipee correspond a l'emplacement 1 et que les emplacements 2/3 sont vides; elle est ensuite retiree.");
			// R162 : runes retrouvees dans la sauvegarde de l'utilisateur; l'ancienne valeur pre-remplie est retiree
			// pour ne jamais re-ajouter une rune volontairement enlevee.
			if (RestoreRunes.Value.Trim() == "1:2/4/0; 17:5/18/0; 23:9/28/0")
			{
				RestoreRunes.Value = string.Empty;
			}
			NormalizeThresholds();
			RuneSlotService.Reset();
			_harmony = new Harmony("fr.suikodenhd.threerunes");
			Passives.PassiveConfig.Bind(((BasePlugin)this).Config);
			SafePatchAll(_harmony, ModLog);
			Passives.PassiveConfig.Install(_harmony);
			LogSaveMethods();
			((BasePlugin)this).AddComponent<RuneController>();
			ManualLogSource modLog = ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(24, 4, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("Suikoden I - Trois runes selon la MAG");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("0.9.22-r171");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" charge. Seuils MAG: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(SecondSlotMag.Value);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(ThirdSlotMag.Value);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
			ModLog.LogInfo((object)"R159 : etat 3 runes garde en memoire de jeu en permanence (toute sauvegarde le contient); compatible avec les anciennes sauvegardes.");
			ModLog.LogInfo((object)"R156 CLEAN : UI R148/R151 conservee; aucune nouvelle fenetre. Le meme m_emblem natif reste le selecteur 3-runes.");
			ModLog.LogInfo((object)"R156 CLEAN : Pie-grieche active utilise temporairement une rune magique secondaire comme carrier natif pour rouvrir le meme selecteur 3-runes; stockage et ActiveIndex restent inchanges.");
			ModLog.LogInfo((object)"R156 CLEAN : si le selecteur attend B mais que le dispatcher est sur command_magic02/10, ce seul handler est route vers command_magic01 natif afin que Cancel soit reellement lu.");
			ModLog.LogInfo((object)"R156 CLEAN : le vrai m_emblem est pre-arme des le A sur Runes puis revalide au prefix OpenWindowUI(Emblem), avant la toute premiere animation; retour Eau/Magic inchange.");
			ModLog.LogInfo((object)"R156 DIRECT-RUNTIME : Pie-grieche garde le stockage 3-runes en memoire mais presente monsyo_exp=0 au moteur natif pendant ciblage/execution; garde player_magic_sub installee.");
			ModLog.LogInfo((object)"R156 PIE-GRIECHE : c_item est maintenu a 0 pendant le chemin direct natif; aucune autre donnee de commande n'est forcee. UI R148/R151 conservee.");
		}
	}

	// Applique chaque classe de patch separement : un point natif introuvable ne desactive plus tout le mod.
	internal static int SafePatchAll(Harmony harmony, ManualLogSource log)
	{
		int num = 0;
		Type[] types;
		try
		{
			types = typeof(Plugin).Assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			types = Array.FindAll(ex.Types, (Type? t) => t != null)!;
		}
		foreach (Type type in types)
		{
			if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Length == 0)
			{
				continue;
			}
			try
			{
				harmony.CreateClassProcessor(type).Patch();
				num++;
			}
			catch (Exception ex2)
			{
				log.LogError((object)("Patch " + type.Name + " non applique (fonction concernee desactivee) : " + ex2.GetBaseException().Message));
			}
		}
		return num;
	}

	// Diagnostic : fonctions de SaveDataManager disponibles (chemins de sauvegarde/chargement du jeu).
	private static void LogSaveMethods()
	{
		try
		{
			MethodInfo[] methods = typeof(SaveDataManager).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
			string[] array = Array.ConvertAll(methods, (MethodInfo m) => m.Name);
			Array.Sort(array, StringComparer.Ordinal);
			ModLog.LogInfo((object)("R159 diagnostic SaveDataManager : " + string.Join(", ", array)));
			MethodInfo[] methods2 = typeof(h_monsyo_c).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
			string[] array2 = Array.ConvertAll(methods2, (MethodInfo m) => m.Name + "(" + string.Join(",", Array.ConvertAll(m.GetParameters(), (ParameterInfo p) => p.ParameterType.Name)) + ")");
			Array.Sort(array2, StringComparer.Ordinal);
			ModLog.LogInfo((object)("R162 diagnostic boutique h_monsyo_c : " + string.Join(", ", array2)));
		}
		catch (Exception ex)
		{
			ModLog.LogWarning((object)("R159 diagnostic SaveDataManager impossible : " + ex.Message));
		}
	}

	private static bool IsSupportedGameBuild()
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		try
		{
			using FileStream inputStream = File.OpenRead(Path.Combine(Paths.GameRootPath, "GameAssembly.dll"));
			using SHA256 sHA = SHA256.Create();
			string text = Convert.ToHexString(sHA.ComputeHash(inputStream)).ToLowerInvariant();
			if (text == "e093f175eaf3734d293ca0a236c652dd7fcc72ea5fa10d7b21103edf12f11c3a")
			{
				return true;
			}
			ModLog.LogError((object)("Version du jeu non prise en charge. Le mod Trois runes reste desactive pour proteger la partie (GameAssembly SHA-256: " + text + ")."));
		}
		catch (Exception ex)
		{
			ManualLogSource modLog = ModLog;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(61, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Verification de la version du jeu impossible; mod desactive: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			modLog.LogError(val);
		}
		return false;
	}

	private static void NormalizeThresholds()
	{
		SecondSlotMag.Value = Math.Clamp(SecondSlotMag.Value, 0, 255);
		ThirdSlotMag.Value = Math.Clamp(ThirdSlotMag.Value, SecondSlotMag.Value, 255);
	}
}
