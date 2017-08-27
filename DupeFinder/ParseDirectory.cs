﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FileDupeFinder
{
    internal class ParseDirectory
    {
        public ParseDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                Console.WriteLine("path is missing or not exist");
                return;
            }
            Console.WriteLine($"output file is {_directoryContentInfo}");
            WriteFile(_directoryContentInfo, new List<string> {"path\tfile\textension\tdateTaken\tsize\tmd5"});
            ParseDir(directory);
            Console.WriteLine("done parsing");
        }

        private readonly string _directoryContentInfo = Environment.GetEnvironmentVariable("USERPROFILE") +
                                                        $@"\Documents\allFiles{DateTime.Now:yyyy-dd-M--HH-mm}.csv";

        private void ParseDir(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("path is missing or not exist");
                return;
            }
            Console.WriteLine($"parsing {directory}");
            var fileInfos = new List<MyFileInfo>();
            try
            {
                var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(f);
                        fileInfos.Add(GetMyFileInfo(fileInfo));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"cannot parse {f}, {e.Message}");
                    }
                }

                if (fileInfos.Count > 0)
                {
                    var thisBatch = fileInfos.Select(MyFileInfoToScvLine).ToList();
                    WriteFile(_directoryContentInfo, thisBatch);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            try
            {
                var directories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly).ToList();
                directories.ForEach(ParseDir);
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed Directory.GetDirectories({directory}\n{e.Message}");
            }
        }

        public string FileNameToDateTaken(string name)
        {
            if (name.Length != 19 || name[8] != '_') return null;
            name = name.Remove(8, 1).Substring(0, 14);
            if (!long.TryParse(name, out long _)) return null;
            name = name.Insert(12, ":");
            name = name.Insert(10, ":");
            name = name.Insert(8, " ");
            name = name.Insert(6, ":");
            name = name.Insert(4, ":");
            return name;
        }

        MyFileInfo GetMyFileInfo(FileInfo fileInfo)
        {
            var myFileInfo = new MyFileInfo
            {
                Name = fileInfo.Name,
                Folder = fileInfo.Directory.ToString(),
                Extension = Path.GetExtension(fileInfo.Name).Replace(".", ""),
                Size = fileInfo.Length,
                Md5 = GetMd5(fileInfo.FullName)
            };
            try
            {
                myFileInfo.DateTaken = FileNameToDateTaken(fileInfo.Name);
                if (string.IsNullOrEmpty(myFileInfo.DateTaken))
                {
                    var image = Image.FromFile(fileInfo.FullName);
                    var id = image.GetPropertyItem(306);
                    var enc = new ASCIIEncoding();
                    myFileInfo.DateTaken = enc.GetString(id.Value, 0, id.Len - 1);
                }
            }
            catch (Exception)
            {
                myFileInfo.DateTaken =
                    fileInfo.CreationTime.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            return myFileInfo;
        }


        public void WriteFile(string fileName, List<string> content)
        {
            using (var wr = new StreamWriter(fileName, true, Encoding.UTF8))
            {
                content.ForEach(x => wr.WriteLine(x));
                wr.Close();
            }
        }

        private string MyFileInfoToScvLine(MyFileInfo myFileInfo)
        {
            return
                $"{myFileInfo.Folder}\t{myFileInfo.Name}\t{myFileInfo.Extension}\t{myFileInfo.DateTaken}\t{myFileInfo.Size}\t{myFileInfo.Md5}";
        }


        private string GetMd5(string filePath)
        {
            string result = null;
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read) {Position = 0})
                {
                    var ha = new MD5CryptoServiceProvider().ComputeHash(fs).ToList();
                    fs.Close();
                    ha.ForEach(x => result += $"{x:X2}");
                }
            }
            catch
            {
                // ignored
            }
            return result;
        }
    }
}
