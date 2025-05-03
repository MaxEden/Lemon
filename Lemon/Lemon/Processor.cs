using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lemon.Lemon;
using Lemon.Tools;
using Lemon.Tools.Weavers;
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
        private readonly HashSet<FileInfo> _dlls = new HashSet<FileInfo>();
        private readonly Dictionary<string, TargetInfo> _targetInfos = new Dictionary<string, TargetInfo>();

        private LemonAssemblyResolver _resolver;

        // static Processor()
        // {
        //     AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        // }

        public bool AutoBackupAndRestore { get; set; }
        public bool ReadDebugSymbols { get; set; } = true;

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

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;

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

        public void AddTargetAssemblies(IList<string> filePaths)
        {
            AddTargetAssemblies(filePaths.Select(p => new FileInfo(p)).ToArray());
        }

        public void AddTargetAssemblies(params FileInfo[] files)
        {
            foreach (var file in files)
            {
                if (_targetInfos.TryGetValue(file.FullName, out var targetInfo)) continue;

                var readerParameters = new ReaderParameters();
                var writerParameters = new WriterParameters();

                readerParameters.AssemblyResolver = _resolver;

                string symbolPath = null;

                if (ReadDebugSymbols)
                {
                    if (Searcher.SearchDebugSymbols(file, out var symbolType, out symbolPath))
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
                }
                else
                {
                    readerParameters.ReadSymbols = false;
                    writerParameters.WriteSymbols = false;
                    readerParameters.SymbolReaderProvider = null;
                    writerParameters.SymbolWriterProvider = null;
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

        public void FreeAssembliesAndDispose()
        {
            var targets = _targetInfos.Values.ToList();
            targets.Sort(_resolver);

            foreach (var target in targets)
            {
                _resolver.Release(target.AssemblyName);
            }

            foreach (var target in targets)
            {
                target.OpenAssemblyDefinition.Dispose();
            }

            Dispose();
        }

        public void WriteAssembliesAndDispose()
        {
            var targets = _targetInfos.Values.ToList();
            targets.Sort(_resolver);

            foreach (var target in targets)
            {
                //target.ReaderParameters = null;

                Conventions.AddStamp(target.OpenAssemblyDefinition);

                _log($"Writing dll to {target.AssemblyPath}");

                _resolver.Release(target.AssemblyName);
                target.OpenAssemblyDefinition.Write(target.AssemblyPath, target.WriterParameters);

                //target.OpenAssemblyDefinition.Write(target.AssemblyPath, target.WriterParameters);
                //target.OpenAssemblyDefinition.Dispose();
                //TargetInfos.Remove(target.FileInfo.FullName);
            }

            foreach (var target in targets)
            {
                target.OpenAssemblyDefinition.Dispose();
            }

            Dispose();
        }

        public void Dispose()
        {
            if (_resolver != null)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainAssemblyResolve;

                _resolver.Dispose();
                _resolver = null;
                Cache.Instance.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public LemonAssemblyResolver Resolver => _resolver;

        private List<AssemblyDefinition> Read()
        {
            Cache.Instance.Clear();
            PrepareResolver();
            if (AutoBackupAndRestore) RestoreDlls();

            _log("reading...");
            foreach (var pair in _targetInfos.ToArray())
            {
                var target = pair.Value;

                _log(target.AssemblyPath);
                // if (!AutoBackupAndRestore && Conventions.IsStampedDll(target.FileInfo))
                // {
                //     _log("Already weaved: " + target.Name);
                //     continue;
                // }

                try
                {

                    if (!ReadDebugSymbols)
                    {
                        target.ReaderParameters.ReadSymbols = false;
                        target.WriterParameters.WriteSymbols = false;
                        target.ReaderParameters.SymbolReaderProvider = null;
                        target.WriterParameters.SymbolWriterProvider = null;
                    }

                    var assembly = AssemblyDefinition.ReadAssembly(target.AssemblyPath, target.ReaderParameters);
                    if (assembly == null)
                    {
                        _log("Couldn't load: " + target.Name);
                        continue;
                    }

                    if (assembly.MainModule.GetType("Weaver", "Stamp") != null)
                    {
                        _log("Already weaved: " + target.Name);
                        assembly.Dispose();
                        continue;
                    }

                    target.OpenAssemblyDefinition = assembly;
                    target.AssemblyFullName = assembly.FullName;
                    target.AssemblyName = assembly.Name.Name;

                    _resolver.AddRead(assembly);
                }
                catch (System.BadImageFormatException)
                {
                    _log("not managed dll. skipping." + target.Name);
                    _targetInfos.Remove(pair.Key);
                    continue;
                }
            }

            var values = _targetInfos.Values.Select(p => p.OpenAssemblyDefinition).ToList();
            return values;
        }

        public void Process(Action<List<AssemblyDefinition>, Action<string>> weaveAction, string name = null)
        {
            _log("===WEAVING===");

            var values = Read();
            try
            {
                name ??= weaveAction.Method.DeclaringType.Name + "." + weaveAction.Method.Name;
                _log($"Weaving with {name}");
                weaveAction(values, _log);
            }
            catch (Exception exception)
            {
                _log("EXCEPTION " + exception);
                Dispose();
                throw;
            }
        }

        public class TargetInfo
        {
            public FileInfo FileInfo;
            public string AssemblyPath => FileInfo.FullName;
            public string Name => FileInfo.Name;
            public string AssemblyName { get; internal set; }
            public string AssemblyFullName { get; internal set; }

            public string SymbolPath;
            public ReaderParameters ReaderParameters;
            public WriterParameters WriterParameters;

            public AssemblyDefinition OpenAssemblyDefinition;
        }

        public void AddLookUpDirectories(params string[] directories)
        {
            Searcher.SearchDlls(out var dlls, out var dllDirs, directories);
            foreach (var dir in dllDirs)
            {
                _searchDirectories.Add(dir.FullName);
            }

            _dlls.AddRange(dlls);
        }

        public void SearchTargets(Func<FileInfo, bool> isTarget)
        {
            AddTargetAssemblies(_dlls.Where(isTarget).ToArray());
        }
    }
}