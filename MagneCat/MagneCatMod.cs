﻿using BepInEx;
using MoreSlugcats;
using MagneCat.hook;
using MagneCat.MagnetSpear;
using System.Security.Permissions;
using UnityEngine;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using Menu.Remix;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace MagneCat
{
    [BepInPlugin("magnecat", "MagneCat", "1.0.0")]
    public class MagneCatMod : BaseUnityPlugin
    {
        bool inited = false;
        public Shader magneticFieldShader;
        
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
                EnergyGraphicsHook.OnModInit();
                var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles/hudasset"));
                self.Shaders.Add("CircleHUD", FShader.CreateShader("CircleHUD", bundle.LoadAsset<Shader>("CircleHUD")));

                //HUDPatch.PatchOn();
                //JollyExtend.PatchOn();
                //JollySetupDialogExtend.PatchOn();
                LoadResources(self);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            inited = true;
        }

        public void LoadResources(RainWorld self)
        {
            string bundlePath = AssetManager.ResolveFilePath("MagnecatBundles/magnecatbundle");
            AssetBundle ab = AssetBundle.LoadFromFile(bundlePath);

            magneticFieldShader = ab.LoadAsset<Shader>("assets/myshader/magneticfieldlines.shader");
            self.Shaders.Add("MagneticFieldLines", FShader.CreateShader("MagneticFieldLines", magneticFieldShader));
        }

        public sealed class Test : AbstractPhysicalObject
        {
            public Test(World world,AbstractObjectType type,PhysicalObject real, WorldCoordinate pos, EntityID ID) : base(world, type, real, pos, ID)
            {

            }

        }
    }
}