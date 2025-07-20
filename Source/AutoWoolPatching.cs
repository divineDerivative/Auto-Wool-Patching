using DivineFramework;
using HarmonyLib;
using UnityEngine;
using Verse;

#if v1_4 
#error Building for 1.4 has been disabled.
#endif

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
            ModManagement.RegisterMod("AutoWool.ModNameShort", new(FrameworkVersionInfo.Version));
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

    internal class AutoWoolLogger : Logging
    {
        public static readonly AutoWoolLogger LogUtil = new AutoWoolLogger();
        private AutoWoolLogger() : base("<color=#00b7dc>[AutoWool]</color>", () => AutoWoolSettings.debugLogging) { }
    }
}
