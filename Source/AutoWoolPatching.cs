using Verse;
using HarmonyLib;
using UnityEngine;

namespace AutoWool
{
    public class AutoWoolPatching : Mod
    {
        public static AutoWoolSettings settings;

        public AutoWoolPatching(ModContentPack content) : base(content)
        {
            settings = GetSettings<AutoWoolSettings>();
            Harmony harmony = new Harmony(id: "divineDerivative.AutoWool");
            harmony.PatchAll();
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
