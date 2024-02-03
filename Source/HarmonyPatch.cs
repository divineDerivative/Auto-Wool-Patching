using RimWorld;
using Verse;
using HarmonyLib;

namespace AutoWool
{
    [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static class DefGenerator_GenerateImpliedDefs_PreResolve
    {
        public static void Postfix()
        {
            //Do this first because I need this list to contain all possible animals regardless of whether they get converted
            GeneratorUtility.MakeListOfShearables();
            foreach (ThingDef def in ThingDefGenerator_Fleece.ImpliedFleeceDefs())
            {
                DefGenerator.AddImpliedDef(def);
            }
            if (ModsConfig.IsActive("sarg.alphaanimals"))
            {
                //Yaks are separate because they're weird
                AlphaAnimalsCompat.ChameleonYaks();
                foreach (ThingDef def in AlphaAnimalsCompat.ImpliedAlphaDefs())
                {
                    DefGenerator.AddImpliedDef(def);
                }
            }
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.Silent);
        }
    }
}
