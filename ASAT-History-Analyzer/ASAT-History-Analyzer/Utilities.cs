﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASAT_History_Analyzer
{
    static class Utilities
    {
        public static string CombineStringArray(string[] strings, int begin, char separator)
        {
            var stringBuilder = new StringBuilder();

            for (var i = begin; i < strings.Length; i++)
            {
                if (i > begin)
                {
                    stringBuilder.Append(separator);
                }

                stringBuilder.Append(strings[i]);
            }

            return stringBuilder.ToString();
        }

        public static string GetDropboxPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbPath = Path.Combine(appDataPath, @"Dropbox\host.db");

            String folderPath;

            if (!File.Exists(dbPath))
            {
                Console.WriteLine("Could not find Dropbox folder, please manually enter path.");
                folderPath = Console.ReadLine();
                Console.WriteLine();
            }
            else
            {
                var lines = File.ReadAllLines(dbPath);
                var dbBase64Text = Convert.FromBase64String(lines[1]);
                folderPath = Encoding.UTF8.GetString(dbBase64Text) + @"\TU Delft\Master 2e jaar\Configuration Analyzer\IO\History\";
            }

            return folderPath;
        }
    }
}
