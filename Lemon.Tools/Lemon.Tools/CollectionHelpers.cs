using System;
using System.Collections.Generic;
using System.Linq;

namespace Lemon.Tools
{
    public static class CollectionHelpers
    {
        internal static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> source)
        {
            foreach(var item in source)
            {
                collection.Add(item);
            }
        }

        internal static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                                       TKey                           key) where TValue : new()
        {
            if(!dict.TryGetValue(key, out var result))
            {
                result = new TValue();
                dict.Add(key, result);
            }

            return result;
        }

        internal static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                                       TKey                           key,
                                                       Func<TKey, TValue>             ctor)
        {
            if(!dict.TryGetValue(key, out var result))
            {
                result = ctor(key);
                dict.Add(key, result);
            }

            return result;
        }

        internal static void AddWithException<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                                          TKey                           key,
                                                          TValue                         value)
        {
            if(dict.TryGetValue(key, out var oldValue))
            {
                throw new ArgumentException($"Key:{key} already exists in {dict}. old:{oldValue}. new:{value}");
            }

            dict.Add(key, value);
        }

        internal static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> source, Predicate<TValue> predicate)
        {
            foreach(var pair in source.ToList())
            {
                if(predicate(pair.Value)) source.Remove(pair.Key);
            }
        }
    }
}