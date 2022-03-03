using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Lemon
{
    public class LemonAssemblyResolver : BaseAssemblyResolver, IComparer<Processor.TargetInfo>
    {
        readonly Dictionary<string, AssemblyDefinition> _cache = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            if(_cache.TryGetValue(name.FullName, out var assembly))
                return assembly;

            try
            {
                assembly = base.Resolve(name, new ReaderParameters(ReadingMode.Deferred)
                {
                    AssemblyResolver = this,
                    ReadWrite = false,
                    InMemory = false
                });
            }
            catch (AssemblyResolutionException e)
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies().First(p => p.GetName().Name == name.Name);
                assembly = AssemblyDefinition.ReadAssembly(asm.Location,
                    new ReaderParameters(ReadingMode.Deferred)
                    {
                        AssemblyResolver = this,
                        ReadWrite = false,
                        InMemory = false
                    });
            }

            _cache[name.FullName] = assembly;
            return assembly;
        }

        public void Release(string fullName)
        {
            if(_cache.TryGetValue(fullName, out var assembly))
            {
                assembly.Dispose();
                _cache.Remove(fullName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            foreach(var assembly in _cache.Values)
                assembly.Dispose();

            _cache.Clear();
            base.Dispose(disposing);
        }

        public int Compare(Processor.TargetInfo x, Processor.TargetInfo y)
        {
            if(!_cache.ContainsKey(x.AssemblyName)) return -1;
            if(!_cache.ContainsKey(y.AssemblyName)) return +1;

            if(!x.OpenAssemblyDefinition.MainModule.HasAssemblyReferences) return -1;
            if(!y.OpenAssemblyDefinition.MainModule.HasAssemblyReferences) return +1;

            if(x.OpenAssemblyDefinition.MainModule.AssemblyReferences.Any(p => p.FullName == y.AssemblyName))
            {
                return +1;
            }

            if(y.OpenAssemblyDefinition.MainModule.AssemblyReferences.Any(p => p.FullName == x.AssemblyName))
            {
                return -1;
            }

            return 0;
        }
    }
}