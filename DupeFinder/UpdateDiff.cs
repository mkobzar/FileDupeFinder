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


        public  void Run()
        {
            if (string.IsNullOrEmpty(_csvFileA) || string.IsNullOrEmpty(_csvFileB))
                return;


            // csv filename without extension:
            var myFileInfoStore0 = ReadCsvToMyFileInfo(_csvFileA);
            var myFileInfoAddition = ReadCsvToMyFileInfo(_csvFileB);

            var fileNameRoot = _csvFileA.Substring(0, _csvFileA.Length - 4);

            // delete Candidates based on unwanted Extensions
            var badExtentionsForDelete = "ctg,db,ini,tmp,bup,ifo".Split(',');
            Console.WriteLine($"initial file count = {myFileInfoStore0.Count}");
            var myFileInfoStore = myFileInfoStore0.Where(x =>
                !badExtentionsForDelete.Contains(x.Extension.ToLower())).ToList();
            Console.WriteLine($"file count after unwanted extensions = {myFileInfoStore.Count}");

            var groupedStore = myFileInfoStore.GroupBy(x => x.Md5).OrderByDescending(x => x.Count()).ToList();

            var singleFileInOneGroup = groupedStore.Where(x => x.Count() == 1).ToList();
            Console.WriteLine($"single File In groupedStore= {singleFileInOneGroup.Count}");
            if (groupedStore.Count != myFileInfoStore.Count)
            {
                Console.WriteLine(
                    $"groupedStore.Count[{groupedStore.Count}] != myFileInfoStore.Count [{myFileInfoStore.Count}]");

            }

            var groupedSortedByFolderName = groupedStore.Select(y => y.OrderBy(z => z.Folder)).ToList();
            var distinctObjects = groupedSortedByFolderName.Select(x => x.Last()).ToList();
            var objectsToBeDeleted = myFileInfoStore0.Except(distinctObjects);
            // TODO start from here. 
            // build files to be deleted in myFileInfoStore0

            var distinctFilesNames = distinctObjects.Select(x => $"{x.Folder}\t{x.Name}").ToList();
            var spaceNeeded = distinctObjects.Select(x => x.Size).Sum() / 1024 / 1024 / 1024;
            var distinctFilesFileName = $"{fileNameRoot}_distinctFiles.txt";
            WriteFile(distinctFilesFileName, distinctFilesNames);
            Console.WriteLine(
                $"{distinctFilesNames.Count} distinct files found with total size = {spaceNeeded}GB, and file list saved as {distinctFilesFileName}");
            // var distinctrecoveredLessFolders = recoveredLessFileInfos.Select(x => StripParentFolder(x.Folder)).Distinct().ToList();
            // WriteFile($"{fileNameRoot}_distinctrecoveredLessFolders.txt", distinctrecoveredLessFolders);
            var distinctFolders = distinctObjects.Select(x => StripParentFolder(x.Folder)).Distinct().ToList();
            WriteFile($"{fileNameRoot}_distinctFolders.txt", distinctFolders);

            // save list of very large files 
            var largeObjects = distinctObjects.Where(x => x.Size > 100000000).OrderByDescending(x => x.Size).ToList();
            var largeFiles = largeObjects.Select(x => $"{x.Folder}\\{x.Name}\t{x.Size}").ToList();
            var largeFilesFileName = $"{fileNameRoot}_largeFiles.txt";
            Console.WriteLine(
                $"found {largeFiles.Count} files where size > 100MB" +
                $"\ntotal size of those files is {largeObjects.Select(x => x.Size).Sum() / 1024 / 1024 / 1024}GB" +
                $"\nand this list was saved as {largeFilesFileName}");
            WriteFile(largeFilesFileName, largeFiles);
        }

        private string StripParentFolder(string path)
        {
            var index = path.IndexOf("\\", 4, StringComparison.Ordinal);
            return index == -1 ? "." : path.Substring(index).ToLower();
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
