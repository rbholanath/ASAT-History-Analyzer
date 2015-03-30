using System;
using System.Collections.Generic;
using System.IO;
using Octokit;

namespace ASAT_History_Analyzer
{
    class CommitAnalyzer
    {
        private StreamWriter _streamWriter;
        private static readonly object _syncObject = new object();

        public CommitAnalyzer(string filename)
        {
            _streamWriter = File.AppendText(filename);
        }

        public void Analyze(IReadOnlyList<GitHubCommit> commits, string line)
        {
            _streamWriter.WriteLine("File: " + line);

            foreach (var commit in commits)
            {
                _streamWriter.WriteLine("\t" + commit.Commit.Committer.Date);
            }
        }

        public void LogError(string line)
        {
            _streamWriter.WriteLine("File: " + line);
            _streamWriter.WriteLine("\tERROR.");
        }

        public void Close()
        {
            _streamWriter.Close();
        }
    }
}
