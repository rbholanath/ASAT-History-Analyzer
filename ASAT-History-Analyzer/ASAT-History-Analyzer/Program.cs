using System;
using System.IO;

namespace ASAT_History_Analyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            var directory = Utilities.GetDropboxPath();

            if (directory != null && Directory.Exists(directory))
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
