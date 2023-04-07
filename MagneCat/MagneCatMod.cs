using BepInEx;
using MoreSlugcats;
using MagneCat.hook;
using MagneCat.MagnetSpear;
using System.Security.Permissions;
using UnityEngine;
using System.Collections;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace MagneCat
{
    [BepInPlugin("magnecat","Magnecat","1.0.0")]
    public class MagneCatMod : BaseUnityPlugin
    {
        bool inited = false;
        
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (inited) return;

            try
            {
                Features.OnModInit();
                SpearPatch.OnModInit();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            inited = true;
        }
    }
}