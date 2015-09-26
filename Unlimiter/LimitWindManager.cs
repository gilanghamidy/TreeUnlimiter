﻿using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace TreeUnlimiter
{
    internal static class LimitWindManager
    {
        private static ushort CalculateSelfHeight(WindManager wm,int x, int z)
        {
            int num; //Terrain min
            int num1; //Terrain avg
            int num2; //Terrain max
            int num3; //Tree number of item's hit
            float single; //tree min
            float single1; //tree avg
            float single2; //tree max
            float single3 = ((float)x - 64f) * 135f;
            float single4 = ((float)z - 64f) * 135f;
            float single5 = ((float)(x + 1) - 64f) * 135f;
            float single6 = ((float)(z + 1) - 64f) * 135f;
            Singleton<TerrainManager>.instance.CalculateAreaHeight(single3, single4, single5, single6, out num, out num1, out num2);

            //new - ignore tree data.
            if (Mod.USE_NO_WINDEFFECTS)   //My Additions to overide tree effects.
            {    
                /*
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL >=2)
                {
                    Debug.Log(string.Concat("[TreeUnlimiter::LimitWindManager:CalculateSelfHeight] num=",
                        num.ToString(), " num1=", num1.ToString(), " num2=", num2.ToString(), " num3=", num3.ToString(),
                        " \n num4=", num4.ToString(), " num5=", num5.ToString(), " num6=", num6.ToString(),
                        "\n Returning:", ((ushort)Mathf.Clamp(num6 + num7 >> 1, 0, 65535)).ToString()));
                }
                */
                return (ushort)Mathf.Clamp(num1 + num2 >> 1, 0, 65535);
            }


            Singleton<TreeManager>.instance.CalculateAreaHeight(single3, single4, single5, single6, out num3, out single, out single1, out single2);
            int num4 = Mathf.RoundToInt(single1 * 64f); //treeavg height
            int num5 = Mathf.RoundToInt(single2 * 64f); //treemax height
            int num6 = num1 + (num4 - num1) * Mathf.Min(num3, 100) / 100; 
            // num6 translated = TerrMin + (newTreeAvg -Terrainmin ) * Min(TreeHits,100) /100
            int num7 = Mathf.Max(num2, num5);
            //num7 translated = max of (TerrainMaxheight or TreeMaxHeight)
            //org
            return (ushort)Mathf.Clamp(num6 + num7 >> 1, 0, 65535);

        } 
    }
}
