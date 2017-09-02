using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileDupeFinder
{
    public class Merge
    {
        private readonly string _fileList, _tagetFolder;
        private readonly List<string> _errors = new List<string>();
        private readonly bool _move;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileList"></param>
        /// <param name="tagetFolder"></param>
        /// <param name="move">set true to Move or false to copy</param>
        public Merge(string fileList, string tagetFolder, bool move)
        {
            _fileList = fileList;
            _tagetFolder = tagetFolder.TrimEnd('\\');
            _move = move;
        }

        public void Run()
        {
            if (!File.Exists(_fileList))
            {
                Console.Write($"{_fileList} is not exist");
                return;
            }
            try
            {
                if (!Directory.Exists(_tagetFolder))
                    Directory.CreateDirectory(_tagetFolder);
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed to create directory {_tagetFolder}, error:\n{e.Message}");
                return;
            }

            var sourceFiles = ReadMyListFile();
            // var fileExist = sourceFiles.Where(x => File.Exists($"{x.Folder}\\{x.Name}")).Select(x=>$"{x.Folder}\\{x.Name}").ToArray();
            //var fileNotExist = sourceFiles.Where(x => !File.Exists($"{x.Folder}\\{x.Name}")).Select(x=>$"{x.Folder}\\{x.Name}").ToArray();

            sourceFiles.ForEach(FileMove);
            if (_errors.Count <= 0) return;
            var fileName = $"{_fileList.Substring(0, _fileList.Length - 4)}_errors.txt";
            using (var wr = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                _errors.ForEach(x => wr.WriteLine(x));
                wr.Close();
            }
        }

        private void FileMove(MyFileInfo mi)
        {
            try
            {
                var file = $"{mi.Folder}\\{mi.Name}";
                if (!File.Exists(file))
                {
                    Console.WriteLine($"{file} does not exist");
                    return;
                }
                Console.Write($"\rmoving {file} ...");
                if (!Directory.Exists(mi.TargetFolder))
                    Directory.CreateDirectory(mi.TargetFolder);
                var targetFile = $"{mi.TargetFolder}\\{mi.Name}";
                if (File.Exists(targetFile))
                {
                    targetFile =
                        $"{Path.GetDirectoryName(targetFile)}\\{Path.GetFileNameWithoutExtension(targetFile)}_{DateTime.Now.Ticks}{Path.GetExtension(targetFile)}";
                }
                if (_move)
                    File.Move(file, targetFile);
                else
                    File.Copy(file, targetFile);
            }
            catch (Exception e)
            {
                var msg = $"failed to move {mi.Name} from {mi.Folder} to {mi.TargetFolder}\n{e.Message}";
                _errors.Add(msg);
                Console.WriteLine(msg);
            }
        }

        private List<MyFileInfo> ReadMyListFile()
        {
            var myFileInfos = new List<MyFileInfo>();
            var sr = new StreamReader(_fileList, Encoding.UTF8);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                var cells = line.Split('\t');
                myFileInfos.Add(new MyFileInfo
                {
                    Folder = cells[0],
                    Name = cells[1],
                    TargetFolder = GetTargetFolder(cells[0])
                });
            }
            return myFileInfos;
        }

        private string GetTargetFolder(string sourceFolder)
        {
            var index = sourceFolder.IndexOf("\\", 4, StringComparison.Ordinal);
            var strippedSourceFolderr =
                _tagetFolder + (index == -1 ? "" : sourceFolder.Substring(index).ToLower());
            return strippedSourceFolderr;
        }
    }
}
