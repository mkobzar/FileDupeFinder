using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileDupeFinder
{
    public class Analyzer
    {
        private readonly string[] _csvFiles;

        public Analyzer(string[] csvFiles)
        {
            _csvFiles = csvFiles;
        }

        public void Analyze()
        {
                var myFileInfos = new List<MyFileInfo>();
            foreach (var csvFile in _csvFiles)
            {
                if(string.Equals(csvFile, "/a", StringComparison.CurrentCultureIgnoreCase))
                    continue;
                if (!File.Exists(csvFile))
                    Console.Write($"{csvFile} is not exist");
                var csv = ReadCsvToMyFileInfo(csvFile);
                if(csv!=null && csv.Count>0)
                    myFileInfos.AddRange(csv);
            }
            if (myFileInfos.Count==0)
            {
                Console.Write($"no valid data found in those files\n{string.Join("\n", _csvFiles)}");
                return;
            }
            FindDupesWithDiffNames(myFileInfos, _csvFiles[1]);
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

        private void FindDupesWithDiffNames(List<MyFileInfo> myFileInfos, string csvFile)
        {
            /*
            1.) "*_files" folders: exclude them and their content from csv, because they are as whole - are html sources 
            2.) ignore *_files folders 
            3.) any files with same md5 and not same size? abort!

             */
            var extensions = myFileInfos.Select(x => x.Extension.ToLower()).Distinct().OrderBy(x => x).ToArray();
            Console.WriteLine($"FYI: all Distinct extensions are:\n{string.Join(", ", extensions)}");

            // csv filename without extension:
            var fileNameRoot = csvFile.Substring(0, csvFile.Length - 4);

            // delete Candidates based on unwanted Extensions
            var badExtentionsForDelete = "ctg,db,ini,tmp,bup,ifo".Split(',');
            var deleteCandidates = myFileInfos.Where(x =>
                    badExtentionsForDelete.Contains(x.Extension.ToLower()) ||
                    x.Name.StartsWith(".") &&
                    string.Equals(x.Extension.ToLower(), "jpg",
                        StringComparison.CurrentCultureIgnoreCase))
                .Select(x => $"{x.Folder}\\{x.Name}").ToList();
            WriteFile($"{fileNameRoot}_deleteCandidatesExtentions.txt", deleteCandidates);
            Console.WriteLine($"initial file count = {myFileInfos.Count}");

            var ignoreFoldersEndsWith = "_files".Split(',');
            myFileInfos = myFileInfos.Where(x =>
                !badExtentionsForDelete.Contains(x.Extension.ToLower())
                && !ignoreFoldersEndsWith.Any(y => x.Folder.ToLower().EndsWith(y))
                && !(x.Name.StartsWith(".") &&
                     string.Equals(x.Extension.ToLower(), "jpg",
                         StringComparison.CurrentCultureIgnoreCase))).ToList();
            Console.WriteLine($"file count after unwanted extensions and folders removal = {myFileInfos.Count}");

            var grouped = myFileInfos.GroupBy(x => x.Md5).OrderByDescending(x => x.Count()).ToList();

            //// filter out f*.jpg files 
            //var mi = grouped
            //    .Select(gr => new {gr, gr0 = gr.Where(x => !x.Folder.ToLower().Contains("recovered")).ToList()})
            //    .Select(t => t.gr0.Any() ? t.gr0 : t.gr.ToList())
            //    .Select(gr1 => new
            //    {
            //        gr1,
            //        gr2 = gr1.Where(x =>
            //                !(x.Name.StartsWith("f") &&
            //                  string.Equals(x.Extension, "jpg", StringComparison.CurrentCultureIgnoreCase)))
            //            .ToList()
            //    })
            //    .Select(t => t.gr2.Any() ? t.gr2 : t.gr1)
            //    .Select(gr3 => gr3.OrderBy(x => x.Folder).Last()).ToList();

            //WriteFile($"{fileNameRoot}_distinct9.txt",
            //    mi.Select(lastItem => $"{lastItem.Folder}\t{lastItem.Name}").ToList());

            var singleFileInOneGroup = grouped.Where(x => x.Count() == 1).ToList();
            Console.WriteLine($"singleFileInOneGroup = {singleFileInOneGroup.Count}");
            var moreThenOneSizeInOneGroup = grouped.Where(x => x.Select(y => y.Size).Distinct().Count() > 1)
                .Select(x => x.Key).ToArray();
            if (moreThenOneSizeInOneGroup.Any())
            {
                var md5S = string.Join(", ", moreThenOneSizeInOneGroup);
                Console.WriteLine(
                    $"there are {moreThenOneSizeInOneGroup.Length} group(s) where files identified with same md5 and different sizes\nthis should never happen. affected md5s:\n{md5S}\nfurther analyzes is questionable\naborting...");
                return;
            }

            var groupedSortedByFolderName = grouped.Select(y => y.OrderBy(z => z.Folder)).ToList();
            var distinctObjects = groupedSortedByFolderName.Select(x => x.Last()).ToList();
            //var distinctObjects = groupedSortedByFolderName.Select(x => x.First()).ToList();
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
