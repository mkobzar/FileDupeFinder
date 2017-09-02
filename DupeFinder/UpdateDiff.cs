using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileDupeFinder
{
    public class UpdateDiff
    {
        private readonly string _csvFileA;
        private readonly string _csvFileB;

        public UpdateDiff(string[] args)
        {
            if (args == null || args.Length != 3 || !File.Exists(args[1]) || !File.Exists(args[2]))
            {
                Console.Write("invalid arguments. expected input sample: \n/u c:\\path\\file1.csv c:\\path\\file2.csv");
                return;
            }
            _csvFileA = args[1];
            _csvFileB = args[2];
        }


        public void Run()
        {
            if (string.IsNullOrEmpty(_csvFileA) || string.IsNullOrEmpty(_csvFileB))
                return;
            var fileInfoStore = CsvParse(_csvFileA);
            var fileInfoNew = CsvParse(_csvFileB);
            var md5A = fileInfoStore.Select(x => x.Md5);
            var md5B = fileInfoNew.Select(x => x.Md5);
            var md5New = md5B.Except(md5A).ToList();
            var newForStore = fileInfoNew.Where(x => md5New.Contains(x.Md5)).ToList();
            var newForStoreFiles = newForStore.Select(x => $"{x.Folder}\t{x.Name}").ToList();
            var fileNameRoot = _csvFileA.Substring(0, _csvFileA.Length - 4);
            var newForStoreFilesFileName = $"{fileNameRoot}_newFiles.txt";
            WriteFile(newForStoreFilesFileName, newForStoreFiles);

            var spaceNeeded = newForStore.Select(x => x.Size).Sum() / 1024 / 1024 / 1024;
            Console.WriteLine(
                $"{newForStoreFiles.Count} new files found with total size = {spaceNeeded}GB, and file list saved as {newForStoreFilesFileName}");
        }

        private List<MyFileInfo> CsvParse(string csvFile)
        {
            // csv filename without extension:
            var fileNameRoot = csvFile.Substring(0, _csvFileA.Length - 4);
            var store0 = ReadCsvToMyFileInfo(csvFile);

            var badExtentionsForDelete = "ctg,db,ini,tmp,bup,ifo,lrcat,lrprev".Split(',');
            Console.WriteLine($"{csvFile} initial count = {store0.Count}");
            var fileInfos = store0.Where(x =>
                !badExtentionsForDelete.Contains(x.Extension.ToLower())).ToList();
            Console.WriteLine($"file count after unwanted extensions = {fileInfos.Count}");

            var extensions = fileInfos.Select(x => x.Extension.ToLower()).Distinct().OrderBy(x => x).ToArray();
            Console.WriteLine($"all Distinct extensions are:\n{string.Join(", ", extensions)}");

            var groupedStore = fileInfos.GroupBy(x => x.Md5).OrderByDescending(x => x.Count()).ToList();

            var singleFileInOneGroup = groupedStore.Where(x => x.Count() == 1).ToList();
            Console.WriteLine($"single File.Count In {csvFile} = {singleFileInOneGroup.Count}");
            if (groupedStore.Count != fileInfos.Count)
            {
                Console.WriteLine(
                    $"groupedStore.Count[{groupedStore.Count}] != myFileInfoStore.Count [{fileInfos.Count}]");
            }

            var groupedSortedByFolderName = groupedStore.Select(y => y.OrderBy(z => z.Folder)).ToList();
            var distinctObjects = groupedSortedByFolderName.Select(x => x.First()).ToList();

            var distinct = distinctObjects.Select(x => $"{x.Folder}\t{x.Name}").ToList();
            var distinctFilesFileName = $"{fileNameRoot}_distinct.txt";
            WriteFile(distinctFilesFileName, distinct);
            Console.WriteLine($"distinct file count = {distinctObjects.Count}\n");
            return distinctObjects;
        }


        List<MyFileInfo> ReadCsvToMyFileInfo(string csvFileName)
        {
            var myFileInfos = new List<MyFileInfo>();
            var sr = new StreamReader(csvFileName, Encoding.UTF8);
            sr.ReadLine();
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                var cells = line.Split('\t');
                myFileInfos.Add(new MyFileInfo
                {
                    //path	file	extension	dateTaken	size
                    Folder = cells[0],
                    Name = cells[1],
                    Extension = cells[2],
                    DateTaken = cells[3],
                    Size = Convert.ToInt64(cells[4]),
                    Md5 = cells[5]
                });
            }
            return myFileInfos;
        }

        private void WriteFile(string fileName, List<string> content)
        {
            using (var wr = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                content.ForEach(x => wr.WriteLine(x));
                wr.Close();
            }
        }
    }
}
