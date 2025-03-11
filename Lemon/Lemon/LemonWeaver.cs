using System;
using System.Collections.Generic;
using System.IO;
using Lemon.Tools;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon
{
    public class LemonWeaver
    {
        private readonly Processor _processor;
        private readonly Action<string> _log;

        public LemonWeaver(Action<string> log)
        {
            this._log = log;
            _processor = new Processor(log);
        }

        public void Process(string[] directories, Func<FileInfo, bool> isTarget, params IWeaver[] weavers)
        {
            using (new Measurer("Weaving", _log))
            {
                _processor.AddDirectories(directories);
                _processor.SearchTargets(isTarget);
                _processor.Process(weavers);
                _processor.WriteAssembliesAndDispose();
            }
        }

        public void Read(string[] directories, Func<FileInfo, bool> isTarget, Action<List<AssemblyDefinition>, Action<string>> readCall)
        {
            using (new Measurer("Reading", _log))
            {
                _processor.AddDirectories(directories);
                _processor.SearchTargets(isTarget);
                var asmDefs = _processor.Read();
                readCall(asmDefs, _log);
                _processor.Dispose();
            }
        }

        public void Restore(string[] directories)
        {
            using (new Measurer("Restore", _log))
            {
                _processor.AddDirectories(directories);
                _processor.RestoreDlls();
            }
        }


    }

    public struct ReadCall
    {
        public string name;
        public List<AssemblyDefinition> asmdefs;
        public Action<string> log;
    }
}