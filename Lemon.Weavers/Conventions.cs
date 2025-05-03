using Lemon.Tools;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lemon.Lemon
{
    internal class Conventions
    {
        public static DllType GetDllType(FileInfo fileInfo)
        {
            if (!fileInfo.Exists) throw new FileNotFoundException(fileInfo.Name);
            if (fileInfo.Name.EndsWith(".Weaver.dll")) return DllType.Weaver;
            if (fileInfo.Name.EndsWith("_.dll")) return DllType.Backup;
            if (fileInfo.Name.EndsWith(".orig")) return DllType.Backup;

            AssemblyDefinition assembly = null;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(
                    fileInfo.FullName, new ReaderParameters
                    {
                        ReadSymbols = false,
                        ReadingMode = ReadingMode.Deferred,
                        ReadWrite = false,
                        InMemory = false,
                    });

                if (assembly.MainModule.GetType(Stamps.Namespace, Stamps.Name) != null) return DllType.Stamped;
                if (assembly.HasAttribute("LemonWeaveMeAttribute") || assembly.HasAttribute("LemonWeaveMe")) return DllType.Target;
            }
            catch (BadImageFormatException)
            {
                return DllType.None;
            }
            finally
            {
                assembly?.Dispose();
            }

            return DllType.None;
        }
    }

    public enum DllType
    {
        None,
        Stamped,
        Weaver,
        Target,
        Backup
    }
}
