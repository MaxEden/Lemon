using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lemon.Tools;
using Mono.Cecil;

namespace Lemon
{
    public static class Searcher
    {
        public static void SearchDlls(out List<FileInfo> files, out List<DirectoryInfo> dirs, params string[] dirPaths)
        {
            files = new List<FileInfo>();
            foreach(var arg in dirPaths)
            {
                if(!Directory.Exists(arg)) throw new ArgumentException($"Directory {arg} not found");
                var dir = new DirectoryInfo(arg);
                var dlls = dir.GetFiles("*.dll", SearchOption.AllDirectories).ToList();
                var exes = dir.GetFiles("*.exe", SearchOption.AllDirectories).ToList();
                files.AddRange(dlls);
                files.AddRange(exes);
            }

            files = files.Select(p => p.FullName).Distinct().Select(p => new FileInfo(p)).ToList();
            dirs = files.Select(p => p.Directory.FullName).Distinct().Select(p => new DirectoryInfo(p)).ToList();
        }

        public static bool SearchDebugSymbols(FileInfo       assemblyPath,
                                              out SymbolType symbolType,
                                              out string     symbolPath)
        {
            symbolType = SymbolType.None;
            symbolPath = null;

            var mdbFile = assemblyPath.FullName + ".mdb";
            var pdbFile = assemblyPath.FullName.Replace(".dll", ".pdb");

            if(File.Exists(mdbFile))
            {
                symbolType = SymbolType.Mdb;
                symbolPath = mdbFile;
                return true;
            }

            if(File.Exists(pdbFile))
            {
                symbolType = SymbolType.Pdb;
                symbolPath = pdbFile;
                return true;
            }

            return false;
        }
        
    }

    public enum SymbolType
    {
        None,
        Mdb,
        Pdb
    }
}