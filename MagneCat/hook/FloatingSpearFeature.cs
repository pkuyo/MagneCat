using MagneCat.MagnetSpear;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MagneCat.hook
{
    public class Features
    {
        public static void OnModInit()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.Stun += Player_Stun;
        }

        private static void Player_Stun(On.Player.orig_Stun orig, Player self, int st)
        {
            orig(self, st);
            if (floatingSpear.TryGetValue(self, out var module))
                module.Stun(st);
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

            SpearState = FloatingSpearState.Suction;
        }

        public Spear? GetSpear()
        {
            Player player;
            if (!playerRef.TryGetTarget(out player) || spearList.Count == 0)
                return null;
            AbstractSpear abstractSpear = new AbstractSpear(player.room.world, null, player.coord, player.room.game.GetNewID(), spearList[0] == 1);
            abstractSpear.electric = spearList[0] == 2;
            abstractSpear.RealizeInRoom();
            spearList.RemoveAt(0);
            return abstractSpear.realizedObject as Spear;

        }

        public void StoreSpear(Spear spear)
        {
            
            spear.room.RemoveObject(spear);
            spear.RemoveFromRoom();
            spearList.Add((spear is ExplosiveSpear)?1 : (spear is ElectricSpear) ? 2 : 0);
            spear.Destroy();
        }


        public void Update()
        {
            Player player;
            if (!playerRef.TryGetTarget(out player))
                return;
            switch (SpearState)
            {
                case FloatingSpearState.Float:
                    if(player.room != null && !player.inShortcut)
                    {
                        floatingCore.Update();
                    }

                    break;
                    
                case FloatingSpearState.Suction:
                    if (stunCoolDownCounter > 0)
                        stunCoolDownCounter--;
                    if (player.room!=null && stunCoolDownCounter == 0)
                    {
                        foreach(var physicals in player.room.physicalObjects)
                        {
                            foreach(var phyObject in physicals)
                            {
                                var spear = (phyObject as Spear);
                                if (spear!=null && !(spear.thrownBy is Player) &&
                                    (spear.mode != Weapon.Mode.StuckInWall && spear.mode != Weapon.Mode.StuckInCreature && spear.mode != Weapon.Mode.Thrown))
                                {
                                    if (spear.mode == Weapon.Mode.Carried || spear.mode == Weapon.Mode.OnBack)
                                    {
                                        if (spear.grabbedBy[0].grabber is Player)
                                            continue;
                                        else
                                            spear.grabbedBy[0].grabber.ReleaseGrasp(spear.grabbedBy[0].grabber.grasps.IndexOf(spear.grabbedBy[0]));
                                    }

                                    var spearPos = player.room.GetTilePosition(spear.firstChunk.pos);
                                    var playerPos = player.room.GetTilePosition(player.mainBodyChunk.pos);
                                    if (!player.room.RayTraceTilesForTerrain(spearPos.x, spearPos.y, playerPos.x, playerPos.y))
                                        continue;

                                 
                                    if (Custom.DistLess(phyObject.firstChunk.pos, player.mainBodyChunk.pos, 30))
                                    {
                                        StoreSpear(spear);
                                        continue;
                                    }

                                    if (spear.mode != Weapon.Mode.Free)
                                    {
                                        spear.ChangeMode(Weapon.Mode.Free);
                                        spear.rotationSpeed = 0;
                                    }
                                    spear.firstChunk.vel += Mathf.InverseLerp(200,0,spear.firstChunk.vel.magnitude) * Custom.DirVec(phyObject.firstChunk.pos, player.mainBodyChunk.pos)*3;
                                    spear.rotationSpeed = ((Custom.VecToDeg(spear.firstChunk.vel) - Custom.VecToDeg(spear.firstChunk.Rotation)) % 180) * 2 * 1 / 40;

                                }
                            }
                        }
                    }

                    if (spearList.Count != 0 && player.FreeHand() != -1 && player.input[0].pckp)
                    {
                        getSpearCounter++;
                        if (getSpearCounter == 20)
                        {
                            getSpearCounter = 0;
                            var spear = (Spear)GetSpear(); 
                            if (spear != null)
                                player.SlugcatGrab(spear, player.FreeHand());
                            
                        }
                    }
                    else if (getSpearCounter > 0)
                    {
                        getSpearCounter = 0;
                    }
                    break;
            }



        }

        public void Stun(int st)
        {
            Player player;
            if (!playerRef.TryGetTarget(out player) || spearList.Count == 0)
                return;
            SSOracleBehavior? behavior = null;
            foreach(var a in player.room.physicalObjects)
                foreach(var b in a)
                    if(b is Oracle && (b as Oracle).oracleBehavior is SSOracleBehavior)
                        behavior = (b as Oracle).oracleBehavior as SSOracleBehavior;
            if (st >= Random.Range(60,80) || (st>=40 && behavior != null && behavior.action == SSOracleBehavior.Action.General_GiveMark))
            {
                Spear spear;
                while((spear = GetSpear())!=null)
                {
                    spear.firstChunk.vel = Custom.RNV() * Random.Range(10, 25);
                    spear.SetRandomSpin();
                    stunCoolDownCounter = 300;
                }
            }
            else if(st >= Random.Range(10, 30))
            {
                int count = Random.Range(2,4);
                Spear spear;
                while ((spear = GetSpear()) != null && (count--)!=0)
                {
                    spear.firstChunk.vel = Custom.RNV() * Random.Range(10, 25);
                    spear.SetRandomSpin();
                    stunCoolDownCounter = st*3;
                }
            }
        }

        public FloatingSpearState SpearState { get; private set; }

        public WeakReference<Player> playerRef;

        public float energy;

        public FloatingCore floatingCore;

        List<int> spearList;
        int getSpearCounter = 0;
        int stunCoolDownCounter = 0;
    }
}
