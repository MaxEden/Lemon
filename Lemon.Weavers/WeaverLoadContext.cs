using System.Reflection;
using System.Runtime.Loader;

namespace Lemon.Lemon.Weavers;

internal class WeaverLoadContext : AssemblyLoadContext
{
    private readonly DirectoryInfo[] _lookupDirectories;
    private readonly Dictionary<string, Assembly> _defaultAsms;

    public WeaverLoadContext(DirectoryInfo[] lookupDirectories, Assembly[] defaultAssemblies) : base(true)
    {
        _lookupDirectories = lookupDirectories;
        _defaultAsms = defaultAssemblies.ToDictionary(p => p.GetName().Name, p => p);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        if (_defaultAsms.TryGetValue(assemblyName.Name, out var defaultAsm))
        {
            return defaultAsm;
        }
        
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName() == assemblyName)
            {
                return assembly;
            }
        }

        foreach (var directoryInfo in _lookupDirectories)
        {
            foreach (var file in directoryInfo.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly))
            {
                var shortName = Path.GetFileNameWithoutExtension(file.Name);
                if (shortName == assemblyName.Name)
                {
                    return LoadFromAssemblyPath(file.FullName);
                }
            }
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        foreach (var directoryInfo in _lookupDirectories)
        {
            foreach (var file in directoryInfo.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly))
            {
                var shortName = Path.GetFileNameWithoutExtension(file.Name);
                if (shortName == unmanagedDllName)
                {
                    return LoadUnmanagedDllFromPath(file.FullName);
                }
            }
        }

        return IntPtr.Zero;
    }
}