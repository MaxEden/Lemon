using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lemon.Tools;
using Mono.Cecil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Lemon
{
    public class Processor : IDisposable
    {
        private readonly Action<string> _log;
        private readonly HashSet<string> _searchDirectories = new HashSet<string>();
        private readonly Dictionary<string, TargetInfo> _targetInfos = new Dictionary<string, TargetInfo>();

        private LemonAssemblyResolver _resolver;
        private List<FileInfo> _targetDlls;

        static Processor()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        }

        public bool AutoBackupAndRestore { get; set; }

        private static Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var domain = (AppDomain)sender;
            foreach (var assembly in domain.GetAssemblies())
            {
                if (assembly.FullName == args.Name)
                {
                    return assembly;
                }
            }

            return null;
        }

        public Processor(Action<string> log)
        {
            _log = log;
        }

        private void PrepareResolver()
        {
            if (_resolver != null)
            {
                _resolver.Dispose();
                _resolver = null;
            }

            _resolver = new LemonAssemblyResolver();
            _resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
            foreach (var info in _searchDirectories)
            {
                _resolver.AddSearchDirectory(info);
            }

            foreach (var target in _targetInfos.Values)
            {
                target.ReaderParameters.AssemblyResolver = _resolver;
            }
        }

        public void TargetAssemblies(params string[] filePaths)
        {
            TargetAssemblies(filePaths.Select(p => new FileInfo(p)).ToList());
        }

        public void TargetAssemblies(List<FileInfo> files)
        {
            foreach (var file in files)
            {
                if (_targetInfos.TryGetValue(file.FullName, out var targetInfo)) continue;

                var readerParameters = new ReaderParameters();
                var writerParameters = new WriterParameters();

                readerParameters.AssemblyResolver = _resolver;

                if (Searcher.SearchDebugSymbols(file, out var symbolType, out var symbolPath))
                {
                    if (symbolType == SymbolType.Mdb)
                    {
                        readerParameters.SymbolReaderProvider = new MdbReaderProvider();
                        writerParameters.SymbolWriterProvider = new MdbWriterProvider();
                    }

                    if (symbolType == SymbolType.Pdb)
                    {
                        readerParameters.SymbolReaderProvider = new PdbReaderProvider();
                        writerParameters.SymbolWriterProvider = new PdbWriterProvider();
                    }

                    readerParameters.ReadSymbols = true;
                    writerParameters.WriteSymbols = true;
                }

                readerParameters.InMemory = true;
                readerParameters.ReadingMode = ReadingMode.Immediate;
                readerParameters.ReadWrite = false;

                targetInfo = new TargetInfo()
                {
                    FileInfo = file,
                    SymbolPath = symbolPath,
                    WriterParameters = writerParameters,
                    ReaderParameters = readerParameters
                };

                _targetInfos.Add(file.FullName, targetInfo);
            }
        }

        public void RestoreDlls()
        {
            Backuper.RestoreDlls(_targetInfos.Values);
        }

        public void WriteAssemblies()
        {
            var targets = _targetInfos.Values.ToList();
            targets.Sort(_resolver);

            foreach (var target in targets)
            {
                //target.ReaderParameters = null;
                target.OpenAssemblyDefinition.MainModule.Types
                      .Add(
                          new TypeDefinition(
                              "Weaver",
                              "Stamp",
                              TypeAttributes.Abstract,
                              target.OpenAssemblyDefinition.MainModule.TypeSystem.Object));

                _log($"Writing dll to {target.AssemblyPath}");

                _resolver.Release(target.AssemblyName);
                target.OpenAssemblyDefinition.Write(target.AssemblyPath, target.WriterParameters);
                target.OpenAssemblyDefinition.Dispose();
                //TargetInfos.Remove(target.FileInfo.FullName);
            }

            Dispose();
        }

        public void Dispose()
        {
            if (_resolver != null)
            {
                _resolver.Dispose();
                _resolver = null;
                Cache.Instance.Clear();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
        }

        public List<AssemblyDefinition> Read()
        {
            Cache.Instance.Clear();
            PrepareResolver();
            if (AutoBackupAndRestore) RestoreDlls();

            _log("reading...");
            foreach (var target in _targetInfos.Values)
            {
                _log(target.AssemblyPath);
                if (!AutoBackupAndRestore && Searcher.IsStampedDll(target.FileInfo))
                {
                    _log("Already weaved: " + target.Name);
                    continue;
                }
                var assembly = AssemblyDefinition.ReadAssembly(target.AssemblyPath, target.ReaderParameters);
                if(assembly == null) continue;
                
                target.OpenAssemblyDefinition = assembly;
                target.AssemblyName = assembly.FullName;
            }

            var values = _targetInfos.Values.Select(p => p.OpenAssemblyDefinition).ToList();
            return values;
        }

        public void Process(IEnumerable<object> weavers)
        {
            _log("===WEAVING===");

            var values = Read();

            foreach (var weaver in weavers)
            {
                var weaverType = weaver.GetType();
                var weaverName = weaverType.Assembly.GetName().Name;
                var weaverMethod = weaverType.GetMethod(nameof(IWeaver.Weave));
                _log($"Weaving with {weaverName} --------------------------------------");
                weaverMethod.Invoke(weaver, new object[] { values, _log });

                _log($"Weaving with {weaverName} is done ------------------------------");
            }
        }

        public void Process(Action<List<AssemblyDefinition>, Action<string>> weaveAction)
        {
            _log("===WEAVING===");

            var values = Read();
            try
            {
                weaveAction(values, _log);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Process(Action<List<AssemblyDefinition>> weaveAction)
        {
            _log("===WEAVING===");

            var values = Read();
            weaveAction(values);
        }

        public void AddSearchDirectory(DirectoryInfo searchDirectory)
        {
            _searchDirectories.Add(searchDirectory.FullName);
        }

        public class TargetInfo
        {
            public FileInfo FileInfo;
            public string AssemblyPath => FileInfo.FullName;
            public string Name => FileInfo.Name;
            public string AssemblyName { get; set; }

            public string SymbolPath;
            public ReaderParameters ReaderParameters;
            public WriterParameters WriterParameters;

            public AssemblyDefinition OpenAssemblyDefinition;
        }

        public void Search(string[] directories)
        {
            if (_targetDlls == null)
            {
                Searcher.SearchDlls(out var dlls, out var dllDirs, directories);
                _targetDlls = Searcher.SearchTargetDlls(dlls);
                dllDirs.ForEach(AddSearchDirectory);
                TargetAssemblies(_targetDlls);
            }
        }
    }
}