using AnimalBehaviours;
using DivineFramework;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AutoWool
{
    public static class AlphaAnimalsCompat
    {
        public static List<ThingDef> AlphaAnimalsWithProducts = new();
        public static List<ThingDef> AlphaAnimalsWithCoreProducts = new();
        public static bool IsYak(this ThingDef def) => def.defName == "AA_ChameleonYak";

        public static void ChameleonYaks()
        {
            ThingDef yak = ThingDef.Named("AA_ChameleonYak");
            CompProperties_AnimalProduct compProps = (CompProperties_AnimalProduct)yak.comps.Find(x => x.compClass == typeof(CompAnimalProduct));
            if (AutoWoolSettings.CheckSettings(yak))
            {
                //Only do this if they're supposed to be converted
                foreach (string name in compProps.seasonalItems)
                {
                    //Really just need to run it through the thing
                    ThingDef fleece = ThingDef.Named(name);
                    ThingDef wool = fleece.butcherProducts[0].thingDef;
                    GeneratorUtility.TryAddEntry(wool, fleece);
                    //Adding butcher drops would be nice, but trying to decide which fleece to drop would probably require harmony patches on VEF, so meh
                }
            }
            else
            {
                //If they're not supposed to be converted, we need to reverse the xml patch
                compProps.seasonalItems =
                [
                    "AA_ChameleonYakWoolTemperate",
                    "AA_ChameleonYakWoolWinter",
                    "AA_ChameleonYakWoolJungle",
                    "AA_ChameleonYakWoolDesert",
                ];
            }
            AlphaAnimalsWithProducts.Add(yak);
        }

        public static IEnumerable<ThingDef> ImpliedAlphaDefs()
        {
            foreach (ThingDef animal in AlphaAnimalsWithProducts)
            {
                var newDef = ProcessAlphaDef(animal);
                if (newDef != null)
                {
                    yield return newDef;
                }
            }
        }

        public static ThingDef ProcessAlphaDef(ThingDef animal)
        {
            ThingDef newThing = new();
            CompProperties_AnimalProduct comp = animal.GetCompProperties<CompProperties_AnimalProduct>();

            if (!AutoWoolSettings.CheckSettings(animal))
            {
                return null;
            }
            if (animal.IsYak())
            {
                LogUtil.Message($"Chameleon yaks already handled via xml", true);
                LogUtil.Message("------------", true);
                return null;
            }

            ThingDef oldThing = comp.resourceDef;
            if (oldThing.defName.Contains("Fleece"))
            {
                LogUtil.Message($"{animal.label} already has fleece {oldThing.defName}", true);
                newThing = oldThing;
                oldThing = newThing.butcherProducts[0].thingDef;
                ThingDefCountClass butcherDrop = animal.butcherProducts?.Find(x => x.thingDef == oldThing);
                if (butcherDrop == null)
                {
                    LogUtil.Message($"{animal.defName} has no butcher products", true);
                    GeneratorUtility.DetermineButcherProducts(animal, oldThing, newThing, comp.resourceAmount);
                }
                GeneratorUtility.TryAddEntry(oldThing, newThing);
                LogUtil.Message("------------", true);
                return null;
            }

            if (GeneratorUtility.WoolDefsSeen.ContainsKey(oldThing))
            {
                newThing = GeneratorUtility.WoolDefsSeen[oldThing];
                comp.resourceDef = newThing;
                LogUtil.Message($"{animal.label} uses {oldThing.defName}, replacing with {newThing.defName}", true);
                GeneratorUtility.DetermineButcherProducts(animal, oldThing, newThing, comp.resourceAmount);
                LogUtil.Message("------------", true);
                return null;
            }

            newThing = GeneratorUtility.MakeFleeceFor(oldThing, animal);
            GeneratorUtility.TryAddEntry(oldThing, newThing);
            comp.resourceDef = newThing;
            GeneratorUtility.DetermineButcherProducts(animal, oldThing, newThing, comp.resourceAmount);
            LogUtil.Message($"Adding {newThing.defName} for {animal.label}", true);
            LogUtil.Message("------------", true);
            return newThing;
        }

        public static void MakeListOfShearables()
        {
            AlphaAnimalsWithProducts.Clear();
            AlphaAnimalsWithCoreProducts.Clear();
            foreach (ThingDef animal in DefDatabase<ThingDef>.AllDefs.Where(x => x.category == ThingCategory.Pawn).ToList())
            {
                if (animal.comps.Any(x => x.compClass == typeof(CompAnimalProduct)))
                {
                    CompProperties_AnimalProduct comp = animal.GetCompProperties<CompProperties_AnimalProduct>();
                    if (comp.isRandom || comp.resourceDef == null)
                    {
                        continue;
                    }
                    AlphaAnimalsWithProducts.Add(animal);
                    if (comp.resourceDef.modContentPack.IsOfficialMod)
                    {
                        AlphaAnimalsWithCoreProducts.Add(animal);
                    }
                }
            }
        }

        public static ThingDef GetAlphaResource(this ThingDef animal)
        {
            CompProperties_AnimalProduct comp = animal.GetCompProperties<CompProperties_AnimalProduct>();
            if (comp == null)
            {
                LogUtil.Error($"Tried to get the resource for {animal.defName} but it does not have CompAnimalProduct");
                return null;
            }
            if (!animal.GetSeasonalList().NullOrEmpty())
            {
                return ThingDef.Named(animal.GetSeasonalList().First());
            }
            return comp.resourceDef;
        }

        public static List<string> GetSeasonalList(this ThingDef animal)
        {
            CompProperties_AnimalProduct comp = animal.GetCompProperties<CompProperties_AnimalProduct>();
            if (comp == null)
            {
                LogUtil.Error($"Tried to get the resource for {animal.defName} but it does not have CompAnimalProduct");
            }
            return comp.seasonalItems;
        }
    }
}
