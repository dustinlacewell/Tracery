using System;
using System.Collections.Generic;
using System.Linq;


namespace Tracery {

    public static class ListExtensions {
        
        private static Random _rnd = new Random();

        public static void Seed(int seed) {
            _rnd = new Random(seed);
        }

        public static T PickRandom<T>(this List<T> source) {
            int count = source.Count();
            int index = _rnd.Next(count);
            return source[index];
        }

    }

}