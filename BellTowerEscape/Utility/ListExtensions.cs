using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BellTowerEscape.Server;

namespace BellTowerEscape.Utility
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this IList<T> list, Game game)
        {
            var rng = game.Random;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}