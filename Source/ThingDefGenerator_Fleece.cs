﻿using DivineFramework;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace AutoWool
{
    public static class ThingDefGenerator_Fleece
    {
        public static IEnumerable<ThingDef> ImpliedFleeceDefs()
        {
            GeneratorUtility.debugThing.options = new();
            foreach (ThingDef animal in AutoWoolSettings.AllShearableAnimals)
            {
                if (!AutoWoolSettings.CheckSettings(animal))
                {
                    //Reverse the xml patch
                    if (animal.defName == "AA_Thunderox")
                    {
                        CompProperties_Shearable oxComp = animal.GetCompProperties<CompProperties_Shearable>();
                        oxComp.woolDef = oxComp.woolDef.butcherProducts[0].thingDef;
                    }
                    continue;
                }

                ThingDef fleeceDef = new();
                CompProperties_Shearable comp = animal.GetCompProperties<CompProperties_Shearable>();
                ThingDef woolDef = comp.woolDef;
                if (woolDef.defName.ToLower().Contains("fleece"))
                {
                    LogUtil.Message($"{animal.label} already has fleece {woolDef.defName}", true);
                    fleeceDef = woolDef;
                    woolDef = fleeceDef.butcherProducts[0].thingDef;
                    ThingDefCountClass butcherDrop = animal.butcherProducts?.Find(x => x.thingDef == woolDef);
                    if (butcherDrop == null)
                    {
                        LogUtil.Message($"{animal.defName} has no butcher products", true);
                        GeneratorUtility.DetermineButcherProducts(animal, woolDef, fleeceDef, comp.woolAmount);
                    }
                    GeneratorUtility.TryAddEntry(woolDef, fleeceDef);
                    LogUtil.Message("------------", true);
                    continue;
                }

                if (GeneratorUtility.WoolDefsSeen.ContainsKey(woolDef))
                {
                    fleeceDef = GeneratorUtility.WoolDefsSeen[woolDef];
                    comp.woolDef = fleeceDef;
                    LogUtil.Message($"{animal.label} uses {woolDef.defName}, replacing with {fleeceDef.defName}", true);
                    GeneratorUtility.DetermineButcherProducts(animal, woolDef, fleeceDef, comp.woolAmount);
                    LogUtil.Message("------------", true);
                    continue;
                }

                fleeceDef = GeneratorUtility.MakeFleeceFor(woolDef, animal);
                GeneratorUtility.TryAddEntry(woolDef, fleeceDef);
                comp.woolDef = fleeceDef;
                GeneratorUtility.DetermineButcherProducts(animal, woolDef, fleeceDef, comp.woolAmount);
                LogUtil.Message($"Adding {fleeceDef.defName} for {animal.label}", true);
                LogUtil.Message("------------", true);
                yield return fleeceDef;
            }
        }
    }
}
