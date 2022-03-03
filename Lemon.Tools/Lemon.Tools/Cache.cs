using System.Collections.Generic;
using Mono.Cecil;

namespace Lemon.Tools
{
    public class Cache
    {
        public static readonly Cache Instance = new Cache();

        private Cache()
        {
        }

        public readonly Dictionary<(string typeName, string baseName), bool> IsSubclassOf = new Dictionary<(string typeName, string baseName), bool>();

        public readonly Dictionary<int, MethodReference> ImportMethodImpl = new Dictionary<int, MethodReference>();

        public void Clear()
        {
            // IsSubclassOf.Clear();
            ImportMethodImpl.Clear();
        }
    }
}