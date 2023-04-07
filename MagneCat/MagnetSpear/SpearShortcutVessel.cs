using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MagneCat.MagnetSpear
{
    public class SpearShortcutVessel : Creature
    {
        AbstractCreature virtualABCreature;
        public SpearShortcutVessel(Spear vesseledSpear,AbstractCreature virtualABCreature,Room room) : base(virtualABCreature, room.world)
        {
            this.virtualABCreature = virtualABCreature;
            virtualABCreature.pos = vesseledSpear.abstractPhysicalObject.pos;
            virtualABCreature.world = room.world;

            room.abstractRoom.AddEntity(virtualABCreature);

            bodyChunks = new BodyChunk[1];
            bodyChunks[0] = new BodyChunk(this, 0, Vector2.zero, 0.1f, 0.1f);

            virtualABCreature.stuckObjects.Add(new AbstractPhysicalObject.AbstractSpearStick(vesseledSpear.abstractPhysicalObject,virtualABCreature, 0,0,0));
        }

        public override void SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            base.SpitOutOfShortCut(pos, newRoom, spitOutAllSticks);
            Debug.Log("SpearShorcutVessel spit out");

            foreach(var stuck in abstractCreature.stuckObjects)
            {
                Debug.Log(stuck.A.ToString() + "-" + stuck.B.ToString());
            }
            Destroy();
        }

        public override Color ShortCutColor()
        {
            return Color.yellow;
        }

        public static AbstractCreature GetVirtualAbCreature()
        {
            var newAB = (FormatterServices.GetSafeUninitializedObject(typeof(AbstractCreature))) as AbstractCreature;

            //AbstractPhysicalObject.ctor
            newAB.type = AbstractPhysicalObject.AbstractObjectType.Creature;
            newAB.stuckObjects = new List<AbstractPhysicalObject.AbstractObjectStick>();

            //AbstractCreature.ctor
            newAB.creatureTemplate = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly);
            newAB.state = new NoHealthState(newAB);

            return newAB;
        }
    }
}
