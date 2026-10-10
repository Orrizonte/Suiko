using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SuikodenThreeRunesPassives;

internal static class PassiveDynamicHooks
{
	private sealed class TurtleStatusState
	{
		internal object? Pm;

		internal readonly List<(MemberInfo Member, object? Value)> Values = new List<(MemberInfo, object)>();
	}

	private sealed class CounterTurtleState
	{
		internal RuneSwapState CounterSwap;

		internal TurtleStatusState? Turtle;
	}

	private static readonly HashSet<string> Installed = new HashSet<string>(StringComparer.Ordinal);

	private static readonly string[] TurtleStatusKeywords = new string[14]
	{
		"status", "ijou", "ijyo", "poison", "dok", "sleep", "nemuri", "unbalance", "balance", "stun",
		"piyo", "baikin", "kemuri", "down"
	};

	private static readonly HashSet<string> DirectLogged = new HashSet<string>(StringComparer.Ordinal);

	internal static void Install(Harmony harmony)
	{
		Patch(harmony, "ac_set_action", "DoubleBeatPrefix", "SingleRestoreFinalizer", "Double-Beat -> ac_set_action");
		Patch(harmony, "player_attack_sub", "KillerPrefix", "SingleRestoreFinalizer", "Killer -> player_attack_sub");
		Patch(harmony, "monster_attack_sub", "CounterTurtlePrefix", "CounterTurtleFinalizer", "Counter + Turtle -> monster_attack_sub");
		Patch(harmony, "next_chara", "SparkPrefix", "ListRestoreFinalizer", "Spark -> next_chara");
		Patch(harmony, "get_monster_okane", "ProsperityPrefix", "ListRestoreFinalizer", "Prosperity -> get_monster_okane");
		Patch(harmony, "monster_rootine", "PheroPrefix", "ListRestoreFinalizer", "Phero -> monster_rootine");
		Patch(harmony, "make_monster_data", "PheroPrefix", "ListRestoreFinalizer", "Phero -> make_monster_data");
		Patch(harmony, "set_player_finished", null, null, "Sunbeam fallback -> set_player_finished", "SunbeamFinishedPostfix");
		if (Plugin.EnableExperimentalFieldPassives.Value)
		{
			InstallFieldDiscovery(harmony);
		}
	}

	private static void Patch(Harmony harmony, string targetName, string? prefixName, string? finalizerName, string label, string? postfixName = null)
	{
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Expected O, but got Unknown
		bool flag = default(bool);
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(BattleBase), targetName, (Type[])null, (Type[])null);
			if ((object)methodInfo == null)
			{
				ManualLogSource modLog = Plugin.ModLog;
				BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(76, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R108 : point natif introuvable: BattleBase.");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(targetName);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; passif correspondant non force.");
				}
				modLog.LogWarning(val);
				return;
			}
			HarmonyMethod val2 = ((prefixName == null) ? ((HarmonyMethod)null) : new HarmonyMethod(AccessTools.Method(typeof(PassiveDynamicHooks), prefixName, (Type[])null, (Type[])null)));
			HarmonyMethod val3 = ((postfixName == null) ? ((HarmonyMethod)null) : new HarmonyMethod(AccessTools.Method(typeof(PassiveDynamicHooks), postfixName, (Type[])null, (Type[])null)));
			HarmonyMethod val4 = ((finalizerName == null) ? ((HarmonyMethod)null) : new HarmonyMethod(AccessTools.Method(typeof(PassiveDynamicHooks), finalizerName, (Type[])null, (Type[])null)));
			harmony.Patch((MethodBase)methodInfo, val2, val3, (HarmonyMethod)null, val4, (HarmonyMethod)null);
			Installed.Add(targetName);
			ManualLogSource modLog2 = Plugin.ModLog;
			BepInExInfoLogInterpolatedStringHandler val5 = new BepInExInfoLogInterpolatedStringHandler(28, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral("R108 routeur installe : ");
				((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<string>(label);
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral(" (");
				((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<MethodInfo>(methodInfo);
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral(").");
			}
			modLog2.LogInfo(val5);
		}
		catch (Exception ex)
		{
			ManualLogSource modLog3 = Plugin.ModLog;
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(23, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R108 : echec routeur ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(label);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(": ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			modLog3.LogWarning(val);
		}
	}

	private static bool TryResolvePlayerFromArgs(object[]? args, out PLAYER_BASE? player, out int battleIndex)
	{
		player = null;
		battleIndex = -1;
		if (args == null)
		{
			return false;
		}
		foreach (object obj in args)
		{
			int num;
			try
			{
				if (obj == null)
				{
					continue;
				}
				num = Convert.ToInt32(obj);
				goto IL_0026;
			}
			catch
			{
			}
			continue;
			IL_0026:
			if (PassiveRuneSlots.TryGetBattlePlayer(num, out player) && player != null)
			{
				battleIndex = num;
				return true;
			}
		}
		player = null;
		battleIndex = -1;
		return false;
	}

	private static void DoubleBeatPrefix(object[] __args, out RuneSwapState __state)
	{
		__state = default(RuneSwapState);
		if (TryResolvePlayerFromArgs(__args, out PLAYER_BASE player, out int _) && player != null)
		{
			__state = PassiveRuneSlots.PresentHiddenRuneToNative(player, Plugin.DoubleBeatRuneId.Value, "Double-Beat");
		}
	}

	private static void KillerPrefix(object[] __args, out RuneSwapState __state)
	{
		__state = default(RuneSwapState);
		if (TryResolvePlayerFromArgs(__args, out PLAYER_BASE player, out int _) && player != null)
		{
			__state = PassiveRuneSlots.PresentHiddenRuneToNative(player, Plugin.KillerRuneId.Value, "Tueur/Killer");
		}
	}

	private static void CounterTurtlePrefix(object[] __args, out CounterTurtleState __state)
	{
		__state = new CounterTurtleState();
		if (TryResolvePlayerFromArgs(__args, out PLAYER_BASE player, out int battleIndex) && player != null)
		{
			__state.CounterSwap = PassiveRuneSlots.PresentHiddenRuneToNative(player, Plugin.CounterRuneId.Value, "Contre/Counter");
			if (PassiveRuneSlots.HasHiddenRune(player, Plugin.TurtleRuneId.Value))
			{
				__state.Turtle = CaptureTurtleStatus(battleIndex, player);
			}
		}
	}

	private static Exception? CounterTurtleFinalizer(Exception? __exception, CounterTurtleState __state)
	{
		PassiveRuneSlots.Restore(__state.CounterSwap);
		RestoreTurtleStatus(__state.Turtle);
		return __exception;
	}

	private static void SparkPrefix(out RuneSwapListState __state)
	{
		__state = PassiveRuneSlots.PresentHiddenRuneToAllBattleHolders(Plugin.SparkRuneId.Value, "Spark");
	}

	private static void ProsperityPrefix(out RuneSwapListState __state)
	{
		__state = PassiveRuneSlots.PresentHiddenRuneToAllBattleHolders(Plugin.ProsperityRuneId.Value, "Prosperity");
	}

	private static void PheroPrefix(out RuneSwapListState __state)
	{
		__state = PassiveRuneSlots.PresentHiddenRuneToAllBattleHolders(Plugin.PheroRuneId.Value, "Phero");
	}

	private static Exception? SingleRestoreFinalizer(Exception? __exception, RuneSwapState __state)
	{
		PassiveRuneSlots.Restore(__state);
		return __exception;
	}

	private static Exception? ListRestoreFinalizer(Exception? __exception, RuneSwapListState __state)
	{
		PassiveRuneSlots.Restore(__state);
		return __exception;
	}

	private static void SunbeamFinishedPostfix(object[] __args)
	{
		try
		{
			if (!TryResolvePlayerFromArgs(__args, out PLAYER_BASE player, out int battleIndex) || player == null || !PassiveRuneSlots.HasHiddenRune(player, Plugin.SunbeamRuneId.Value))
			{
				return;
			}
			BATTLE_WORK battle_work = BattleBase.battle_work;
			Il2CppReferenceArray<PM_DATA> val = ((battle_work != null) ? battle_work.pm_data : null);
			if (val == null || battleIndex < 0 || battleIndex >= ((Il2CppArrayBase<PM_DATA>)(object)val).Length || ((Il2CppArrayBase<PM_DATA>)(object)val)[battleIndex] == null)
			{
				return;
			}
			object obj = ((Il2CppArrayBase<PM_DATA>)(object)val)[battleIndex];
			if (TryReadInt(obj, "hp", out var value) && TryReadInt(obj, "max_hp", out var value2) && value > 0 && value2 > 0)
			{
				int num = Math.Min(value2, value + 5);
				if (num != value && TryWriteInt(obj, "hp", num))
				{
					LogDirectOnce(player, Plugin.SunbeamRuneId.Value, "Sunbeam combat +5 HP");
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("R108 Sunbeam combat ignoree pour ce tour : " + ex.Message));
		}
	}

	private static TurtleStatusState? CaptureTurtleStatus(int battleIndex, PLAYER_BASE player)
	{
		try
		{
			BATTLE_WORK battle_work = BattleBase.battle_work;
			Il2CppReferenceArray<PM_DATA> val = ((battle_work != null) ? battle_work.pm_data : null);
			if (val == null || battleIndex < 0 || battleIndex >= ((Il2CppArrayBase<PM_DATA>)(object)val).Length || ((Il2CppArrayBase<PM_DATA>)(object)val)[battleIndex] == null)
			{
				return null;
			}
			object obj = ((Il2CppArrayBase<PM_DATA>)(object)val)[battleIndex];
			TurtleStatusState turtleStatusState = new TurtleStatusState
			{
				Pm = obj
			};
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			Type type = obj.GetType();
			PropertyInfo[] properties = type.GetProperties(bindingAttr);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (propertyInfo.CanRead && propertyInfo.CanWrite && propertyInfo.GetIndexParameters().Length == 0 && IsStatusName(propertyInfo.Name) && IsSimpleStatusType(propertyInfo.PropertyType))
				{
					try
					{
						turtleStatusState.Values.Add((propertyInfo, propertyInfo.GetValue(obj)));
					}
					catch
					{
					}
				}
			}
			FieldInfo[] fields = type.GetFields(bindingAttr);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (IsStatusName(fieldInfo.Name) && IsSimpleStatusType(fieldInfo.FieldType))
				{
					try
					{
						turtleStatusState.Values.Add((fieldInfo, fieldInfo.GetValue(obj)));
					}
					catch
					{
					}
				}
			}
			if (turtleStatusState.Values.Count > 0)
			{
				LogDirectOnce(player, Plugin.TurtleRuneId.Value, "Turtle protection etats (" + string.Join(",", turtleStatusState.Values.Select<(MemberInfo, object), string>(((MemberInfo Member, object Value) v) => v.Member.Name).Distinct()) + ")");
			}
			return turtleStatusState;
		}
		catch
		{
			return null;
		}
	}

	private static void RestoreTurtleStatus(TurtleStatusState? state)
	{
		if (state?.Pm == null)
		{
			return;
		}
		foreach (var (memberInfo, value) in state.Values)
		{
			try
			{
				if (memberInfo is PropertyInfo { CanWrite: not false } propertyInfo)
				{
					propertyInfo.SetValue(state.Pm, value);
				}
				else if (memberInfo is FieldInfo fieldInfo)
				{
					fieldInfo.SetValue(state.Pm, value);
				}
			}
			catch
			{
			}
		}
	}

	private static bool IsStatusName(string name)
	{
		string i = name.ToLowerInvariant();
		return TurtleStatusKeywords.Any((string k) => i.Contains(k));
	}

	private static bool IsSimpleStatusType(Type t)
	{
		if (!(t == typeof(byte)) && !(t == typeof(sbyte)) && !(t == typeof(short)) && !(t == typeof(ushort)) && !(t == typeof(int)) && !(t == typeof(uint)))
		{
			return t == typeof(bool);
		}
		return true;
	}

	private static void LogDirectOnce(PLAYER_BASE p, int runeId, string effect)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		if (!Plugin.LogFirstActivation.Value)
		{
			return;
		}
		string item = $"direct:{p.chara_no}:{runeId}:{effect}";
		if (DirectLogged.Add(item))
		{
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(88, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R108 PASSIF 3-SLOTS - ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(effect);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(": chara=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(p.chara_no);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", rune secondaire=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(runeId);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", appliquee sans modifier le slot actif.");
			}
			modLog.LogInfo(val);
		}
	}

	private static bool TryReadInt(object obj, string name, out int value)
	{
		value = 0;
		try
		{
			Type type = obj.GetType();
			object obj2 = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj);
			if (obj2 == null)
			{
				obj2 = type.GetField(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj);
			}
			if (obj2 == null)
			{
				return false;
			}
			value = Convert.ToInt32(obj2);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryWriteInt(object obj, string name, int value)
	{
		try
		{
			Type type = obj.GetType();
			PropertyInfo property = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if ((object)property != null && property.CanWrite)
			{
				property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
				return true;
			}
			FieldInfo field = type.GetField(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if ((object)field != null)
			{
				field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void InstallFieldDiscovery(Harmony harmony)
	{
		try
		{
			Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => string.Equals(a.GetName().Name, "GSD1", StringComparison.OrdinalIgnoreCase));
			if ((object)assembly == null)
			{
				return;
			}
			string[] source = new string[4] { "dash", "encount", "encounter", "step" };
			List<string> list = new List<string>();
			foreach (Type item in SafeGetTypes(assembly))
			{
				MethodInfo[] methods = item.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					string i = methodInfo.Name.ToLowerInvariant();
					if (source.Any((string k) => i.Contains(k)))
					{
						list.Add(item.FullName + "." + methodInfo.Name);
					}
				}
			}
			if (list.Count > 0)
			{
				Plugin.ModLog.LogInfo((object)("R108 terrain : candidats detectes (diagnostic, non patches aveuglement) : " + string.Join(" | ", list.Take(20))));
			}
			else
			{
				Plugin.ModLog.LogInfo((object)"R108 terrain : aucun point nomme dash/encount/step expose dans GSD1; Holy/True Holy/Champion/Sunbeam-marche restent a valider separement.");
			}
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("R108 terrain : inspection impossible : " + ex.Message));
		}
	}

	private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where((Type t) => (object)t != null).Cast<Type>();
		}
		catch
		{
			return Array.Empty<Type>();
		}
	}
}
