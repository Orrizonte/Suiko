using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace SuikodenThreeRunes;

internal static class DirectRuneDiagnostic
{
	internal sealed class Snap
	{
		internal string Before = string.Empty;
	}

	private static bool _armed;

	private static int _player;

	private static int _chara;

	private static int _slot;

	private static int _rune;

	private static int _startFrame;

	private static int _lines;

	private const int MaxLines = 120;

	internal static void Arm(int player, int chara, int rune, int slot)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		_armed = true;
		_player = player;
		_chara = chara;
		_rune = rune;
		_slot = slot + 1;
		_startFrame = Time.frameCount;
		_lines = 0;
		ManualLogSource modLog = Plugin.ModLog;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(88, 4, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("================ R128 DIRECT-RUNE DIAG BEGIN player=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(player);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" chara=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(chara);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" slot=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_slot);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" rune=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(rune);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" ================");
		}
		modLog.LogInfo(val);
		Plugin.ModLog.LogInfo((object)("R128 DIRECT-DIAG ARM " + State()));
	}

	internal static Snap Begin(MethodBase? method)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		if (!_armed)
		{
			return new Snap();
		}
		string text = method?.Name ?? "?";
		string text2 = State();
		if (_lines < 120)
		{
			_lines++;
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(24, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R128 DIRECT-DIAG ENTER ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text2);
			}
			modLog.LogInfo(val);
		}
		return new Snap
		{
			Before = text2
		};
	}

	internal static void End(MethodBase? method, int result, Snap snap)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		if (!_armed)
		{
			return;
		}
		string text = method?.Name ?? "?";
		string text2 = State();
		bool flag = default(bool);
		if (_lines < 120)
		{
			_lines++;
			ManualLogSource modLog = Plugin.ModLog;
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(44, 4, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R128 DIRECT-DIAG EXIT ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" ret=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(result);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" BEFORE[");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(snap.Before);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("] AFTER[");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text2);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("]");
			}
			modLog.LogInfo(val);
		}
		int num = ReadInt(BattleBase.window_work, "selected_player") ?? (-1);
		if (num >= 0 && _player >= 0 && num != _player)
		{
			ManualLogSource modLog2 = Plugin.ModLog;
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(29, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R128 DIRECT-DIAG ADVANCE ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_player);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("->");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text2);
			}
			modLog2.LogInfo(val);
			Disarm("personnage suivant");
		}
		else if (Time.frameCount - _startFrame > 900 || _lines >= 120)
		{
			Disarm("limite diagnostic");
		}
	}

	internal static string State()
	{
		object window_work = BattleBase.window_work;
		int num = ReadInt(window_work, "selected_player") ?? _player;
		string text = $"frame={Time.frameCount} player={num} chara={_chara} slot={_slot} rune={_rune} wi={Fmt(ReadInt(window_work, "window_index"))} step={Fmt(ReadInt(window_work, "window_step"))} level={Fmt(ReadInt(window_work, "index_level"))} item={Fmt(ReadInt(window_work, "selected_item"))} monster={Fmt(ReadInt(window_work, "selected_monster"))} magic_kazu={Fmt(ReadInt(window_work, "magic_kazu"))} winret={Fmt(ReadInt(window_work, "return_value"))}";
		try
		{
			object obj = ReadMember(BattleBase.battle_work, "pm_data");
			object obj2 = ((obj == null || num < 0) ? null : GetItem(obj, num));
			if (obj2 != null)
			{
				text = text + " koudou=" + Fmt(ReadInt(obj2, "c_koudou")) + " citem=" + Fmt(ReadInt(obj2, "c_item")) + " target=" + Fmt(ReadInt(obj2, "c_mokuhyo")) + " finished=" + Fmt(ReadInt(obj2, "finished_flg"));
			}
		}
		catch
		{
		}
		try
		{
			BATTLE_WORK battle_work = BattleBase.battle_work;
			if (((battle_work != null) ? battle_work.p_battle_data : null) != null && num >= 0 && num < ((Il2CppArrayBase<P_BATTLE_DATA>)(object)battle_work.p_battle_data).Length)
			{
				P_BATTLE_DATA obj4 = ((Il2CppArrayBase<P_BATTLE_DATA>)(object)battle_work.p_battle_data)[num];
				object obj5;
				if (obj4 == null)
				{
					obj5 = null;
				}
				else
				{
					PLAYER_DATA player_data = obj4.player_data;
					obj5 = ((player_data != null) ? player_data.player_base : null);
				}
				PLAYER_MONSYO val = ((obj5 != null) ? ((PLAYER_BASE)obj5).monsyo_data : null);
				if (val != null)
				{
					text += $" nativeRune={val.monsyo_id}/lvl{val.monsyo_level}/exp{val.monsyo_exp}";
				}
			}
			text = text + " WinWorkMagicNum=" + Fmt(ReadStaticInt(typeof(BattleBase), "WinWorkMagicNum"));
		}
		catch
		{
		}
		return text;
	}

	private static int? ReadStaticInt(Type type, string name)
	{
		BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		try
		{
			PropertyInfo property = type.GetProperty(name, bindingAttr);
			if ((object)property != null && property.CanRead)
			{
				object value = property.GetValue(null);
				if (value != null)
				{
					return Convert.ToInt32(value);
				}
			}
		}
		catch
		{
		}
		try
		{
			FieldInfo field = type.GetField(name, bindingAttr);
			if ((object)field != null)
			{
				object value2 = field.GetValue(null);
				if (value2 != null)
				{
					return Convert.ToInt32(value2);
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static void Disarm(string why)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		ManualLogSource modLog = Plugin.ModLog;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(62, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("================ R128 DIRECT-RUNE DIAG END (");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(why);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(") ================");
		}
		modLog.LogInfo(val);
		_armed = false;
	}

	private static object? ReadMember(object? obj, string name)
	{
		if (obj == null)
		{
			return null;
		}
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		Type type = obj.GetType();
		try
		{
			PropertyInfo property = type.GetProperty(name, bindingAttr);
			if ((object)property != null && property.CanRead && property.GetIndexParameters().Length == 0)
			{
				return property.GetValue(obj);
			}
		}
		catch
		{
		}
		try
		{
			FieldInfo field = type.GetField(name, bindingAttr);
			if ((object)field != null)
			{
				return field.GetValue(obj);
			}
		}
		catch
		{
		}
		return null;
	}

	private static int? ReadInt(object? obj, string name)
	{
		object obj2 = ReadMember(obj, name);
		if (obj2 == null)
		{
			return null;
		}
		try
		{
			return Convert.ToInt32(obj2);
		}
		catch
		{
			return null;
		}
	}

	private static object? GetItem(object obj, int index)
	{
		try
		{
			PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo in properties)
			{
				ParameterInfo[] indexParameters = propertyInfo.GetIndexParameters();
				if (propertyInfo.CanRead && indexParameters.Length == 1)
				{
					object obj2 = Convert.ChangeType(index, Nullable.GetUnderlyingType(indexParameters[0].ParameterType) ?? indexParameters[0].ParameterType);
					return propertyInfo.GetValue(obj, new object[1] { obj2 });
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string Fmt(int? v)
	{
		return v?.ToString() ?? "?";
	}
}
