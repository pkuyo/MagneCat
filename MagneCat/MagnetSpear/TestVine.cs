using MagneCat.MagnetSpear;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MagneCat
{
    public class TestVine : Vine, IDrawable
    {
        public TestVine(Room room,float length,Vector2 posA,Vector2 posB,bool stuckAtA,bool stuckAtB) : base(room, length, posA, posB, stuckAtA, stuckAtB)
        {
            graphic = new TestVineGraphic(this, segments.GetLength(0));

            room.AddObject(new Hooker(this, SpearShortcutVessel.GetVirtualAbCreature(), room));
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            graphic.InitiateSprites(sLeaser, rCam);

            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            graphic.DrawSprite(sLeaser, rCam, timeStacker, camPos);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            graphic.ApplyPalette(sLeaser, rCam, palette);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.RemoveAllSpritesFromContainer();
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
        }

        public class TestVineGraphic : VineGraphic
        {
            public TestVineGraphic(Vine owner, int segment) : base(owner, segment, 0)
            {
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[this.firstSprite] = TriangleMesh.MakeLongMeshAtlased(this.segments.Length, false, true);

                this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            }

            public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                Vector2 vector = Vector2.Lerp(this.segments[0].lastPos, this.segments[0].pos, timeStacker);
                vector += Custom.DirVec(Vector2.Lerp(this.segments[1].lastPos, this.segments[1].pos, timeStacker), vector) * 1f;
                float num = 2f;
                for (int i = 0; i < this.segments.Length; i++)
                {
                    float num2 = (float)i / (float)(this.segments.Length - 1);
                    Vector2 vector2 = Vector2.Lerp(this.segments[i].lastPos, this.segments[i].pos, timeStacker);
                    if (i < this.segments.Length - 1)
                    {
                        Vector2.Lerp(this.segments[i + 1].lastPos, this.segments[i + 1].pos, timeStacker);
                    }
                    Vector2 vector3 = Custom.PerpendicularVector((vector - vector2).normalized);
                    (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4, vector - vector3 * num - camPos);
                    (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 1, vector + vector3 * num - camPos);
                    (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - vector3 * num - camPos);
                    (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + vector3 * num - camPos);
                    vector = vector2;
                }
            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                for (int i = 0; i < (sLeaser.sprites[this.firstSprite] as TriangleMesh).vertices.Length; i++)
                {
                    float floatPos = (float)i / (float)((sLeaser.sprites[this.firstSprite] as TriangleMesh).vertices.Length - 1);
                    (sLeaser.sprites[this.firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(palette.blackColor, this.owner.EffectColor, this.OnVineEffectColorFac(floatPos));
                }
            }
        }

        public class Hooker : Creature
        {
            Vine vine;
            public Hooker(Vine owner,AbstractCreature obj, Room room) : base(obj, room.world)
            {
                vine = owner;

                obj.pos = room.GetWorldCoordinate(owner.segments[0, 0]);

                bodyChunks = new BodyChunk[1];
                bodyChunks[0] = new BodyChunk(this, 0, Vector2.zero, 10f, 1f);
                bodyChunkConnections = new BodyChunkConnection[0];
            }

            public override bool CanBeGrabbed(Creature grabber)
            {
                return grabber is Player;
            }

            public override void Update(bool eu)
            {
                base.Update(eu);

                Vector2 aimPos = firstChunk.pos;
                Vector2 caculatePos = firstChunk.pos;
                if (grabbedBy != null && grabbedBy.Count > 0)
                {
                    caculatePos = grabbedBy[0].grabber.DangerPos;
                }

                for(int i = 0;i < vine.graphic.segments.Length - 1; i++)
                {
                    Vector2 vinePosLeft = vine.graphic.segments[i].pos;
                    Vector2 VinePosRight = vine.graphic.segments[i + 1].pos;

                    if (caculatePos.x < VinePosRight.x && caculatePos.x > vinePosLeft.x)
                    {
                        float t = Mathf.InverseLerp(vinePosLeft.x,VinePosRight.x, caculatePos.x);
                        aimPos = Vector2.Lerp(vinePosLeft, VinePosRight, t);
                        break;
                    }
                }

                if (firstChunk.pos.x < vine.graphic.segments[0].pos.x)
                {
                    aimPos = vine.graphic.segments[0].pos;
                }
                if (firstChunk.pos.x > vine.graphic.segments[vine.graphic.segments.Length - 1].pos.x)
                {
                    aimPos = vine.graphic.segments[vine.graphic.segments.Length - 1].pos;
                }

                firstChunk.pos = aimPos;

                Vector2 delta = aimPos - firstChunk.pos;
                firstChunk.vel = delta * 0.5f;
            }

            public override void Die()
            {
            }

            public override void InitiateGraphicsModule()
            {
                if (graphicsModule == null) graphicsModule = new HookerGraphic(this);
            }

            public class HookerGraphic : GraphicsModule
            {
                public HookerGraphic(PhysicalObject owner) : base(owner, false)
                {
                }

                public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
                {
                    sLeaser.sprites = new FSprite[1];
                    sLeaser.sprites[0] = new FSprite("pixel", true) { color = Color.black, scaleX = 15f, scaleY = 15f ,isVisible = true};
                    AddToContainer(sLeaser, rCam, null);
                    base.InitiateSprites(sLeaser, rCam);
                }

                public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
                {
                    base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                    sLeaser.sprites[0].SetPosition(owner.firstChunk.pos - camPos);

                    Debug.Log(owner.firstChunk.pos);

                }

                public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
                {
                    rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
                }
            }
        }
    }
}
