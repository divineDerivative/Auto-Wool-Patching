using DivineFramework;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AutoWool
{
    public class AutoWoolPatching : Mod
    {
        public static AutoWoolSettings settings;

        public AutoWoolPatching(ModContentPack content) : base(content)
        {
            settings = GetSettings<AutoWoolSettings>();
            Harmony harmony = new(id: "divineDerivative.AutoWool");
            harmony.PatchAll();
            ModManagement.RegisterMod("AutoWool.ModNameShort", typeof(AutoWoolPatching).Assembly.GetName().Name, new("0.3.0.0"), "<color=#00b7dc>[AutoWool]</color>", () => AutoWoolSettings.debugLogging);
        }

        public override string SettingsCategory()
        {
            return "AutoWool.ModNameShort".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            AutoWoolSettings.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            AutoWoolSettings.ApplySettings();
        }
    }
}
