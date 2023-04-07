using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace MagneCat.MagnetSpear
{
    public class MagnetismSpearAI
    {
        public FloatingCore floatingCore;
        public WeakReference<Spear> spearRef;
        public WeakReference<Player> playerRef;

        public QuickPath? path;
        public WorldCoordinate dest;

        public int nextTileTimeStacker = 0;
        public int currentPathIndex = 0;
        public MagnetismSpearAI(FloatingCore floatingCoredule, Spear spear, Player player)
        {
            floatingCore = floatingCoredule;
            spearRef = new WeakReference<Spear>(spear);
            playerRef = new WeakReference<Player>(player);
        }
        public void Update(Spear self)
        {
            if (!floatingCore.ShouldMagnetismSpears)
            {
                self.gravity = 0.9f;
                return;
            }
            if (!playerRef.TryGetTarget(out var player)) return;

            var newDest = player.coord;
            var room = self.room;
            if(newDest != dest|| path == null)
            {
                dest = player.coord;
                path = UpdatePath(room, room.GetTilePosition(self.firstChunk.pos), dest.Tile);
                currentPathIndex = 0;
            }

            Vector2 nextPathPos = room.MiddleOfTile(path.tiles[currentPathIndex]);

            self.firstChunk.vel = Vector2.Lerp(self.firstChunk.vel, (nextPathPos - self.firstChunk.pos), 0.05f);

            if(Custom.DistLess(nextPathPos, self.firstChunk.pos,20f) && currentPathIndex < path.Length - 1)
            {
                var shortcut = room.shortcutData(nextPathPos);
                ShortcutData? nextPathShortcutData = null;
                if(currentPathIndex + 1 < path.Length)
                {
                    nextPathShortcutData = room.shortcutData(path.tiles[currentPathIndex + 1]);
                }
                if (shortcut.shortCutType == ShortcutData.Type.Normal && nextPathShortcutData != null && nextPathShortcutData.Value.shortCutType == shortcut.shortCutType)
                {
                    var vessel = new SpearShortcutVessel(self, SpearShortcutVessel.GetVirtualAbCreature(), self.room);
                    self.room.AddObject(vessel);
                    vessel.SuckedIntoShortCut(shortcut.StartTile, false);
                }
                currentPathIndex++;
            }
        }

        public QuickPath UpdatePath(Room room,IntVector2 start,IntVector2 dest)
        {
            var pather = new QuickPathFinder(start, dest, room.aimap, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly));

            float startTime = Time.time;
            while(pather.status == 0)
            {
                pather.Update();
            }
            QuickPath quickPath = pather.ReturnPath();
            Debug.Log("Spear find path : " + (Time.time - startTime).ToString());
            return quickPath;
        }
    }
}
