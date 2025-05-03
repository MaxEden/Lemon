using Lemon.Tools.Weavers;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using Lemon.Weavers;
using Mono.Cecil.Cil;

namespace Lemon.Lemon.Weavers
{
    internal class WeaverProcessor
    {
        private HashSet<DirectoryInfo> _directories = new();
        private HashSet<FileInfo> _weaverFiles = new();

        private readonly Action<string> _log;
        private readonly Processor _processor;
        
        public WeaverProcessor(Action<string> log)
        {
            _log = log;
            _processor = new Processor(_log);
        }

        public void AddLookupDirectories(params string[] directoryPaths)
        {
            _processor.AddLookUpDirectories(directoryPaths);

            foreach (var directoryPath in directoryPaths)
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                foreach (var fileInfo in dirInfo.GetFiles("*.dll", SearchOption.AllDirectories))
                {
                    var type = Conventions.GetDllType(fileInfo);
                    if (type == DllType.Target)
                    {
                        _processor.AddTargetAssemblies(fileInfo);
                    }
                    else if (type == DllType.Weaver)
                    {
                        _weaverFiles.Add(fileInfo);
                    }

                    _directories.Add(fileInfo.Directory!);
                }
            }
        }

        public bool BackupDlls { get; set; } = true;
        public bool ProcessDebugSymbols { get; set; } = true;
        public void Process()
        {
            _log("===WEAVING===");

            var dirs = _directories.ToArray();

            if (BackupDlls)
            {
                Backuper.BackupAndRestoreDlls(dirs);
            }

            _processor.ProcessDebugSymbols = ProcessDebugSymbols;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<WeaverLoadContext> contexts = new();
            foreach (var weaverFile in _weaverFiles)
            {
                var context = new WeaverLoadContext(dirs, assemblies);
                contexts.Add(context);
                var asm = context.LoadFromAssemblyPath(weaverFile.FullName);
                var types = asm.GetTypes();
                var weaverName = asm.GetName().Name;
                IWeaver weaver = null;

                foreach (var type in types)
                {
                    if (type.IsAssignableTo(typeof(IWeaver)))
                    {
                        weaver = (IWeaver)Activator.CreateInstance(type);

                        if (weaver == null) throw new InvalidOperationException($"Cant create {weaverName} weaver");
                       
                        var weaverMethod = type.GetMethod(nameof(IWeaver.Weave));

                        _log($"Weaving with {weaverName} --------------------------------------");
                        _processor.Process(p =>
                        {
                            weaverMethod.Invoke(weaver, new object[] { p });
                        });
                        _log($"Weaving with {weaverName} is done ------------------------------");
                    }
                }

                if(weaver == null) throw new InvalidOperationException($"Cant create {weaverName} weaver");
            }

            _log("===DONE===");
        }
    }
}