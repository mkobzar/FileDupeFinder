using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FileDupeFinder
{
public  class Analyze
    {
        public Analyze(string csvFile)
        {
            if (!File.Exists(csvFile))
            {
                Console.Write($"{csvFile} is not exist");
                return;
            }
            var csv = ReadCsvToMyFileInfo(csvFile);
            FindDupesWithDiffNames(csv, csvFile);
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

         void FindDupesWithDiffNames(List<MyFileInfo> myFileInfos, string csvFile)
         {
            /*
             * FOLDER UNITS:
             1.) "*_files" folders: exclude them and their content from csv, because they are as hole - are html sources 
               
             ignore *_files folders 

            any files with same md5 and not same size?

             */
            //var extensions = myFileInfos.Select(x => x.Extension.ToLower()).Distinct().ToArray();
            //Console.WriteLine($"all extensions are:\n{string.Join(", ", extensions)}");

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
                // && !ignoreFoldersEndsWith.Contains(x.Folder.ToLower())).ToList();
            Console.WriteLine($"file count after unwanted extensions and folders removal = {myFileInfos.Count}");

             var grouped = myFileInfos.GroupBy(x => x.Md5).OrderByDescending(x => x.Count()).ToList();
            var moreThenOneSizeInOneGroup = grouped.Where(x=>x.Select(y=>y.Size).Distinct().Count()>1);
             var assert = moreThenOneSizeInOneGroup.Any();
            //any files with same md5 and not same size?

            // var folderUnitsContainExtentions = "bup".Split(',');

            //var folderBadPatterns = "".Split(',');
            //var fileBadPatterns = "".Split(',');
            //var myFileInfosWithoutBadExtentions = myFileInfos.Where(x => !badExtentionsForDelete.Contains(x.Extension.ToLower())).ToList();
            //var distinctMd5S = myFileInfosWithoutBadExtentions.Select(x => x.Md5).Distinct().ToArray();
            // var distinct =myFileInfosWithoutBadExtentions.SelectMany(x => distinctMd5S.Where(y => string.Equals(y, x.Md5)).Take(1)).ToList();
            // var distinct2 = distinctMd5S.Select(x => myFileInfosWithoutBadExtentions.First(y => y.Md5 == x)).ToArray();
            //  var d3=    myFileInfosWithoutBadExtentions.SelectMany(x => distinctMd5S.Where(y => string.Equals(y, x.Md5)).Take(1));
            //var grouped = myFileInfosWithoutBadExtentions.GroupBy(x => x.Md5).OrderByDescending(x => x.Count()).ToList();
            var dups = grouped.Where(x => x.Count() > 1).ToList();

            return;

            //var toDelete = new List<MyFileInfo>();
            //var toDeleteWithDiffnames = new List<MyFileInfo>();
            var list = dups.Where(x => x.Count() > 1).ToList();
            var toDelete = list.SelectMany(x =>
            {
                var fr = x.OrderByDescending(y => y.DateTaken).ToList();
                fr.RemoveAt(0);
                return fr;
            })
                .ToList();
            //toDelete.AddRange(collection);

            var listDiffNames = dups.Where(x => x.Count() > 1 && x.Select(n => n.Name).Distinct().Count() > 1).ToList();
            var toDeleteWithDiffnames = listDiffNames.SelectMany(x =>
            {
                var fr = x.OrderByDescending(y => y.Name).ToList();
                //  fr.RemoveAt(0);
                return fr;
            })
                .ToList();
            // toDeleteWithDiffnames.AddRange(collectionDiffNames);

            var dupList = dups.SelectMany(x => x.ToList()).ToList().Select(MyFileInfoToScvLine).ToList();
            var deleteList = toDelete.Select(MyFileInfoToScvLine).ToList();
            var deleteDiffList = toDeleteWithDiffnames.Select(MyFileInfoToScvLine).ToList();
            // var toSave = myFileInfos.Except(toDeleteWithDiffnames).Select(MyFileInfoToScvLine).ToList();
            deleteDiffList.Insert(0, "path\tfile\textension\tdateTaken\tsize\tmd5");
            dupList.Insert(0, "path\tfile\textension\tdateTaken\tsize\tmd5");
            deleteList.Insert(0, "path\tfile\textension\tdateTaken\tsize\tmd5");
            var allDupesFileName = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents\\allDupes3.csv";
            var toDeleteFileName = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents\\toDelete3.csv";
            var toDeleteDiffFileName =
                $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents\\toDeleteDiff3.csv";
            //var toSaveFileName = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents\\toSave.csv";
            WriteFile(allDupesFileName, dupList);
            WriteFile(toDeleteFileName, deleteList);
            //WriteFile(toSaveFileName, toSave);
            WriteFile(toDeleteDiffFileName, deleteDiffList);
        }


        //static void Run3Steps(string rootFolder)
        //{
        //    if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
        //    {
        //        Console.WriteLine("path is missing or not exist");
        //        return;
        //    }
        //    GetFilesFromFolderToScv(rootFolder);
        //    ReadCsvToMyFileInfo(DirectoryContentInfo);
        //    DupeFind();
        //}


        static string MyFileInfoToScvLine(MyFileInfo myFileInfo)
        {
            return
                $"{myFileInfo.Folder}\t{myFileInfo.Name}\t{myFileInfo.Extension}\t{myFileInfo.DateTaken}\t{myFileInfo.Size}\t{myFileInfo.Md5}";
        }

        static void WriteFile(string fileName, List<string> content)
        {
            using (var wr = new StreamWriter(fileName, true, Encoding.UTF8))
            {
                content.ForEach(x => wr.WriteLine(x));
                wr.Close();
            }
        }

        static void DupeFind(List<MyFileInfo> myFileInfos)
        {
            var grouped = myFileInfos.GroupBy(x => x.Md5).ToList();
            var dups = grouped.Where(x => x.Count() > 1).ToList();

            var toDelete = new List<MyFileInfo>();
            var list = dups.Where(x => x.Count() > 1).ToList();
            var collection = list.SelectMany(x =>
            {
                var fr = x.OrderByDescending(y => y.DateTaken).ToList();
                fr.RemoveAt(0);
                return fr;
            })
                .ToList();
            toDelete.AddRange(collection);

            var dupList = dups.SelectMany(x => x.ToList()).ToList().Select(MyFileInfoToScvLine).ToList();
            var deleteList = toDelete.Select(MyFileInfoToScvLine).ToList();
            var toSave = myFileInfos.Except(toDelete).Select(MyFileInfoToScvLine).ToList();
            dupList.Insert(0, "path\tfile\textension\tdateTaken\tsize\tmd5");
            deleteList.Insert(0, "path\tfile\textension\tdateTaken\tsize\tmd5");
            var allDupesFileName = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents\\allDupes.csv";
            var toDeleteFileName = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents\\toDelete.csv";
            var toSaveFileName = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents\\toSave.csv";
            WriteFile(allDupesFileName, dupList);
            WriteFile(toDeleteFileName, deleteList);
            WriteFile(toSaveFileName, toSave);
        }


        //static void GetFilesFromFolderToScv(string rootFolder)
        //{
        //    if (!Directory.Exists(rootFolder))
        //    {
        //        Console.WriteLine("path is missing or not exist");
        //        return;
        //    }
        //    Console.WriteLine($"parsing {rootFolder}");
        //    if (File.Exists(DirectoryContentInfo)) File.Delete(DirectoryContentInfo);

        //    var ff = Directory.GetFiles(rootFolder, "*", SearchOption.AllDirectories);
        //    var dd = Directory.GetDirectories(rootFolder, "*", SearchOption.TopDirectoryOnly);
        //    //.Select(x => new FileInfo(x)).ToList();
        //    var myFileInfos = new List<MyFileInfo>();

        //    WriteFile(DirectoryContentInfo, new List<string> {"path\tfile\textension\tdateTaken\tsize\tmd5"});

        //    foreach (var f in ff)
        //    {
        //        try
        //        {
        //            var fileInfo = new FileInfo(f);
        //            myFileInfos.Add(GetMyFileInfo(fileInfo));
        //            if (myFileInfos.Count <= 1000) continue;
        //            var thisBatch = myFileInfos.Select(MyFileInfoToScvLine).ToList();
        //            WriteFile(DirectoryContentInfo, thisBatch);
        //            myFileInfos = new List<MyFileInfo>();
        //        }
        //        catch
        //        {
        //            Console.WriteLine($"cannot parse {f}");
        //        }

        //    }

        //    if (myFileInfos.Count > 0)
        //    {
        //        var thisBatch = myFileInfos.Select(MyFileInfoToScvLine).ToList();
        //        WriteFile(DirectoryContentInfo, thisBatch);
        //    }

        //    Console.WriteLine("done parsing");
        //}

    }
}
