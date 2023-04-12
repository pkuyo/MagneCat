using HUD;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;

namespace MagneCat
{
    public class HUDPatch
    {
        public static void PatchOn()
        {
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (Input.GetKeyDown(KeyCode.L))
            {
                MagnetEnergyHUD.StaticSetEnergy(-10f);
            }
        }

        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig.Invoke(self, cam);
            if (MagnetEnergyHUD.instance == null) self.AddPart(new MagnetEnergyHUD(self, new Vector2(300f, 10f), 200f));
        }
    }

    sealed class MagnetEnergyHUD : HudPart
    {
        public static MagnetEnergyHUD instance;

        public bool Show
        {
            get => hud.owner.RevealMap || hud.showKarmaFoodRain || forceShowCounter > 0;
        }

        public int noEnergyChangeCounter = 0;
        public int forceShowCounter = 0;

        public float maxEnergy;

        public Vector2 pos;
        public Vector2 size;

        public FSprite backgroundSprite;
        public FSprite energySprite;
        public FSprite retentionSprite;

        public GradientCol backgroundColor;
        public GradientCol energyColor;
        public GradientCol retentionColor;

        public GradiantVar<float> backgroundAlpha;
        public GradiantVar<float> energyAlpha;
        public GradiantVar<float> retentionAlpha;

        public GradiantVar<float> smoothedEnergy;
        public GradiantVar<float> smoothedRetentionEnergy;

        public Queue<float> energyGoalQueue = new Queue<float>(60);//remeber 60 frame in total;

        public MagnetEnergyHUD(HUD.HUD hud,Vector2 size,float maxEnergy) : base(hud)
        {
            instance = this;
            this.maxEnergy = maxEnergy;

            backgroundSprite = new FSprite("pixel", true) { anchorX = 0f};
            energySprite = new FSprite("pixel", true) {anchorX = 0f};
            retentionSprite = new FSprite("pixel", true) { anchorX = 0f };

            backgroundColor = new GradientCol(Color.cyan * 0.5f);
            energyColor = new GradientCol(Color.white);
            retentionColor = new GradientCol(Color.gray);

            backgroundAlpha = new GradiantVar<float>(0f);
            energyAlpha = new GradiantVar<float>(0f);
            retentionAlpha = new GradiantVar<float>(0f);
            smoothedEnergy = new GradiantVar<float>(maxEnergy);
            smoothedRetentionEnergy = new GradiantVar<float>(maxEnergy);

            hud.fContainers[0].AddChild(backgroundSprite);
            hud.fContainers[0].AddChild(energySprite);
            hud.fContainers[0].AddChild(retentionSprite);
            
            retentionSprite.MoveInFrontOfOtherNode(backgroundSprite);
            energySprite.MoveInFrontOfOtherNode(retentionSprite);

            for(int i = 0;i < 60; i++)
            {
                energyGoalQueue.Enqueue(maxEnergy);
            }


            this.size = size;
            pos = new Vector2(Mathf.Max(55.01f, hud.rainWorld.options.SafeScreenOffset.x + 22.51f), Mathf.Max(45.01f, hud.rainWorld.options.SafeScreenOffset.y + 22.51f)) + Vector2.up * 90f;
        }

        public override void Update()
        {
            base.Update();
            energyGoalQueue.Dequeue();
            energyGoalQueue.Enqueue(smoothedEnergy.goalVar);
            if (forceShowCounter > 0) forceShowCounter--;
            if(noEnergyChangeCounter > 0) noEnergyChangeCounter--;
            else
            {
                StaticSetEnergyForASecond(10f);
            }
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            smoothedEnergy.LerpToGoal(0.1f);
            smoothedRetentionEnergy.LerpToGoal(0.1f);
           
            float t = Mathf.InverseLerp(0, maxEnergy, smoothedEnergy.currentVar);
            float tt = Mathf.InverseLerp(0,maxEnergy, smoothedRetentionEnergy.LerpToGoal(energyGoalQueue.First(),0.1f));

            //Debug.Log(String.Format("{0} - {1}",smoothedEnergy.currentVar,smoothedRetentionEnergy.currentVar));
            backgroundSprite.SetPosition(pos);
            backgroundSprite.color = backgroundColor.LerpToGoal(0.1f);
            backgroundSprite.alpha = backgroundAlpha.LerpToGoal(Show ? 1f : 0f,0.1f);
            backgroundSprite.scaleX = size.x;
            backgroundSprite.scaleY = size.y;

            energySprite.SetPosition(pos);
            energySprite.color = energyColor.LerpToGoal(0.1f);
            energySprite.alpha = energyAlpha.LerpToGoal(Show ? 1f : 0f, 0.1f);
            energySprite.scaleX = size.x * t;
            energySprite.scaleY = size.y;

            retentionSprite.SetPosition(pos + Vector2.right * size.x * t);
            retentionSprite.color = retentionColor.LerpToGoal(0.1f);
            retentionSprite.alpha = retentionAlpha.LerpToGoal(Show ? 1f : 0f, 0.1f);
            retentionSprite.scaleX =  size.x * Mathf.Clamp01(tt - t);
            retentionSprite.scaleY = size.y;
        }

        public override void ClearSprites()
        {
            backgroundSprite.RemoveFromContainer();
            energySprite.RemoveFromContainer();
            retentionSprite.RemoveFromContainer();
            instance = null;
            base.ClearSprites();
        }

        public void SetEnergy(float change)
        {
            smoothedEnergy.goalVar = Mathf.Clamp(smoothedEnergy.goalVar + change,0, maxEnergy);
            if(change < 0) noEnergyChangeCounter = 80;
            forceShowCounter = 360;
        }

        public static void StaticSetEnergy(float change)
        {
            instance?.SetEnergy(change);
        }

        public static void StaticSetEnergyForASecond(float change)
        {
            instance?.SetEnergy(change / 40f);
        }

        public static bool CanUseEnergy()
        {
            if(instance == null) return false;
            return instance.smoothedEnergy.currentVar > 0.001f;
        }

        public class GradientCol
        {
            public Color goalColor;
            public Color currentColor;
            public Color lastColor;
            public GradientCol(Color origCol)
            {
                goalColor = origCol;
                currentColor = origCol;
                lastColor = origCol;
            }

            public Color LerpToGoal(float t)
            {
                return LerpToGoal(goalColor, t);
            }

            public Color LerpToGoal(Color newGoal,float t)
            {
                goalColor = newGoal;

                currentColor = Color.Lerp(lastColor, goalColor, t);
                lastColor = currentColor;

                return currentColor;
            }
        }

        public class GradiantVar<T> where T : IConvertible
        {
            public T goalVar;
            public T currentVar;
            public T lastVar;

            public GradiantVar(T origVal)
            {
                goalVar = origVal;
                currentVar = origVal;
                lastVar = origVal;
            }

            public T LerpToGoal(float t)
            {
                return LerpToGoal(goalVar, t);
            }

            public T LerpToGoal(T newGoal, float t)
            {
                goalVar = newGoal;

                dynamic a = lastVar;
                dynamic b = goalVar;
                t = Mathf.Clamp01(t);


                currentVar = a * (1f - t) + b * t;
                lastVar = currentVar;
                return currentVar;
            }
        }
    }
}
