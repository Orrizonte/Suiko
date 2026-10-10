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
// version
foreach (var ca in plugin.CustomAttributes.Where(c => c.AttributeType.Name == "BepInPlugin")) {
  var a = ca.ConstructorArguments;
  ca.ConstructorArguments[2] = new CustomAttributeArgument(a[2].Type, "0.2.53-allies-magic-fix");
}
var vf = plugin.Fields.SingleOrDefault(f => f.Name == "Version" && f.HasConstant);
if (vf != null) vf.Constant = "0.2.53-allies-magic-fix";
m.Write(args[1]);
Console.WriteLine("ok");
