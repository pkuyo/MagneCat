﻿using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
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

        public IntVector2 lastTile;

        public Mode mode;

        public int nextTileTimeStacker = 0;
        public int currentPathIndex = 0;
        public int stuckInPosCounter = 0;//卡在一个地方太久则尝试重新计算

        public int forceFollowingThisPathCounter = 0;//计算完成一次之后强制路径跟踪40帧，在这个空档期可以进行更多的路径计算而不干扰跟踪效果
        public int randomDelay = 0; //随机延迟，防止多个矛同时计算路径

        public bool lastShouldMagnetism = false;

        public bool shouldUpdatePath = true;
        //用字典建立pather和ai的关系，如果两个矛可以使用类似的路径，那么可以两个使用同一个pather来计算路径
        public static Dictionary<MagnetismSpearAI, MagnetismQuickPathFinder> patherLinks = new Dictionary<MagnetismSpearAI, MagnetismQuickPathFinder>();

        public MagnetismSpearAI(FloatingCore floatingCoredule, Spear spear, Player player)
        {
            floatingCore = floatingCoredule;
            spearRef = new WeakReference<Spear>(spear);
            playerRef = new WeakReference<Player>(player);

            mode = Mode.Magnetism;
        }
        public void Update(Spear self)
        {
            switch (mode)
            {
                case Mode.Magnetism:
                    MagnetismUpdate(self);
                    break;
                case Mode.OnRing:
                    OnRingUpdate(self);
                    break;
            }
        }

        public void MagnetismUpdate(Spear self)
        {
            //按下吸附的时候重新计算路径
            if (lastShouldMagnetism != floatingCore.ShouldMagnetismSpears && floatingCore.ShouldMagnetismSpears)
            {
                Debug.Log("ReUpdate Path");
                shouldUpdatePath = true;
                forceFollowingThisPathCounter = 0;
                randomDelay = Random.Range(0, 5);
            }
            lastShouldMagnetism = floatingCore.ShouldMagnetismSpears;

            //不吸附矛的时候更改矛的状态为正常状态
            if (!floatingCore.ShouldMagnetismSpears)
            {
                self.gravity = 0.9f;
                self.GoThroughFloors = false;
                return;
            }

            //吸附时候更改矛的状态
            if (self.mode == Weapon.Mode.Carried || self.mode == Weapon.Mode.OnBack) return;
            else
            {
                self.ChangeMode(Weapon.Mode.Free);
            }
            if (!playerRef.TryGetTarget(out var player)) return;

            var newDest = player.coord;
            var room = self.room;

            if (newDest != dest || shouldUpdatePath)
            {
                dest = player.coord;
                shouldUpdatePath = true;
            }

            UpdatePath(room, self.abstractPhysicalObject.pos.Tile, dest.Tile + new IntVector2(0, 2));
            if (path == null) return;

            if (lastTile == self.abstractPhysicalObject.pos.Tile) stuckInPosCounter++;
            else
            {
                lastTile = self.abstractPhysicalObject.pos.Tile;
                stuckInPosCounter = 0;
            }

            self.gravity = 0f;
            self.GoThroughFloors = true;

            //移动矛
            Vector2 nextPathPos = room.MiddleOfTile(path.tiles[currentPathIndex]);

            Vector2 delta = Vector2.Lerp(self.firstChunk.vel, (nextPathPos - self.firstChunk.pos), 0.05f) - self.firstChunk.vel;
            delta = Vector2.ClampMagnitude(delta, 1f);
            self.firstChunk.vel += delta;
            self.firstChunk.vel = Vector2.ClampMagnitude(self.firstChunk.vel, 15f);
            self.rotation = self.firstChunk.vel.normalized;

            //更新路径点
            if (Custom.DistLess(nextPathPos, self.firstChunk.pos, 20f) && currentPathIndex < path.Length - 1)
            {
                var shortcut = room.shortcutData(nextPathPos);
                ShortcutData? nextPathShortcutData = null;
                if (currentPathIndex + 1 < path.Length)
                {
                    nextPathShortcutData = room.shortcutData(path.tiles[currentPathIndex + 1]);
                }
                if (shortcut.shortCutType == ShortcutData.Type.Normal && nextPathShortcutData != null && nextPathShortcutData.Value.shortCutType == shortcut.shortCutType)
                {
                    SuckIntoShortcut(shortcut.StartTile, self.room);
                }
                currentPathIndex++;
            }
            if (stuckInPosCounter > 80 && currentPathIndex < path.Length - 1)
            {
                shouldUpdatePath = true;
                stuckInPosCounter = 0;
            }

            //
            if (Custom.DistLess(floatingCore.ring.ringPos, self.firstChunk.pos, 40f))
            {
                Debug.Log("Change Mode to OnRing");
                mode = Mode.OnRing;
                floatingCore.ring.AddToRing(this);
            }
        }
        public void OnRingUpdate(Spear self)
        {
            if (self.mode == Weapon.Mode.Carried || self.mode == Weapon.Mode.OnBack)
            {
                self.gravity = 0.9f;
                self.GoThroughFloors = false;
                mode = Mode.Magnetism;
                Debug.Log("Change Mode to Magnetism");
                return;
            }
            else
            {
                self.gravity = 0f;
                self.GoThroughFloors = true;
                self.ChangeMode(Weapon.Mode.Free);
                self.spinning = false;
            }

            Vector2 targetPos = floatingCore.ring.GetPosOnRing(this, true);
            self.firstChunk.pos = Vector2.Lerp(self.firstChunk.pos, targetPos, 0.5f);
            self.rotation = Custom.DirVec(self.firstChunk.lastPos, self.firstChunk.pos);
        }

        public void UpdatePath(Room room,IntVector2 start, IntVector2 dest)
        {
            if (shouldUpdatePath && forceFollowingThisPathCounter == 0 && randomDelay == 0)
            {
                if (!patherLinks.ContainsKey(this))
                {
                    patherLinks.Add(this, new MagnetismQuickPathFinder(start, dest, room.aimap));
                }
                var array = patherLinks.Keys.ToArray();
                MagnetismSpearAI? ai = array.Count() > 0 ? array[Random.Range(0, patherLinks.Count - 1)] : null;

                if (ai != null && ai != this)
                {
                    var patherForOther = patherLinks[ai];
                    if (room.VisualContact(start, patherForOther.start))
                    {
                        float distanceForThis = Custom.DistNoSqrt(start.ToVector2(), dest.ToVector2());
                        float distanceForOther = Custom.DistNoSqrt(patherForOther.start.ToVector2(), patherForOther.goal.ToVector2());

                        if (distanceForOther < distanceForThis)
                        {
                            if (patherLinks.ContainsKey(this))
                            {
                                patherLinks[this].LinkedAiCount--;
                                patherLinks[this] = patherForOther;
                                patherForOther.LinkedAiCount++;
                                //Debug.Log(String.Format("Using other pather to process searching for path from {0} to {1}", patherLinks[this].start, patherLinks[this].goal));
                            }
                        }
                        else
                        {
                            if (patherLinks.ContainsKey(ai))
                            {
                                patherLinks[ai].LinkedAiCount--;
                                patherLinks[ai] = patherLinks[this];
                                patherLinks[this].LinkedAiCount++;
                                //Debug.Log(String.Format("Start process searching for path from {0} to {1}", patherLinks[this].start, patherLinks[this].goal));
                            }
                        }
                    }
                }
            }
            else
            {
                if (randomDelay > 0)
                {
                    randomDelay--;
                    return;
                }

                if (forceFollowingThisPathCounter > 0)
                {
                    forceFollowingThisPathCounter--;
                    return;
                }
            }

            if (!(patherLinks.TryGetValue(this, out var updatePath))) return;
            //每帧对于一个AI最多计算120步，对于一个中型房间，差不多需要2000步才能完成一次路径跟踪，当然这也取决于距离
            //同时因为多个AI会引用同一个Pather，所以同一批update中可能会出现多次对同一个pather的更新
            int tempStep = 0;
            while (updatePath.status == 0 && tempStep < Mathf.Max(5,(int)(120f / updatePath.LinkedAiCount))) 
            {
                updatePath.Update();
                tempStep++;
            }
            if(updatePath.status == 0) return;


            Debug.Log(String.Format("Finish searching for path from {0} to {1}", updatePath.start, updatePath.goal));

            //因为该方法返回的是一个全新的实例，包括其中的tiles变量，不存在引用了同一个对象的问题
            QuickPath quickPath = updatePath.ReturnPath();

            //移除patherLinks中的引用，该pather已经完成寻路，待链接的矛获取Path数据后即可销毁
            patherLinks.Remove(this);

            //未能成功找到路径，直接返回等待下一次计算
            if (quickPath == null)
            {
                return;
            }

            int currentIndex = 0;
            IntVector2 first = quickPath.tiles[currentIndex++];
            List<IntVector2> allTiles = quickPath.tiles.ToList();

            //移除不必要的路径点，如果当前路径点可以直接看到下一个路径点，那么就可以把下一个移除了
            //同时判断是不是要进入管道，管道出入口不应当忽略
            while(currentIndex < allTiles.Count - 1)
            {
                IntVector2 next = allTiles[currentIndex];
                if (room.VisualContact(next, first) && (room.shortcutData(next).shortCutType == ShortcutData.Type.DeadEnd))
                {
                    allTiles.Remove(next);
                }
                else
                {
                    first = next;
                    currentIndex++;
                }
            }

            quickPath.tiles = allTiles.ToArray();
            path = quickPath;
            currentPathIndex = 0;
            shouldUpdatePath = false;
            forceFollowingThisPathCounter = 40;
        }

        public void SuckIntoShortcut(IntVector2 startTile,Room room)
        {
            if(!spearRef.TryGetTarget(out var self))return;
            var vessel = new SpearShortcutVessel(self, SpearShortcutVessel.GetVirtualAbCreature(), self.room);//利用一个包装的生物把矛从管道中带走
            self.room.AddObject(vessel);
            vessel.SuckedIntoShortCut(startTile, false);
        }

        public void Attack(Player player,Vector2 targetPos)
        {
            if(!spearRef.TryGetTarget(out var self))return;
            Vector2 dir = (targetPos - self.firstChunk.pos).normalized;

            self.Thrown(player, self.firstChunk.pos, -dir * 1000f, new IntVector2(dir.x > 0 ? 1 : -1, 0), 1.5f, true);

            float vel = self.firstChunk.vel.magnitude;
            self.firstChunk.vel = vel * dir;
            self.rotation = dir;
            self.setRotation = dir;

            self.gravity = 0.9f;
            self.GoThroughFloors = false;
            mode = Mode.Magnetism;
        }

        public enum Mode
        {
            Magnetism,
            OnRing,
        }
    }

    public class MagnetismQuickPathFinder : QuickPathFinder
    {
        public MagnetismQuickPathFinder(IntVector2 start,IntVector2 goal,AImap aimap) : base(start,goal,aimap,StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly))
        {
        }

        public int _linkedAICount = 1;
        public int LinkedAiCount
        {
            get => _linkedAICount;
            set => _linkedAICount = Mathf.Max(value,1);
        }
    }
}
