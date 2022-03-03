using System;
using System.Collections.Generic;
using System.Linq;

namespace Lemon.Tools
{
    public static partial class Extensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> source)
        {
            foreach(var item in source)
            {
                collection.Add(item);
            }
        }
        
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                                       TKey                           key) where TValue : new()
        {
            if(!dict.TryGetValue(key, out var result))
            {
                result = new TValue();
                dict.Add(key, result);
            }

            return result;
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict,
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

        public static void AddWithException<TKey, TValue>(this IDictionary<TKey, TValue> dict,
                                                          TKey                           key,
                                                          TValue                         value)
        {
            if(dict.ContainsKey(key))
            {
                throw new ArgumentException($"Key:{key} already exists in {dict}. old:{dict[key]}. new:{value}");
            }

            dict.Add(key, value);
        }

        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> source, Predicate<TValue> predicate)
        {
            foreach(var pair in source.ToList())
            {
                if(predicate(pair.Value)) source.Remove(pair.Key);
            }
        }
    }
}