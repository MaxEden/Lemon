using Lemon.Lemon;
using Microsoft.VisualBasic.FileIO;
using SearchOption = System.IO.SearchOption;

namespace Lemon.Weavers
{
    public static class Backuper
    {
        private static string _userDir;

        private static string GetBackupPath(string path)
        {
            return path + ".orig";
        }
        private static string GetBackupPath2(string path)
        {
            var name = path
                       .Replace('\\', '_')
                       .Replace(' ', '_')
                       .Replace(':', '_');

            _userDir ??= Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

            return _userDir+"/.lemon/BackupDlls/" + name + ".orig";
        }
        private static string GetSymbolPath(FileInfo fileInfo)
        {
            return fileInfo.FullName.Substring(0, fileInfo.FullName.Length - 4) + ".pdb";
        }

        public static void BackupAndRestoreDlls(DirectoryInfo[] directories)
        {
            foreach (var directory in directories)
            {
                var dirInfo = directory;
                foreach (var fileInfo in dirInfo.GetFiles("*.dll", SearchOption.AllDirectories))
                {
                    var type = Conventions.GetDllType(fileInfo);
                    
                    if (type == DllType.Stamped)
                    {
                        var backupPath = GetBackup(fileInfo.FullName);
                        FileCopy(backupPath, fileInfo.FullName);
                        var symbolPath = GetSymbolPath(fileInfo);
                        if (File.Exists(symbolPath))
                        {
                            FileCopy(GetBackup(symbolPath), symbolPath);
                        }
                    }
                    else if (type == DllType.Target)
                    {
                        FileCopy(fileInfo.FullName, GetBackupPath(fileInfo.FullName));
                        FileCopy(fileInfo.FullName, GetBackupPath2(fileInfo.FullName));

                        var symbolPath = GetSymbolPath(fileInfo);
                        if (File.Exists(symbolPath))
                        {
                            FileCopy(symbolPath, GetBackupPath(symbolPath));
                            FileCopy(symbolPath, GetBackupPath2(symbolPath));
                        }
                    }
                }
            }
        }

        static string GetBackup(string origPath)
        {
            var path = GetBackupPath(origPath);
            if (!File.Exists(path))
            {
                path = GetBackupPath2(origPath);
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Cant find {origPath} backup");
            }

            return path;
        }

        static void FileCopy(string from, string to)
        {
            var dir = Path.GetDirectoryName(GetBackupPath(to));
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.Copy(from, to, true);
        }
    }
}