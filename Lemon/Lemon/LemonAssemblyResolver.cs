using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Lemon
{
    public class LemonAssemblyResolver : BaseAssemblyResolver, IComparer<TargetDllInfo>
    {
        readonly Dictionary<string, AssemblyDefinition> _cache = new(StringComparer.Ordinal);

        private Dictionary<string, Assembly> _currentAssemblies;

        public void AddRead(AssemblyDefinition assembly)
        {
            var key = assembly.Name.Name;
            _cache[key] = assembly;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            var key = name.Name;
            if (_cache.TryGetValue(key, out var assembly))
            {
                return assembly;
            }

            if (_currentAssemblies == null)
            {
                var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                _currentAssemblies = currentAssemblies.ToDictionary(p => p.GetName().Name, p => p);
            }
            
            if (_currentAssemblies.TryGetValue(key, out var asmAssembly))
            {
                assembly = AssemblyDefinition.ReadAssembly(
                    asmAssembly.Location,
                    new ReaderParameters(ReadingMode.Deferred)
                    {
                        AssemblyResolver = this,
                        ReadWrite = false,
                        InMemory = false,
                        ReadSymbols = false,
                        SymbolReaderProvider = null
                    });

                _cache[key] = assembly;
                return assembly;
            }
            else
            {
                assembly = base.Resolve(name, new ReaderParameters(ReadingMode.Deferred)
                {
                    AssemblyResolver = this,
                    ReadWrite = false,
                    InMemory = false,
                    ReadSymbols = false,
                    SymbolReaderProvider = null
                });

                _cache[name.Name] = assembly;
                return assembly;
            }
        }

        public void Release(string shortName)
        {
            if (_cache.TryGetValue(shortName, out var assembly))
            {
                //assembly.Dispose();
                _cache.Remove(shortName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var assembly in _cache.Values)
                assembly.Dispose();

            _cache.Clear();
            base.Dispose(disposing);
        }

        public int Compare(TargetDllInfo x, TargetDllInfo y)
        {
            if (!_cache.ContainsKey(x.AssemblyName)) return -1;
            if (!_cache.ContainsKey(y.AssemblyName)) return +1;

            if (!x.OpenAssemblyDefinition.MainModule.HasAssemblyReferences) return -1;
            if (!y.OpenAssemblyDefinition.MainModule.HasAssemblyReferences) return +1;

            if (x.OpenAssemblyDefinition.MainModule.AssemblyReferences.Any(p => p.Name == y.AssemblyName))
            {
                return +1;
            }

            if (y.OpenAssemblyDefinition.MainModule.AssemblyReferences.Any(p => p.Name == x.AssemblyName))
            {
                return -1;
            }

            return 0;
        }


    }
}