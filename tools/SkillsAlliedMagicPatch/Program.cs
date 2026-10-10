// Patch IL de SuikodenSkills.dll 0.2.52 : dans SkillBattleEffects.ApplyMagicDamage, si le lanceur ET la cible
// sont des personnages (soin, sort de soutien), aucun bonus Magie ni malus Resistance magique n est applique.
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Linq;
// Usage: ilpatch <in.dll> <out.dll>
var rp = new ReaderParameters { ReadWrite = false };
var m = ModuleDefinition.ReadModule(args[0], rp);
var t = m.GetType("SuikodenSkills.SkillBattleEffects");
var me = t.Methods.Single(x => x.Name == "ApplyMagicDamage");
var plugin = m.GetType("SuikodenSkills.Plugin");
var modLog = plugin.Fields.Single(f => f.Name == "ModLog");
// methode LogInfo(object) de ManualLogSource : reprise d'une reference existante dans le module
var logInfo = m.GetMemberReferences().OfType<MethodReference>().First(r => r.DeclaringType.Name == "ManualLogSource" && r.Name == "LogInfo" && r.Parameters.Count == 1 && r.Parameters[0].ParameterType.FullName == "System.Object");
// champ "deja journalise"
var flag = new FieldDefinition("_alliedMagicLogged", FieldAttributes.Private | FieldAttributes.Static, m.TypeSystem.Boolean);
t.Fields.Add(flag);
me.Body.SimplifyMacros();
var il = me.Body.GetILProcessor();
var ins = me.Body.Instructions;
// point d'insertion : premier "ldloc.3" suivi de "ldc.i4.0; bgt" (test des pourcentages)
var anchor = ins.First(i => (i.OpCode == OpCodes.Ldloc && ((VariableDefinition)i.Operand).Index == 3) && i.Next.OpCode == OpCodes.Ldc_I4 && (int)i.Next.Operand == 0 && i.Next.Next.OpCode == OpCodes.Bgt);
var ret = il.Create(OpCodes.Ret);
var skipLog = il.Create(OpCodes.Ret);
var seq = new[] {
  il.Create(OpCodes.Ldloc, me.Body.Variables[0]), il.Create(OpCodes.Brfalse, anchor),
  il.Create(OpCodes.Ldloc, me.Body.Variables[1]), il.Create(OpCodes.Brfalse, anchor),
  // lanceur et cible sont deux personnages (soin, sort sur un allie) : aucun bonus/malus
  il.Create(OpCodes.Ldsfld, flag), il.Create(OpCodes.Brtrue, skipLog),
  il.Create(OpCodes.Ldc_I4_1), il.Create(OpCodes.Stsfld, flag),
  il.Create(OpCodes.Ldsfld, modLog),
  il.Create(OpCodes.Ldstr, "R1 Magie/Resistance magique : sort entre allies (soin ou sort de soutien) laisse intact; les bonus ne s'appliquent qu'entre personnage et monstre."),
  il.Create(OpCodes.Callvirt, logInfo),
  skipLog,
};
foreach (var s in seq) il.InsertBefore(anchor, s);
me.Body.OptimizeMacros();
// ---- 0.2.54 : verification visible de la Resistance magique ----
// ApplyMagicDamage d'origine -> ApplyMagicDamage_Core ; nouvelle ApplyMagicDamage = { avant = degats; Core(...); Verifier(...) }
var notify = m.GetType("SuikodenSkills.SkillRuntimeController").Methods.Single(x => x.Name == "Notify");
var service = m.GetType("SuikodenSkills.SkillService");
var getPct = service.Methods.Single(x => x.Name == "GetEffectPercent");
var getName = service.Methods.Single(x => x.Name == "GetCharacterName");
var tryGet = t.Methods.Single(x => x.Name == "TryGetBattlePlayer");
var concat = m.GetMemberReferences().OfType<MethodReference>().First(r => r.DeclaringType.FullName == "System.String" && r.Name == "Concat" && r.Parameters.Count == 1 && r.Parameters[0].ParameterType.FullName == "System.String[]");
var intToString = m.GetMemberReferences().OfType<MethodReference>().First(r => r.DeclaringType.FullName == "System.Int32" && r.Name == "ToString" && r.Parameters.Count == 0);
var int32 = intToString.DeclaringType;
var playerType = tryGet.Parameters[1].ParameterType.GetElementType();
me.Name = "ApplyMagicDamage_Core";

// Verifier(int lanceur, int cible, int avant, int apres)
var check = new MethodDefinition("VerifyMagicResistance", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, m.TypeSystem.Void);
foreach (var n in new[] { "attackerIndex", "defenderIndex", "before", "after" }) check.Parameters.Add(new ParameterDefinition(n, ParameterAttributes.None, m.TypeSystem.Int32));
var vAttacker = new VariableDefinition(playerType); var vTarget = new VariableDefinition(playerType); var vPct = new VariableDefinition(m.TypeSystem.Int32); var vMsg = new VariableDefinition(m.TypeSystem.String);
foreach (var v in new[] { vAttacker, vTarget, vPct, vMsg }) check.Body.Variables.Add(v);
check.Body.InitLocals = true;
var c = check.Body.GetILProcessor();
var end = c.Create(OpCodes.Ret);
c.Append(c.Create(OpCodes.Ldarg_1)); c.Append(c.Create(OpCodes.Ldloca, vTarget)); c.Append(c.Create(OpCodes.Call, tryGet)); c.Append(c.Create(OpCodes.Brfalse, end));
c.Append(c.Create(OpCodes.Ldarg_0)); c.Append(c.Create(OpCodes.Ldloca, vAttacker)); c.Append(c.Create(OpCodes.Call, tryGet)); c.Append(c.Create(OpCodes.Brtrue, end));
c.Append(c.Create(OpCodes.Ldloc, vTarget)); c.Append(c.Create(OpCodes.Ldc_I4_7)); c.Append(c.Create(OpCodes.Call, getPct)); c.Append(c.Create(OpCodes.Stloc, vPct));
string[] parts = { "Verification Resistance magique : ", null, " recoit ", null, " -> ", null, " degats (Resistance magique ", null, "%)" };
c.Append(c.Create(OpCodes.Ldc_I4, parts.Length)); c.Append(c.Create(OpCodes.Newarr, m.TypeSystem.String));
for (int k = 0; k < parts.Length; k++)
{
  c.Append(c.Create(OpCodes.Dup)); c.Append(c.Create(OpCodes.Ldc_I4, k));
  if (parts[k] != null) c.Append(c.Create(OpCodes.Ldstr, parts[k]));
  else if (k == 1) { c.Append(c.Create(OpCodes.Ldloc, vTarget)); c.Append(c.Create(OpCodes.Call, getName)); }
  else if (k == 3) { c.Append(c.Create(OpCodes.Ldarga, check.Parameters[2])); c.Append(c.Create(OpCodes.Call, intToString)); }
  else if (k == 5) { c.Append(c.Create(OpCodes.Ldarga, check.Parameters[3])); c.Append(c.Create(OpCodes.Call, intToString)); }
  else if (k == 7) { c.Append(c.Create(OpCodes.Ldloca, vPct)); c.Append(c.Create(OpCodes.Call, intToString)); }
  c.Append(c.Create(OpCodes.Stelem_Ref));
}
c.Append(c.Create(OpCodes.Call, concat)); c.Append(c.Create(OpCodes.Stloc, vMsg));
c.Append(c.Create(OpCodes.Ldloc, vMsg)); c.Append(c.Create(OpCodes.Ldc_R4, 4f)); c.Append(c.Create(OpCodes.Call, notify));
c.Append(c.Create(OpCodes.Ldsfld, modLog)); c.Append(c.Create(OpCodes.Ldloc, vMsg)); c.Append(c.Create(OpCodes.Callvirt, logInfo));
c.Append(end);
t.Methods.Add(check);

// nouvelle ApplyMagicDamage (meme signature)
var wrap = new MethodDefinition("ApplyMagicDamage", me.Attributes, me.ReturnType);
foreach (var p0 in me.Parameters) wrap.Parameters.Add(new ParameterDefinition(p0.Name, p0.Attributes, p0.ParameterType));
var vBefore = new VariableDefinition(m.TypeSystem.Int32); wrap.Body.Variables.Add(vBefore); wrap.Body.InitLocals = true;
var w = wrap.Body.GetILProcessor();
w.Append(w.Create(OpCodes.Ldarg_2)); w.Append(w.Create(OpCodes.Ldind_I4)); w.Append(w.Create(OpCodes.Stloc, vBefore));
w.Append(w.Create(OpCodes.Ldarg_0)); w.Append(w.Create(OpCodes.Ldarg_1)); w.Append(w.Create(OpCodes.Ldarg_2)); w.Append(w.Create(OpCodes.Call, me));
var skip = w.Create(OpCodes.Ret);
w.Append(w.Create(OpCodes.Ldloc, vBefore)); w.Append(w.Create(OpCodes.Ldc_I4_0)); w.Append(w.Create(OpCodes.Ble, skip));
w.Append(w.Create(OpCodes.Ldarg_0)); w.Append(w.Create(OpCodes.Ldarg_1)); w.Append(w.Create(OpCodes.Ldloc, vBefore)); w.Append(w.Create(OpCodes.Ldarg_2)); w.Append(w.Create(OpCodes.Ldind_I4)); w.Append(w.Create(OpCodes.Call, check));
w.Append(skip);
t.Methods.Add(wrap);
// les appelants (patchs Harmony) appellent ApplyMagicDamage par reference : on les redirige vers la nouvelle
foreach (var ty in m.Types) foreach (var mm in ty.Methods.Where(x => x.HasBody && x != wrap))
  foreach (var ins2 in mm.Body.Instructions.Where(x => (x.OpCode == OpCodes.Call) && x.Operand is MethodReference mr && mr == me))
    ins2.Operand = wrap;

// version
foreach (var ca in plugin.CustomAttributes.Where(c => c.AttributeType.Name == "BepInPlugin")) {
  var a = ca.ConstructorArguments;
  ca.ConstructorArguments[2] = new CustomAttributeArgument(a[2].Type, "0.2.54-check-magic-resistance");
}
var vf = plugin.Fields.SingleOrDefault(f => f.Name == "Version" && f.HasConstant);
if (vf != null) vf.Constant = "0.2.54-check-magic-resistance";
m.Write(args[1]);
Console.WriteLine("ok");
