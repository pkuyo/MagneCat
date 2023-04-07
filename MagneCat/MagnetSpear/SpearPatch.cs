using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MagneCat.MagnetSpear
{
    public class SpearPatch
    {
        public static ConditionalWeakTable<Spear, MagnetismSpearAI> magnetsmSpearAIs = new ConditionalWeakTable<Spear, MagnetismSpearAI>();
        public static void OnModInit()
        {
            On.Spear.Update += Spear_Update;
        }

        private static void Spear_Update(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig.Invoke(self, eu);
            if(magnetsmSpearAIs.TryGetValue(self,out var magnetismSpearAI))
            {
                magnetismSpearAI.Update(self);
            }
        }
    }
}
