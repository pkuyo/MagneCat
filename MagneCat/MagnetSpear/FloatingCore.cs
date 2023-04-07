using MagneCat.hook;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MagneCat.MagnetSpear
{
    public class FloatingCore
    {
        public WeakReference<FloatingSpearModule> moduleRef;
        public FloatingRing ring;

        public List<Spear> MagnetismSpears = new List<Spear>();

        public bool ShouldMagnetismSpears { get; private set; }

        public FloatingCore(FloatingSpearModule module)
        {
            moduleRef = new WeakReference<FloatingSpearModule>(module);
            ring = new FloatingRing(this);
        }

        public void Update()
        {
            if (!moduleRef.TryGetTarget(out var module)) return;
            if (!module.playerRef.TryGetTarget(out var player)) return;

            ShouldMagnetismSpears = Input.GetKey(KeyCode.C);

            var spears = from updated in player.room.updateList where updated is Spear select updated as Spear;

            Spear closetSpear = null;
            float closet = float.MaxValue;

            foreach (Spear spear in spears)
            {
                if(!SpearPatch.magnetsmSpearAIs.TryGetValue(spear,out var ai))
                {
                    SpearPatch.magnetsmSpearAIs.Add(spear, new MagnetismSpearAI(this, spear, player));
                }
                else
                {
                    float distance = Vector2.Distance(spear.firstChunk.pos, player.DangerPos);
                    if(distance < closet)
                    {
                        closet = distance;
                        closetSpear = spear;
                    }
                }
            }

            if(closetSpear != null)
            {
                var playerGraphics = (player.graphicsModule as PlayerGraphics);
                var hands = playerGraphics == null ? null : playerGraphics.hands;
                SlugcatHand freeHand = null;
                if(hands != null)
                {
                    for (int i = 0; i < hands.Length; i++)
                    {
                        if (player.grasps[i] == null) freeHand = hands[i];
                    }

                    if (freeHand != null)
                    {
                        freeHand.mode = Limb.Mode.Dangle;
                        freeHand.vel = closetSpear.firstChunk.pos - player.DangerPos;
                    }
                }
            }

            ring.Update();
        }

        public class FloatingRing
        {
            FloatingCore floatingCore;

            //ring state
            public float ringRad = 40f;
            public float rotateAngle = 0f;

            public Vector2 ringPos = Vector2.zero;

            public int attackCountDown = 0;

            public List<WeakReference<MagnetismSpearAI>> onRingSpearAI = new List<WeakReference<MagnetismSpearAI>>();
            public FloatingRing(FloatingCore core)
            {
                floatingCore = core;
            }

            public void Update()
            {
                if (!floatingCore.moduleRef.TryGetTarget(out var module)) return;
                if(!module.playerRef.TryGetTarget(out var player)) return;


                for (int i = onRingSpearAI.Count - 1; i >= 0; i--)
                {
                    if (!onRingSpearAI[i].TryGetTarget(out var target))
                    {
                        onRingSpearAI.RemoveAt(i);
                        continue;
                    }
                    if (target.mode == MagnetismSpearAI.Mode.Magnetism) onRingSpearAI.RemoveAt(i);
                }

                if (!player.inShortcut)
                {
                    ringPos = Vector2.Lerp(ringPos, player.DangerPos + Vector2.up * 40f, 0.1f);;
                }
                else if(player.enteringShortCut != null)
                {
                    for (int i = onRingSpearAI.Count - 1; i >= 0; i--)
                    {
                        if (onRingSpearAI[i].TryGetTarget(out var target))
                        {
                            target.SuckIntoShortcut(player.enteringShortCut.Value, player.room);
                        }
                    }
                }
                rotateAngle += Time.deltaTime * 120f;

                if (attackCountDown > 0) attackCountDown--;
                else
                {
                    if (player.input[0].thrw)
                    {
                        var creatures = from updated in player.room.updateList where updated is Creature select updated as Creature;

                        Creature closet = null;
                        float minDistance = float.MaxValue;
                        foreach(var creature in creatures)
                        {
                            if (creature.inShortcut || creature is Player) continue;

                            float distance = Vector2.Distance(player.DangerPos, creature.DangerPos);
                            if(distance < minDistance)
                            {
                                minDistance = distance;
                                closet = creature;
                            }
                        }

                        if(closet != null)
                        {
                            for (int i = onRingSpearAI.Count - 1; i >= 0; i--)
                            {
                                if (onRingSpearAI[i].TryGetTarget(out var target))
                                {
                                    target.Attack(player, closet.DangerPos);
                                    onRingSpearAI.RemoveAt(i);

                                    attackCountDown = 10;
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            public Vector2 GetPosOnRing(MagnetismSpearAI ai,bool addIfNotOnRing = false)
            {
                //获取index
                int onRingIndex = -1;
                for(int i = 0;i < onRingSpearAI.Count;i++)
                {
                    if (onRingSpearAI[i].TryGetTarget(out var target) && target == ai)
                    {
                        onRingIndex = i;
                        break;
                    }
                }
                if(onRingIndex == -1)
                {
                    if (addIfNotOnRing && ai.mode == MagnetismSpearAI.Mode.OnRing)
                    {
                        onRingSpearAI.Add(new WeakReference<MagnetismSpearAI>(ai));
                        onRingIndex = onRingSpearAI.Count - 1;
                    }
                    else return Vector2.zero;
                }


                float angle = Custom.LerpMap((float)onRingIndex, 0f, onRingSpearAI.Count, 0f, 360f);
                angle += rotateAngle;

                Vector2 r = Custom.DegToVec(angle) * ringRad;
                Vector2 pos = r + ringPos;

                return pos;
            }

            public void AddToRing(MagnetismSpearAI ai)
            {
                for (int i = 0; i < onRingSpearAI.Count; i++)
                {
                    if (onRingSpearAI[i].TryGetTarget(out var target) && target == ai)
                    {
                        return;
                    }
                }
                onRingSpearAI.Add(new WeakReference<MagnetismSpearAI>(ai));
            }
        }
    }
}
