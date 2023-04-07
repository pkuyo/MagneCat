using MagneCat.hook;
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

        public List<Spear> MagnetismSpears = new List<Spear>();
        public List<Spear> SpearsOnRing = new List<Spear>();

        public bool ShouldMagnetismSpears { get; private set; }

        public FloatingCore(FloatingSpearModule module)
        {
            moduleRef = new WeakReference<FloatingSpearModule>(module);
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
        }
    }
}
