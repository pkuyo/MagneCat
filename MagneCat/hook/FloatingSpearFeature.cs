using MagneCat.MagnetSpear;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MagneCat.hook
{
    public class Features
    {
        public static void OnModInit()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Spear.HitSomething += Spear_HitSomething;
        }

        private static bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            FloatingSpearModule module;
            if (self.thrownBy == result.obj && result.obj is Player && floatingSpear.TryGetValue((result.obj as Player),out module))
            {
                module.StoreSpear(self);
                return true;
            }
            return orig(self,result, eu);
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            Debug.Log("Player_Ctor");
            orig(self,abstractCreature,world);
            if (!floatingSpear.TryGetValue(self, out _))
                Debug.Log("Add FloatingSpearModule");
                floatingSpear.Add(self, new FloatingSpearModule(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (floatingSpear.TryGetValue(self, out var module))
                module.Update();
        }

        static public ConditionalWeakTable<Player, FloatingSpearModule> floatingSpear = new ConditionalWeakTable<Player, FloatingSpearModule>();
    }

    public class FloatingSpearModule
    {
        public enum FloatingSpearState
        {
            Float,
            Suction
        }

        public FloatingSpearModule(Player player)
        {
            playerRef = new WeakReference<Player>(player);
            spearList = new List<int>();
            floatingCore = new FloatingCore(this);

            SpearState = FloatingSpearState.Float;
        }

        public Spear GetSpear()
        {
            Player player;
            if (!playerRef.TryGetTarget(out player) || spearList.Count>0)
                return null;

            AbstractSpear abstractSpear = new AbstractSpear(player.room.world, null, player.coord, player.room.game.GetNewID(), spearList[0] == 1);
            abstractSpear.electric = spearList[0] == 2;
            abstractSpear.RealizeInRoom();
            return abstractSpear.realizedObject as Spear;

        }

        public void StoreSpear(Spear spear)
        {
            spear.RemoveFromRoom();
            spearList.Add((spear is ExplosiveSpear)?1 : (spear is ElectricSpear) ? 2 : 0);
            spear.Destroy();
        }


        public void Update()
        {
            Debug.Log("update");
            Player player;
            if (!playerRef.TryGetTarget(out player))
                return;

            switch (SpearState)
            {
                case FloatingSpearState.Float:
                    //TODO : 漂浮模式！
                    if(player.room != null && !player.inShortcut)
                    {
                        floatingCore.Update();
                    }

                    break;
                    
                case FloatingSpearState.Suction:
                    if (player.room!=null)
                    {
                        foreach(var physicals in player.room.physicalObjects)
                        {
                            foreach(var phyObject in physicals)
                            {
                                var spear = (phyObject as Spear);
                                if (spear!=null && Custom.DistLess(phyObject.firstChunk.pos,player.mainBodyChunk.pos,1000) && 
                                    (spear.mode != Weapon.Mode.StuckInWall || spear.mode != Weapon.Mode.StuckInCreature || spear.mode != Weapon.Mode.OnBack))
                                {
                                    var spearPos = player.room.GetTilePosition(spear.firstChunk.pos);
                                    var playerPos = player.room.GetTilePosition(player.mainBodyChunk.pos);
                                    if (!player.room.RayTraceTilesForTerrain(spearPos.x, spearPos.y, playerPos.x, playerPos.y))
                                        break;

                                    if (spear.mode != Weapon.Mode.Free)
                                    {
                                        spear.ChangeMode(Weapon.Mode.Free);
                                        spear.thrownBy = player;
                                        spear.rotationSpeed = 0;
                                    }

                                    spear.firstChunk.vel += Mathf.InverseLerp(30,5,spear.firstChunk.vel.magnitude) * Custom.DirVec(phyObject.firstChunk.pos, player.mainBodyChunk.pos) * 10 * 1/40;
                                    spear.rotationSpeed = (Custom.VecToDeg(spear.firstChunk.vel) - Custom.VecToDeg(spear.firstChunk.Rotation)) * 2 * 1 / 40;

                                }
                            }
                        }
                    }
                    break;
            }

            if (spearList.Count!=0 && player.FreeHand() != -1 && player.input[0].pckp)
            {
                getSpearCounter++;
                if(getSpearCounter == 20)
                {
                    getSpearCounter = 0;
                    var spear = (Spear)GetSpear();
                    if (spear != null)
                    {
                        player.SlugcatGrab(spear,player.FreeHand());
                    }
                }
            }
            else if (getSpearCounter > 0)
            {
                getSpearCounter = 0;
            }


        }

 

        public FloatingSpearState SpearState { get; private set; }

        public WeakReference<Player> playerRef;
        public float energy;
        List<int> spearList;
        int getSpearCounter = 0;

        public FloatingCore floatingCore;
    }
}
