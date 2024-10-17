using DivineFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AutoWool
{
    public class AutoWoolSettings : ModSettings
    {
        public static bool GardenActive = ModsConfig.IsActive("dismarzero.vgp.vgpgardenfabrics") || ModsConfig.IsActive("dismarzero.vgp.vgpgardenfabricssimplified");
        public static bool AlphaAnimalsActive = ModsConfig.IsActive("sarg.alphaanimals");
        public static bool debugLogging = false;

        public static List<ThingDef> AllShearableAnimals = new();
        public static Dictionary<string, bool> DictOfAnimalSettings = new();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref DictOfAnimalSettings, "WoolConversionSettings", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref debugLogging, "woolDebugLogging", false, true);
        }

        private static float padding = 24f;
        static Vector2 scrollPos = Vector2.zero;

        static SettingsHandler<AutoWoolSettings> settingsHandler = new();
        static SettingsHandler<AutoWoolSettings> alphaHandler = new(true);
        public static void DoSettingsWindowContents(Rect canvas)
        {
            Listing_ScrollView outerList = new()
            {
                ColumnWidth = (canvas.width / 2f) - padding
            };

            float scrollHeight;
            if (AlphaAnimalsActive)
            {
                scrollHeight = Math.Max(settingsHandler.height, alphaHandler.height);
            }
            else
            {
                scrollHeight = settingsHandler.height / 2f;
            }
            Listing_Standard list = outerList.BeginScrollView(canvas, scrollHeight, ref scrollPos);

            if (!settingsHandler.Initialized)
            {
                if (AlphaAnimalsActive)
                {
                    settingsHandler.maxOneColumn = true;
                }
                settingsHandler.width = list.ColumnWidth;
                settingsHandler.RegisterNewRow("Title").AddLabel("AutoWool.SettingsWool".Translate);
                settingsHandler.RegisterNewRow().AddLine();
                foreach (ThingDef animal in AllShearableAnimals)
                {
                    ThingDef compThing = ThingFromComp(animal);
                    MakeRow(animal, compThing);
                }

                //Special stuff for Alpha Animals
                if (AlphaAnimalsActive)
                {
                    alphaHandler.width = list.ColumnWidth;
                    alphaHandler.RegisterNewRow(newColumn: true).AddSpace(height: 12f);
                    alphaHandler.RegisterNewRow("AlphaAnimalsProducts")
                        .AddLabel("AutoWool.SettingsAlphaUnique".Translate)
                        .WithTooltip("AutoWool.SettingsAlphaUniqueTooltip".Translate);
                    alphaHandler.RegisterNewRow().AddLine(); ;

                    foreach (ThingDef animal in AlphaAnimalsCompat.AlphaAnimalsWithProducts.Except(AlphaAnimalsCompat.AlphaAnimalsWithCoreProducts))
                    {
                        if (animal.IsYak())
                        {
                            List<string> seasonal = AlphaAnimalsCompat.GetSeasonalList(animal);
                            bool first = false;
                            foreach (string item in seasonal)
                            {
                                ThingDef fleece = ThingDef.Named(item);
                                if (!first)
                                {
                                    MakeRow(animal, fleece, true);
                                    first = true;
                                }
                                else
                                {
                                    //Same as MakeRow but without the animal name in the first label
                                    UIContainer row = alphaHandler.RegisterNewRow($"{animal.label}Row{item}");
                                    row.AddSpace();
                                    ThingDef resource = GeneratorUtility.WoolDefsSeen.ContainsValue(fleece) ? ReverseLookup(fleece) : fleece;
                                    row.AddLabel(() => resource.label);
                                    row.AddElement(NewElement.Checkbox(absolute: 24f)
                                        .WithReference(AutoWoolPatching.settings, nameof(DictOfAnimalSettings), DictOfAnimalSettings[animal.defName], animal.defName));
                                }
                            }
                            continue;
                        }
                        ThingDef compThing = AlphaAnimalsCompat.GetAlphaResource(animal);
                        MakeRow(animal, compThing, true);
                    }

                    alphaHandler.RegisterNewRow().AddSpace(height: 12f);
                    alphaHandler.RegisterNewRow("AlphaAnimalsCoreProducts").AddLabel("AutoWool.SettingsAlphaVanilla".Translate).WithTooltip("AutoWool.SettingsAlphaVanillaTooltip".Translate);
                    alphaHandler.RegisterNewRow().AddLine();
                    foreach (ThingDef animal in AlphaAnimalsCompat.AlphaAnimalsWithCoreProducts)
                    {
                        ThingDef compThing = AlphaAnimalsCompat.GetAlphaResource(animal);
                        MakeRow(animal, compThing, true);
                    }
                    alphaHandler.Initialize();
                }

                settingsHandler.RegisterNewRow().AddLine().HideWhen(() => !Prefs.DevMode);
                settingsHandler.RegisterNewRow("DebugLogging").AddElement(NewElement.Checkbox().WithLabel(() => "Debug logging").WithReference(AutoWoolPatching.settings, nameof(debugLogging), debugLogging).HideWhen(() => !Prefs.DevMode).WithTooltip(() => "Restart required after activating, since all the important stuff happens during startup."));

                settingsHandler.Initialize();
            }

            settingsHandler.Draw(list);
            if (AlphaAnimalsActive)
            {
                alphaHandler.Draw(list);
            }

            outerList.End();

            static ThingDef ReverseLookup(ThingDef thing)
            {
                foreach (KeyValuePair<ThingDef, ThingDef> entry in GeneratorUtility.WoolDefsSeen)
                {
                    if (entry.Value == thing)
                    {
                        return entry.Key;
                    }
                }
                return null;
            }

            static void MakeRow(ThingDef animal, ThingDef compThing, bool alpha = false)
            {
                UIContainer row = (alpha ? alphaHandler : settingsHandler).RegisterNewRow($"{animal.label}Row");
                row.AddLabel(() => animal.label);
                ThingDef resource = GeneratorUtility.WoolDefsSeen.ContainsValue(compThing) ? ReverseLookup(compThing) : compThing;
                row.AddLabel(() => resource.label);
                row.AddElement(NewElement.Checkbox(absolute: 24f)
                    .WithReference(AutoWoolPatching.settings, nameof(DictOfAnimalSettings), DictOfAnimalSettings[animal.defName], animal.defName));
            }
        }

        private static ThingDef ThingFromComp(ThingDef animal)
        {
            CompProperties_Shearable comp = animal.GetCompProperties<CompProperties_Shearable>();
            if (comp == null)
            {
                if (AlphaAnimalsActive)
                {
                    return AlphaAnimalsCompat.GetAlphaResource(animal);
                }
                LogUtil.Error($"Tried to get resource for {animal.defName} but it does not have CompShearable");
                return null;
            }
            return comp.woolDef;
        }

        public static void ApplySettings()
        {
            bool restartNeededAdd = false;
            bool restartNeededRemove = false;
            foreach (ThingDef animal in AllShearableAnimals.Concat(AlphaAnimalsCompat.AlphaAnimalsWithProducts))
            {
                ThingDef compThing = ThingFromComp(animal);
                if (DictOfAnimalSettings.TryGetValue(animal.defName, out bool value))
                {
                    if (value)
                    {
                        //If their compThing is not a value in WoolDefsSeen, need a restart
                        if (!GeneratorUtility.WoolDefsSeen.Any(x => x.Value == compThing))
                        {
                            restartNeededAdd = true;
                        }
                    }
                    else
                    {
                        //If their wooldef is a value in WoolDefsSeen (aka it's a fleece), need a restart
                        if (GeneratorUtility.WoolDefsSeen.Any(x => x.Value == compThing))
                        {
                            restartNeededRemove = true;
                        }
                    }
                }
            }

            if (restartNeededAdd || restartNeededRemove)
            {
                //Warning that game must be restarted for changes to take effect
                string warning = "AutoWool.RestartNeeded".Translate();
                if (restartNeededRemove)
                {
                    //Warning that all instances of the fleece in list need to be removed before restart
                    warning += "AutoWool.RemovedFleeceWarning".Translate();
                }

                Dialog_MessageBox window = new(warning, "Confirm".Translate());
                Find.WindowStack.Add(window);
            }
        }

        public static bool CheckSettings(ThingDef animal)
        {
            return DictOfAnimalSettings.TryGetValue(animal.defName);
        }
    }
}