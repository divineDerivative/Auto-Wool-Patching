using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AutoWool
{
    public class AutoWoolSettings : ModSettings
    {
        public static bool GardenActive = ModsConfig.IsActive("dismarzero.vgp.vgpgardenfabrics") || ModsConfig.IsActive("dismarzero.vgp.vgpgardenfabricssimplified");
        public static bool AlphaAnimalsActive = ModsConfig.IsActive("sarg.alphaanimals");
        //Add a toggle for this somewhere? Maybe in the dev debug menu, I don't feel like putting it in the regular settings
        public static bool debugLogging = false;

        public static List<ThingDef> AllShearableAnimals = new();
        public static Dictionary<string, bool> DictOfAnimalSettings = new();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref DictOfAnimalSettings, "WoolConversionSettings", LookMode.Value, LookMode.Value);
            base.ExposeData();
        }

        private static float padding = 24f;
        static Vector2 scrollPos = Vector2.zero;
        static FieldInfo curX = AccessTools.Field(typeof(Listing), "curX");

        public static void DoSettingsWindowContents(Rect canvas)
        {
            //put a debug toggle somewhere?
            Listing_Standard outerList = new();
            outerList.Begin(canvas);

            Rect window = new(0f, 0f, canvas.width, canvas.height);
            Listing_Standard list = new(window, () => scrollPos)
            {
                ColumnWidth = (window.width / 2f) - padding
            };

            float scrollHeight = (AllShearableAnimals.Count + AlphaAnimalsCompat.AlphaAnimalsWithProducts.Count + 6) * (Text.CalcHeight("test", window.width) + list.verticalSpacing) / 2f;
            Rect bigRect = new(window)
            {
                width = window.width - GenUI.ScrollBarWidth,
                height = scrollHeight > window.height ? scrollHeight : window.height
            };

            Widgets.BeginScrollView(window, ref scrollPos, bigRect, true);
            list.Begin(bigRect);
            list.Label("AutoWool.SettingsWool".Translate());
            list.GapLine();
            float textHeight = Text.CalcHeight("test", list.ColumnWidth);
            float labelWidth = (list.ColumnWidth - Widgets.CheckboxSize) / 2f;
            foreach (ThingDef animal in AllShearableAnimals)
            {
                ThingDef compThing = ThingFromComp(animal);
                MakeLabel(animal, compThing);
            }

            //Special stuff for Alpha Animals
            if (AlphaAnimalsActive)
            {
                list.Gap();
                list.Label("AutoWool.SettingsAlphaAnimals".Translate(), tooltip: "AutoWool.SettingsAlphaTooltip".Translate());
                list.GapLine();
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
                                MakeLabel(animal, fleece);
                                first = true;
                            }
                            else
                            {
                                ThingDef resource = GeneratorUtility.WoolDefsSeen.ContainsValue(fleece) ? ReverseLookup(fleece) : fleece;
                                bool value = CheckSettings(animal);
                                list.CheckboxLabeled(resource.label.Truncate(labelWidth), ref value, labelWidth);
                                DictOfAnimalSettings[animal.defName] = value;
                            }
                        }
                        continue;
                    }
                    ThingDef compThing = AlphaAnimalsCompat.GetAlphaResource(animal);
                    MakeLabel(animal, compThing);
                }

                list.Gap();
                list.Label("AutoWool.SettingsAlphaVanilla".Translate(), tooltip: "AutoWool.SettingsAlphaVanillaTooltip".Translate());
                list.GapLine();
                foreach (ThingDef animal in AlphaAnimalsCompat.AlphaAnimalsWithCoreProducts)
                {
                    ThingDef compThing = AlphaAnimalsCompat.GetAlphaResource(animal);
                    MakeLabel(animal, compThing);
                }
            }

            list.End();
            outerList.End();
            Widgets.EndScrollView();

            ThingDef ReverseLookup(ThingDef thing)
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

            void MakeLabel(ThingDef animal, ThingDef compThing)
            {
                ThingDef resource = GeneratorUtility.WoolDefsSeen.ContainsValue(compThing) ? ReverseLookup(compThing) : compThing;
                bool value = CheckSettings(animal);
                list.CheckboxLabeled(resource.label.Truncate(labelWidth), ref value, labelWidth);
                Rect rect = new((float)curX.GetValue(list), list.CurHeight - textHeight, list.ColumnWidth * 0.5f, textHeight);
                Widgets.Label(rect, animal.label);
                DictOfAnimalSettings[animal.defName] = value;
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
                Logging.Error($"Tried to get resource for {animal.defName} but it does not have CompShearable");
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