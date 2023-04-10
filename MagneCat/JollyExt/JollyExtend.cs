using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using JollyCoop;
using JollyCoop.JollyMenu;
using Menu;
using RWCustom;
using UnityEngine;
using Color = UnityEngine.Color;

namespace MagneCat
{
    public class JollyExtend
    {
        public static ConditionalWeakTable<JollyPlayerOptions, JollyPlayerOptionModule> modules = new ConditionalWeakTable<JollyPlayerOptions, JollyPlayerOptionModule>();
        public static void PatchOn()
        {
            On.JollyCoop.JollyMenu.JollyPlayerOptions.ctor += JollyPlayerOptions_ctor;

            On.JollyCoop.JollyMenu.JollyPlayerOptions.SetColorsToDefault += JollyPlayerOptions_SetColorsToDefault;

            On.JollyCoop.JollyMenu.JollyPlayerOptions.FromString += JollyPlayerOptions_FromString;
            On.JollyCoop.JollyMenu.JollyPlayerOptions.ToString += JollyPlayerOptions_ToString;

            On.PlayerGraphics.LoadJollyColorsFromOptions += PlayerGraphics_LoadJollyColorsFromOptions;
        }

        private static void PlayerGraphics_LoadJollyColorsFromOptions(On.PlayerGraphics.orig_LoadJollyColorsFromOptions orig, int playerNumber)
        {
            orig.Invoke(playerNumber);

            if(modules.TryGetValue(Custom.rainWorld.options.jollyPlayerOptionsArray[playerNumber], out var module))
            {
                //不到四个颜色默认使用原版的方法。
                if (PlayerGraphics.DefaultBodyPartColorHex(Custom.rainWorld.options.jollyPlayerOptionsArray[playerNumber].playerClass).Count < 4) return;
                //Debug.Log("Get Custom Color from options");

                int origLength = PlayerGraphics.jollyColors[playerNumber].Length;
                //Debug.Log(string.Format("Array size from {0} to {1}", origLength, origLength + module.uniqueColors.Length));
                Array.Resize(ref PlayerGraphics.jollyColors[playerNumber], origLength + module.uniqueColors.Length);
                for(int i = 0;i < module.uniqueColors.Length; i++)
                {
                    PlayerGraphics.jollyColors[playerNumber][i + origLength] = module.GetUniqueColorOfIndex(i);
                }
            }
        }

        private static string JollyPlayerOptions_ToString(On.JollyCoop.JollyMenu.JollyPlayerOptions.orig_ToString orig, JollyPlayerOptions self)
        {
            string result = orig.Invoke(self);
            if(modules.TryGetValue(self,out var module))
            {
                result += module.OptionToString();
            }

            //Debug.Log("ToString\n" + result);
            return result;
        }

        private static void JollyPlayerOptions_FromString(On.JollyCoop.JollyMenu.JollyPlayerOptions.orig_FromString orig, JollyPlayerOptions self, string origin)
        {
            if(modules.TryGetValue(self, out var module))
            {
                module.OptionFromString(ref origin);
            }
            try
            {
                orig.Invoke(self, origin);
            }
            catch
            {
            }
        }

        private static void JollyPlayerOptions_SetColorsToDefault(On.JollyCoop.JollyMenu.JollyPlayerOptions.orig_SetColorsToDefault orig, JollyPlayerOptions self, SlugcatStats.Name slugcat)
        {
            orig.Invoke(self, slugcat);
            if (modules.TryGetValue(self, out var module))
            {
                module.SetColorsToDefault(self,slugcat);
            }
        }

        private static void JollyPlayerOptions_ctor(On.JollyCoop.JollyMenu.JollyPlayerOptions.orig_ctor orig, JollyPlayerOptions self, int playerNumber)
        {
            orig.Invoke(self, playerNumber);
            if (!modules.TryGetValue(self, out var _)) modules.Add(self, new JollyPlayerOptionModule(self));
        }


        public class JollyPlayerOptionModule
        {
            public WeakReference<JollyPlayerOptions> playerOptionRef;

            public Color[] uniqueColors = null; 

            public JollyPlayerOptionModule(JollyPlayerOptions jollyPlayerOptions)
            {
                playerOptionRef = new WeakReference<JollyPlayerOptions>(jollyPlayerOptions);
            }

            public Color GetUniqueColorOfIndex(int index)
            {
                if (!playerOptionRef.TryGetTarget(out var option)) return Color.white;
                if(option.playerClass == null)return Color.white;

                var list = PlayerGraphics.DefaultBodyPartColorHex(option.playerClass);

                if (uniqueColors == null || uniqueColors.Length != Mathf.Max(0, list.Count - 3))
                {
                    Debug.Log(option.playerClass.ToString() + list.Count().ToString());

                    uniqueColors = new Color[Mathf.Max(0, list.Count - 3)];
                    for (int i = 3; i < list.Count; i++)
                    {
                        uniqueColors[i - 3] = Custom.hexToColor(list[i]);
                        Debug.Log("GetUniqueColorsAddCol : " + Custom.hexToColor(list[i]));
                    }
                }

                return uniqueColors[index];
            }

            public void SetUniqueColorOfIndex(int index,Color color)
            {
                uniqueColors[index] = color;
                if (!playerOptionRef.TryGetTarget(out var self)) return;
                self.colorsEverModified = true;
            }

            public void SetColorsToDefault(JollyPlayerOptions self, SlugcatStats.Name slugcat)
            {
                List<string> list = PlayerGraphics.DefaultBodyPartColorHex(slugcat);
                for(int i = 3;i < list.Count; i++)
                {
                    uniqueColors[i - 3] = Custom.hexToColor(list[i]);
                }
            }
            public void OptionFromString(ref string origin)
            {
                string[] array = origin.Split('#');
                Debug.Log("FromString\n" + origin);
                try
                {
                    foreach (var option in array)
                    {
                        if (!option.Contains("JollyPlayerOptionModule")) continue;

                        string replaced = Regex.Replace(option,"JollyPlayerOptionModule", "");
                        var colors = replaced.Split('_').ToList();

                        for(int i = colors.Count - 1; i >= 0; i--)
                        {
                            if (colors[i] == "") colors.RemoveAt(i);
                        }

                        if(uniqueColors == null && colors.Count > 0)    
                        {
                            uniqueColors = new Color[colors.Count];

                            for (int i = 0; i < colors.Count; i++)
                            {
                                uniqueColors[i] = (Custom.hexToColor(colors[i]));
                            }
                        }

                        origin = Regex.Replace(origin, "#" + option, "");
                    }
                }
                catch
                {
                }
            }

            public string OptionToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("JollyPlayerOptionModule");;

                if (uniqueColors == null) return "";
                foreach(var color in uniqueColors)
                {
                    string hex = Custom.colorToHex(color);
                    stringBuilder.Append(hex);
                    stringBuilder.Append('_');
                }
                stringBuilder.Append("#");
                return stringBuilder.ToString();
            }
        }
    }

    public class JollySetupDialogExtend
    {
        public static ConditionalWeakTable<ColorChangeDialog, ColorChangeDialogModule> modules = new ConditionalWeakTable<ColorChangeDialog, ColorChangeDialogModule> ();
        public static void PatchOn()
        {
            On.JollyCoop.JollyMenu.ColorChangeDialog.ctor += ColorChangeDialog_ctor;
            On.JollyCoop.JollyMenu.ColorChangeDialog.ActualSavingColor += ColorChangeDialog_ActualSavingColor;
            On.JollyCoop.JollyMenu.ColorChangeDialog.Singal += ColorChangeDialog_Singal;

            On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += ColorChangeDialog_SliderSetValue;
            On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += ColorChangeDialog_ValueOfSlider;
        }

        private static float ColorChangeDialog_ValueOfSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ValueOfSlider orig, ColorChangeDialog self, Menu.Slider slider)
        {
            if (modules.TryGetValue(self, out var module))
            {
                if (!slider.ID.value.Contains("JOLLY"))
                {
                    return 0f;
                }

                if (module.sliders.Count == 0) return orig.Invoke(self, slider);

                string[] array = slider.ID.value.Split('_');
                if (!int.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var nameIndex))
                {
                    return 0f;
                }
                int bodyPart2 = array[2] switch
                {
                    "SAT" => 1,
                    "LIT" => 2,
                    _ => 0,
                };

                ColorChangeDialog.ColorSlider colorSlider = null;
                if (nameIndex > 2)
                {
                    colorSlider = module.sliders[module.sliderBodyIndex.IndexOf(nameIndex)];
                }
                else
                {
                    return orig.Invoke(self, slider);
                }

                return GetCorrectColorDimension(colorSlider.hslColor, bodyPart2);

                static float GetCorrectColorDimension(HSLColor colorHSL, int bodyPart)
                {
                    return bodyPart switch
                    {
                        1 => Mathf.Clamp(colorHSL.saturation, 0f, 1f),
                        2 => Mathf.Clamp(colorHSL.lightness, 0.01f, 1f),
                        _ => Mathf.Clamp(colorHSL.hue, 0f, 0.99f),
                    };
                }
            }
            return orig.Invoke(self, slider);
        }

        private static void ColorChangeDialog_SliderSetValue(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_SliderSetValue orig, ColorChangeDialog self, Menu.Slider slider, float f)
        {
            if(modules.TryGetValue(self,out var module))
            {
                if (!slider.ID.value.Contains("JOLLY"))
                {
                    orig.Invoke(self, slider, f);
                    return;
                }

                string[] array = slider.ID.value.Split('_');
                if (int.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var nameIndex))
                {
                    int bodyPart2 = array[2] switch
                    {
                        "SAT" => 1,
                        "LIT" => 2,
                        _ => 0,
                    };

                    ColorChangeDialog.ColorSlider colorSlider = null;
                    if (nameIndex > 2)
                    {
                        colorSlider = module.sliders[module.sliderBodyIndex.IndexOf(nameIndex)];
                    }
                    else
                    {
                        orig.Invoke(self, slider, f);
                        return;
                    }

                    AssignCorrectColorDimension(f, ref colorSlider.hslColor, bodyPart2);
                    colorSlider.HSL2RGB();
                    self.selectedObject = slider;

                    static void AssignCorrectColorDimension(float f, ref HSLColor colorHSL, int bodyPart)
                    {
                        switch (bodyPart)
                        {
                            default:
                                colorHSL.hue = Mathf.Clamp(f, 0f, 0.99f);
                                break;
                            case 1:
                                colorHSL.saturation = Mathf.Clamp(f, 0f, 1f);
                                break;
                            case 2:
                                colorHSL.lightness = Mathf.Clamp(f, 0.01f, 1f);
                                break;
                        }
                    }
                    return;
                }
            }
            orig.Invoke(self, slider, f);
        }

        private static void ColorChangeDialog_Singal(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_Singal orig, ColorChangeDialog self, Menu.MenuObject sender, string message)
        {
            orig.Invoke(self,sender,message);
            if (modules.TryGetValue(self, out var module))
            {
                module.Signal(self, sender, message);
            }
        }

        private static void ColorChangeDialog_ActualSavingColor(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ActualSavingColor orig, ColorChangeDialog self)
        {
            orig.Invoke(self);
            if(modules.TryGetValue(self, out var module))
            {
                module.ActualSavingColor(self);
            }
        }

        private static void ColorChangeDialog_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ctor orig, ColorChangeDialog self, JollySetupDialog jollyDialog, SlugcatStats.Name playerClass, int playerNumber, ProcessManager manager, List<string> names)
        {
            orig.Invoke(self, jollyDialog, playerClass, playerNumber, manager, names);
            if(!modules.TryGetValue(self,out var module))
            {
                module = new ColorChangeDialogModule(self);
                modules.Add(self, module);
                module.OrigCtor(self, jollyDialog, playerClass, playerNumber, manager, names);
            }
        }

        public class ColorChangeDialogModule
        {
            public List<ColorChangeDialog.ColorSlider> sliders = new List<ColorChangeDialog.ColorSlider>();
            public List<int> sliderBodyIndex = new List<int>();

            public WeakReference<ColorChangeDialog> colorChangeDialogRef;
            public ColorChangeDialogModule(ColorChangeDialog colorChangeDialog)
            {
                colorChangeDialogRef = new WeakReference<ColorChangeDialog> (colorChangeDialog);
                Debug.Log("ColorChangeDialogModule ctor");
            }

            public void OrigCtor(ColorChangeDialog self, JollySetupDialog jollyDialog, SlugcatStats.Name playerClass, int playerNumber, ProcessManager manager, List<string> names)
            {
                if (names.Count > 3)
                {
                    string title = jollyDialog.Translate("COLOR CONFIGURATION");
                    foreach (var obj in self.pages[0].subObjects)
                    {
                        if(obj is RoundedRect)
                        {
                            (obj as RoundedRect).size.y *= 1.6f;
                            //(obj as RoundedRect).pos;
 
                        }
                        if(obj is MenuLabel)
                        {
                            if((obj as MenuLabel).text == title)
                            {
                                (obj as MenuLabel).pos.y += 190f;
                            }
                        }
                    }

                    //if(self.descriptionLabelLong != null) self.descriptionLabelLong.pos += Vector2.up * 380f;
                    //if (self.descriptionLabel != null) self.descriptionLabel.pos += Vector2.up * 130f;
                    self.resetButton.pos += Vector2.up * 180f;

                    self.body.pos.y += 190f;
                    self.face.pos.y += 190f;
                    self.unique.pos.y += 190f;

                    Vector2 vector = new Vector2(135f, 190f);

                    if (!JollyExtend.modules.TryGetValue(self.JollyOptions, out var module)) return;

                    for (int i = 3; i < names.Count; i++)
                    {
                        var bias = new Vector2(140f * (i - 3), 0f);
                        sliderBodyIndex.Add(i);
                        ColorChangeDialog.ColorSlider tempSlider = null;
                        self.AddSlider(ref tempSlider, jollyDialog.Translate(names[i]), vector + bias, self.playerNumber, i);
                        tempSlider.color = module.GetUniqueColorOfIndex(sliderBodyIndex.IndexOf(i));
                        tempSlider.RGB2HSL();
                        sliders.Add(tempSlider);
                        
                        Debug.Log(String.Format("ColorChangeDialogModule add new slider{0} : {1}", names[i], i));
                    }

                    try
                    {
                        self.Update();
                        self.GrafUpdate(0f);
                    }
                    catch
                    {
                    }
                }
            }

            public void ActualSavingColor(ColorChangeDialog self)
            {
                if (sliders.Count == 0) return;
                if (!JollyExtend.modules.TryGetValue(self.JollyOptions, out var module)) return;

                foreach(var slider in sliders)
                {
                    int colorIndex = sliders.IndexOf(slider);
                    slider.HSL2RGB();

                    Debug.Log(String.Format("ColorChangeDialogModule ActualSavingColor {0} : {1}", colorIndex, slider.color));
                    module.SetUniqueColorOfIndex(colorIndex, slider.color);
                }
            }

            public void Signal(ColorChangeDialog self, Menu.MenuObject sender, string message)
            {
                if (message.StartsWith("RESET_COLOR_DIALOG_"))
                {
                    if (sliders.Count == 0) return;
                    if (!JollyExtend.modules.TryGetValue(self.JollyOptions, out var module)) return;
                    
                    foreach(var slider in sliders)
                    {
                        int colorIndex = sliders.IndexOf(slider);
                        slider.color = module.GetUniqueColorOfIndex(colorIndex);
                        slider.RGB2HSL();

                        Debug.Log(String.Format("ColorChangeDialogModule RESET_COLOR_DIALOG_ {0} : {1}", slider, slider.color));
                    }
                }
            }
        }
    }
}
