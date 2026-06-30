using System;
using UnityEngine;

namespace StoreSim.NPC
{
    public static class TransformUtils
    {
        public static T[] FilterNull<T>(T[] items) where T : class
        {
            if (items == null || items.Length == 0)
                return Array.Empty<T>();

            int count = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    count++;
            }

            if (count == 0)
                return Array.Empty<T>();

            var result = new T[count];
            int index = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    result[index++] = items[i];
            }

            return result;
        }

        public static T PickRandom<T>(T[] items) where T : class
        {
            if (items == null || items.Length == 0)
                return null;

            return items[UnityEngine.Random.Range(0, items.Length)];
        }
    }
}
