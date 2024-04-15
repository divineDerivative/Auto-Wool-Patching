using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AutoWool
{
    public static class GeneratorUtility
    {
        public static Dictionary<ThingDef, ThingDef> WoolDefsSeen = new();
        public static ThingSetMaker_Sum debugThing = DefDatabase<ThingSetMakerDef>.GetNamed("OneOfEachFleece").root as ThingSetMaker_Sum;
        private static Dictionary<string, string> NamesList = new()
        {
            {"BWP_woollymammothFleece", "BWP_mammothFleece" },
            {"BWP_thunderoxFleece","BWP_NightFleece" },
            {"BWP_angorarabbitFleece","BWP_ACPAngoraFleece" },
            {"BWP_llamaFleece","BWP_ACPLlamaFleece" },
            {"BWP_blackdirewolfFleece","BWP_ACPBDireWolfFleece" },
            {"BWP_merinosheepFleece","BWP_finesheepFleece" },
            {"BWP_woollyrhinocerosFleece","BWP_woollyrhinoFleece" },
            {"BWP_woolycowFleece","BWP_woolcowFleece" },
            {"BWP_mamuffaloFleece","BWP_MamuffaloFleece" },
            {"BWP_argaliFleece","BWP_ArgaliFleece" },
            {"BWP_GuldensheepFleece","BWP_GuldenSheepFleece" },
        };
        private static ModContentPack myContentPack = LoadedModManager.GetMod<AutoWoolPatching>().Content;

        public static void MakeListOfShearables()
        {
            foreach (ThingDef animal in DefDatabase<ThingDef>.AllDefs.Where(x => x.category == ThingCategory.Pawn).ToList())
            {
                if (animal.comps.Any(x => x.compClass == typeof(CompShearable)))
                {
                    AutoWoolSettings.AllShearableAnimals.Add(animal);
                }
            }
            if (AutoWoolSettings.AlphaAnimalsActive)
            {
                AlphaAnimalsCompat.MakeListOfShearables();
            }
            foreach (ThingDef animal in AutoWoolSettings.AllShearableAnimals)
            {
                AutoWoolSettings.DictOfAnimalSettings.AddDistinct(animal.defName, true);
            }
            ThingDef yaks = DefDatabase<ThingDef>.GetNamedSilentFail("AA_ChameleonYak");
            if (yaks != null)
            {
                AutoWoolSettings.DictOfAnimalSettings.AddDistinct(yaks.defName, true);
            }
            foreach (ThingDef animal in AlphaAnimalsCompat.AlphaAnimalsWithProducts)
            {
                AutoWoolSettings.DictOfAnimalSettings.AddDistinct(animal.defName, false);
            }
        }

        public static void TryAddEntry(ThingDef wool, ThingDef fleece)
        {
            if (!WoolDefsSeen.ContainsKey(wool))
            {
                //Add hyperlinks
                fleece.descriptionHyperlinks ??= new();
                fleece.descriptionHyperlinks.Add(new() { def = wool });
                //Add content source
                fleece.modContentPack = myContentPack;
                //Add to thing set maker
                ThingFilter filter = new();
                List<ThingDef> list = new()
                {
                    { fleece }
                };
                AccessTools.Field(typeof(ThingFilter), "thingDefs").SetValue(filter, list);
                debugThing.options.Add(new ThingSetMaker_Sum.Option
                {
                    thingSetMaker = new ThingSetMaker_Count
                    {
                        fixedParams = new ThingSetMakerParams
                        {
                            filter = filter
                        }
                    }
                });
                //Add to dictionary
                WoolDefsSeen.Add(wool, fleece);
            }
        }

        private static ThingDef BasicFleeceDef()
        {
            ThingDef def = new()
            {
                thingClass = typeof(ThingWithComps),
                category = ThingCategory.Item,
                drawerType = DrawerType.MapMeshOnly,
                resourceReadoutPriority = ResourceCountPriority.Middle,
                useHitPoints = true,
                selectable = true,
                stackLimit = 100,
                alwaysHaulable = true,
                drawGUIOverlay = true,
                rotatable = false,
                pathCost = 14,
                allowedArchonexusCount = -1,
                tickerType = TickerType.Rare,
                healthAffectsPrice = false,
                soundInteract = SoundDefOf.Standard_Drop,
                statBases = new()
            };
            def.SetStatBaseValue(StatDefOf.Beauty, -4f);
            def.SetStatBaseValue(StatDefOf.MaxHitPoints, 60f);
            def.SetStatBaseValue(StatDefOf.Flammability, 2f);
            def.SetStatBaseValue(StatDefOf.DeteriorationRate, 8f);
            def.SetStatBaseValue(StatDefOf.Mass, 0.03f);
            def.SetStatBaseValue(StatDefOf.MarketValue, 1f);

            def.graphicData = new GraphicData
            {
                graphicClass = typeof(Graphic_Single),
                texPath = "Things/Item/Resource/fleece"
            };

            def.comps.Add(new CompProperties_Forbiddable());
            def.thingCategories =
            [
                WoolDefOf.BWP_Fleece,
            ];

            return def;
        }

        public static void DetermineButcherProducts(ThingDef animal, ThingDef woolDef, ThingDef fleeceDef, int number)
        {
            animal.butcherProducts ??= new();

            int half = (int)Math.Round(number / 2f);
            int mod = half % 5;
            mod = (mod == 0) ? 5 : mod;
            int count = half + (5 - mod);

            Logging.Message($"Choose {count} {fleeceDef.defName} for {number} {woolDef.defName}", true);
            animal.butcherProducts.Add(new ThingDefCountClass { thingDef = fleeceDef, count = count });
        }

        public static ThingDef MakeFleeceFor(ThingDef woolDef, ThingDef raceDef)
        {
            ThingDef fleeceDef = BasicFleeceDef();
            SetNameAndDesc(woolDef, fleeceDef, raceDef);
            if (woolDef.stuffProps != null)
            {
                fleeceDef.graphicData.color = woolDef.stuffProps.color;
            }
            else
            {
                fleeceDef.graphicData.color = woolDef.graphicData.color;
            }
            fleeceDef.butcherProducts =
            [
                new() {
                    thingDef = woolDef,
                    count = 25
                }
            ];
            return fleeceDef;
        }

        private static void SetNameAndDesc(ThingDef woolDef, ThingDef fleeceDef, ThingDef raceDef)
        {
            bool silk = false;
            if (woolDef.defName.ToLower().Contains("silk"))
            {
                fleeceDef.defName = $"BWP_{raceDef.label}Thread".Replace(" ", "").Replace("-", "");
                fleeceDef.label = $"{raceDef.label} thread";
                silk = true;
            }
            else
            {
                fleeceDef.defName = $"BWP_{raceDef.label}Fleece".Replace(" ", "").Replace("-", "");
                fleeceDef.label = $"{raceDef.label} fleece";
            }
            fleeceDef.defName = CheckForOldNames(fleeceDef.defName);
            if (silk)
            {
                fleeceDef.description = (AutoWoolSettings.GardenActive ? "AutoWool.VGPThreadDesc" : "AutoWool.ThreadDesc").Translate(raceDef.label);
            }
            else
            {
                fleeceDef.description = (AutoWoolSettings.GardenActive ? "AutoWool.VGPFleeceDesc" : "AutoWool.FleeceDesc").Translate(raceDef.label);
            }
        }

        private static string CheckForOldNames(string defName)
        {
            if (NamesList.ContainsKey(defName))
            {
                return NamesList[defName];
                //Should I remove it from the list you think? In case there's another animal with the same label that shouldn't get replaced?
            }
            return defName;
        }
    }
}
