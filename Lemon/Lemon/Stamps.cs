using Mono.Cecil;

namespace Lemon.Lemon
{
    public class Stamps
    {
        public static void AddStamp(AssemblyDefinition assemblyDefinition)
        {
            assemblyDefinition.MainModule.Types
                      .Add(
                          new TypeDefinition(
                              Namespace,
                              Name,
                              TypeAttributes.Abstract,
                              assemblyDefinition.MainModule.TypeSystem.Object));
        }

        public const string Namespace = "LemonWeaver";
        public const string Name = "Stamp";
    }
}
