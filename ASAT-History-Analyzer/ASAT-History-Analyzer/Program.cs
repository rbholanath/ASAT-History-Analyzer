﻿using System;
using System.IO;

namespace ASAT_History_Analyzer
{
    static class Program
    {
        static void Main()
        {
            var directory = Utilities.GetDropboxPath() + @"History\";

            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories);

                var commitRetriever = new CommitRetriever();

                foreach (var file in files)
                {
                    commitRetriever.RetrieveCommits(file);
                }
            }
           
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
