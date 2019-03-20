using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WDLockStep
{
    public class MoveData
    {
        public long x, y;

        public static MoveData Parse(string[] tmps)
        {
            var item = new MoveData();
            item.x = System.Convert.ToInt64(tmps[0]);
            item.y = System.Convert.ToInt64(tmps[1]);

            return item;
        }
    }
}