using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDupeFinder
{
    public class Merge
    {
        private readonly string _fileList, _tagetFolder;

        public Merge(string fileList, string tagetFolder)
        {
            _fileList = fileList;
            _tagetFolder = tagetFolder;
        }

        public void Run()
        {
            if (!File.Exists(_fileList))
            {
                Console.Write($"{_fileList} is not exist");
                return;
            }
            var sourceFiles = ReadMyListFile();


        }

        private string GetTargetFolder(string path)
        {
            var index = path.IndexOf("\\", 4, StringComparison.Ordinal);
            return index == -1 ? "." : path.Substring(index).ToLower();
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
                    Name = cells[1]
                });
            }
            return myFileInfos;
        }
    }
}
