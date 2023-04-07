using BepInEx;
using MagneCat.hook;
using System;

namespace MagneCat
{
    [BepInPlugin("magnecat", "MagneCat", "1.0.0")]
    public class MagneCatMod : BaseUnityPlugin
    {
        public MagneCatMod()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                FloatingSpearFeature.OnModInit();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
         
        }
    }
}