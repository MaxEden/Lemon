using Lemon.Lemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lemon
{
    public class Backuper
    {
        public static string GetBackupPath(string path)
        {
            return path + ".orig";
        }

        public static string GetBackupPath2(string path)
        {
            var name = path
                       .Replace('\\', '_')
                       .Replace(' ', '_')
                       .Replace(':', '_');

            return "BackupDlls/" + name + ".orig";
        }
        
        public static void RestoreDlls(IEnumerable<Processor.TargetInfo> targetInfos)
        {
            Parallel.ForEach(targetInfos, target =>
            {
                //TODO Console.WriteLine(target.FileInfo.FullName);

                if (!Conventions.IsStampedDll(target.FileInfo))
                {
                    FileCopy(target.AssemblyPath, GetBackupPath(target.AssemblyPath));
                    FileCopy(target.AssemblyPath, GetBackupPath2(target.AssemblyPath));

                    if (target.SymbolPath != null)
                    {
                        FileCopy(target.SymbolPath, GetBackupPath(target.SymbolPath));
                        FileCopy(target.SymbolPath, GetBackupPath2(target.SymbolPath));
                    }
                }
                else
                {
                    var backupPath = GetBackup(target.AssemblyPath);
                    FileCopy(backupPath, target.AssemblyPath);
                    if (target.SymbolPath != null)
                        FileCopy(GetBackup(target.SymbolPath), target.SymbolPath);
                }
            });

            string GetBackup(string origPath)
            {
                var path = GetBackupPath(origPath);
                if (!File.Exists(path))
                {
                    path = GetBackupPath2(origPath);
                }

                if (!File.Exists(path))
                {
                    throw new ArgumentException($"Cant find original {origPath}. Dll is already weaved!");
                }

                return path;
            }

            void FileCopy(string from, string to)
            {
                var dir = Path.GetDirectoryName(GetBackupPath(to));
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.Copy(from, to, true);
            }
        }
    }
}