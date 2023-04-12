using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MagneCat.MagnetEnergyHUD;

namespace MagneCat.hook
{
    static class EnergyGraphicsHook
    {
        static EnergyGraphicsHook()
        {
            EnergyGraphics = new ConditionalWeakTable<PlayerGraphics, EnergyGraphicsModule>();
        }
            

        static public void OnModInit()
        {
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;

            On.Player.Update += Player_Update;
        }

        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (EnergyGraphics.TryGetValue(self, out var module))
                module.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (EnergyGraphics.TryGetValue(self, out var module))
                module.AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (EnergyGraphics.TryGetValue(self, out var module))
                module.DrawSprites(sLeaser,rCam,timeStacker,camPos); 
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self,sLeaser,rCam);
            if(EnergyGraphics.TryGetValue(self,out var module))
                module.InitSprites(sLeaser,rCam);
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if(!EnergyGraphics.TryGetValue(self, out var _))
                EnergyGraphics.Add(self,new EnergyGraphicsModule(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.graphicsModule != null&& EnergyGraphics.TryGetValue(self.graphicsModule as PlayerGraphics, out var module))
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    module.AddEnergy(-10f);
                }
            }
        }

        static public ConditionalWeakTable<PlayerGraphics, EnergyGraphicsModule> EnergyGraphics;
    }
    class EnergyGraphicsModule
    {
        public EnergyGraphicsModule(PlayerGraphics self)
        {
            graphicsRef = new WeakReference<PlayerGraphics> (self);
            energyVar = new GradiantFloat(100f);
            retentionEnergyVar = new GradiantFloat(100f);
            for(int i=0;i<60;i++)
            {
                energyGoalQueue.Enqueue(100f);
            }
        }

        public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            startIndex = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);


            sLeaser.sprites[startIndex] = new FSprite("Futile_White", true);
            sLeaser.sprites[startIndex].anchorY = 0f;
            sLeaser.sprites[startIndex].shader = rCam.room.game.rainWorld.Shaders["CircleHUD"];
            sLeaser.sprites[startIndex].scale = 1.5f;


            rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[startIndex]);
  
        }

        public void SetEnergy(float newEnergy)
        {
            Energy = newEnergy;
        }

        public void AddEnergy(float energy)
        {
            Energy += energy;
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (introCounter > 0)
                introCounter -= Time.deltaTime;
            if (introCounter > 0)
                introTimer = Mathf.Lerp(introTimer, 1, 0.1f * 40 * Time.deltaTime);
            else
                introTimer = Mathf.Lerp(introTimer, 0, 0.1f * 40 * Time.deltaTime);

            conTick += Time.deltaTime;
            if(conTick>1/60f)
            {
                conTick -= 1 / 60f;
                energyGoalQueue.Enqueue(Energy);
                energyGoalQueue.Dequeue();
            }

            energyVar.LerpToGoal(0.1f);
            retentionEnergyVar.LerpToGoal(energyGoalQueue.First(), 0.1f * 40 * Time.deltaTime);

            sLeaser.sprites[startIndex].x = sLeaser.sprites[3].x;
            sLeaser.sprites[startIndex].y = sLeaser.sprites[3].y + 20;

            param.r = energyVar.currentVar/100f;
            param.g = retentionEnergyVar.currentVar / 100f;
            param.b = introTimer;
            sLeaser.sprites[startIndex].color = param;
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (startIndex != -1 && startIndex < sLeaser.sprites.Length)
            {
                rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[startIndex]);
   
            }
        }
        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (startIndex != -1 && startIndex < sLeaser.sprites.Length)
            {
                sLeaser.sprites[startIndex].color = param;
            }
    
        }

        private int startIndex = -1;

        private WeakReference<PlayerGraphics> graphicsRef;


        //TODO : 平滑
        public float Energy
        {
            get => energyVar.goalVar;
            set
            {
                
                if (energyVar.goalVar != value && value >= 0)
                {
                    introCounter = 7f;
                    energyVar.goalVar = value;
                }
            }
        }
        Color param = Color.white;
        GradiantFloat energyVar;
        GradiantFloat retentionEnergyVar;

        float conTick = 0.0f;
        float introCounter = 0.0f;
        float introTimer = 0.0f;

        public Queue<float> energyGoalQueue = new Queue<float>(60);
    }
    public class GradiantFloat
    {
        public float goalVar;
        public float currentVar;
        public float lastVar;

        public GradiantFloat(float origVal)
        {
            goalVar = origVal;
            currentVar = origVal;
            lastVar = origVal;
        }

        public float LerpToGoal(float t)
        {
            return LerpToGoal(goalVar, t);
        }

        public float LerpToGoal(float newGoal, float t)
        {
            goalVar = newGoal;

            t = Mathf.Clamp01(t);


            currentVar = lastVar * (1f - t) + goalVar * t;
            lastVar = currentVar;
            return currentVar;
        }
    }
}
