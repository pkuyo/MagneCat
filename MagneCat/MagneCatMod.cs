using BepInEx;
using MagneCat.hook;

namespace MagneCat
{
    public class MagneCatMod : BaseUnityPlugin
    {
        public MagneCatMod()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            FloatingSpearFeature.OnModInit();
        }
    }
}