using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileDupeFinder
{
    public class Analyzer
    {
        private readonly string _csvFile;
        public Analyzer(string csvFile)
        {
            _csvFile = csvFile;
        }

        public void Analyze()
        {
            if (!File.Exists(_csvFile))
            {
                Console.Write($"{_csvFile} is not exist");
                return;
            }
            var csv = ReadCsvToMyFileInfo(_csvFile);
            FindDupesWithDiffNames(csv, _csvFile);
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

            var ignoreFoldersEndsWith = "_files".Split(','); // there are 65 files
            myFileInfos = myFileInfos.Where(x =>
                !badExtentionsForDelete.Contains(x.Extension.ToLower())
                && !ignoreFoldersEndsWith.Any(y => x.Folder.ToLower().EndsWith(y))
                && !(x.Name.StartsWith(".") &&
                     string.Equals(x.Extension.ToLower(), "jpg",
                         StringComparison.CurrentCultureIgnoreCase))).ToList();
            Console.WriteLine($"file count after unwanted extensions and folders removal = {myFileInfos.Count}");

            var grouped = myFileInfos.GroupBy(x => x.Md5).OrderByDescending(x => x.Count()).ToList();
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
            var distinctFiles = groupedSortedByFolderName.Select(x => x.Last()).Select(x => $"{x.Folder}\\{x.Name}")
                .ToList();
            var distinctFilesFileName = $"{fileNameRoot}_distinctFiles.txt";
            WriteFile(distinctFilesFileName, distinctFiles);
            Console.WriteLine(
                $"{distinctFiles.Count} distinct files found and they are saved as {distinctFilesFileName}");
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
