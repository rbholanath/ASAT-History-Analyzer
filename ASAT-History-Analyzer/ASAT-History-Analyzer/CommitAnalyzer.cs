using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Octokit;

namespace ASAT_History_Analyzer
{
    class CommitAnalyzer
    {
        private readonly Dictionary<int, int> _timesChanged;
        private readonly Dictionary<int, int> _relativeDates;
        private readonly Dictionary<string, int> _absoluteDates;
        private readonly Dictionary<string, int> _createdOn;

        private readonly Dictionary<int, int> _additions;
        private readonly Dictionary<int, int> _deletions;
        private readonly Dictionary<int, int> _totalChanges;

        private readonly string _filePath;
        private readonly string _filename;

        private int _filesRead;
        private int _errors;

        private readonly StreamWriter _loggerStreamWriter;

        public CommitAnalyzer(string filePath, string filename)
        {
            _timesChanged = new Dictionary<int, int>();
            _relativeDates = new Dictionary<int, int>();
            _absoluteDates = new Dictionary<string, int>();
            _createdOn = new Dictionary<string, int>();

            _additions = new Dictionary<int, int>();
            _deletions = new Dictionary<int, int>();
            _totalChanges = new Dictionary<int, int>();

            _filePath = filePath;
            _filename = filename;

            _filesRead = 0;
            _errors = 0;

            _loggerStreamWriter = File.CreateText(filePath + DateTime.Now.ToString("yyyyMMdd-HHmm") + "_log_" + filename);
        }

        public void Analyze(IReadOnlyList<GitHubCommit> commits, string line)
        {
            var firstDate = commits.Select(commit => commit.Commit.Committer.Date).ToList().Last();

            _loggerStreamWriter.WriteLine("[" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "] | File: " + line);

            _loggerStreamWriter.WriteLine("\tTimes changed: " + (commits.Count - 1));

            AddOrIncrease(_timesChanged, commits.Count - 1);

            AddOrIncrease(_createdOn, firstDate.Date.ToString("yyyyMMdd"));

            foreach (var commit in commits)
            {
                AnalyzeCommit(commit, line, firstDate);
            }

            _filesRead++;
        }

        private void AnalyzeCommit(GitHubCommit commit, string line, DateTimeOffset firstDate)
        {
            var parts = line.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path = Utilities.CombineStringArray(parts, 6, '/');

            GitHubCommitFile file = null;

            if (commit.Files != null)
            {
                var fileList = commit.Files.ToList();

                file = fileList.FirstOrDefault(potentialFile => potentialFile.Filename == path);
            }

            var date = commit.Commit.Committer.Date;

            if (!date.Equals(firstDate))
            {
                var daysSince = (date.Subtract(firstDate)).Days;

                AddOrIncrease(_relativeDates, daysSince);
                AddOrIncrease(_absoluteDates, date.Date.ToString("yyyyMMdd"));

                if (file == null)
                {
                    _loggerStreamWriter.WriteLine("\t" + date.Date.ToString("yyyy-MM-dd") + " : " + daysSince + " days from " + firstDate.Date.ToString("yyyy-MM-dd")
                        + " +? -? =?");
                }
                else
                {
                    var additions = file.Additions;
                    var deletions = file.Deletions;
                    var totalChanges = additions - deletions;

                    AddOrIncrease(_additions, additions);
                    AddOrIncrease(_deletions, deletions);
                    AddOrIncrease(_totalChanges, totalChanges);

                    _loggerStreamWriter.WriteLine("\t" + date.Date.ToString("yyyy-MM-dd") + " : " + daysSince + " days from " + firstDate.Date.ToString("yyyy-MM-dd")
                        + " +" + additions + " -" + deletions + " =" + totalChanges);
                }
            }
        }

        private void AddOrIncrease<T>(IDictionary<T, int> dictionary, T key)
        {
            if (dictionary.ContainsKey(key))
            {
                int amount;
                dictionary.TryGetValue(key, out amount);
                dictionary[key] = + 1;
            }
            else
            {
                dictionary.Add(key, 1);
            }
        }

        public void LogError(string line)
        {
            _loggerStreamWriter.WriteLine("[" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "] | File: " + line);
            _loggerStreamWriter.WriteLine("\tERROR.");

            _errors++;
        }

        public void Close()
        {
            Utilities.WriteDictionary(_relativeDates, _filePath + "relative_" + Path.ChangeExtension(_filename, ".csv"));
            Utilities.WriteDictionary(_absoluteDates, _filePath + "absolute_" + Path.ChangeExtension(_filename, ".csv"));
            Utilities.WriteDictionary(_timesChanged, _filePath + "times_changed_" + Path.ChangeExtension(_filename, ".csv"));
            Utilities.WriteDictionary(_createdOn, _filePath + "created_" + Path.ChangeExtension(_filename, ".csv"));

            Utilities.WriteDictionary(_additions, _filePath + "additions_" + Path.ChangeExtension(_filename, ".csv"));
            Utilities.WriteDictionary(_deletions, _filePath + "deletions_" + Path.ChangeExtension(_filename, ".csv"));
            Utilities.WriteDictionary(_totalChanges, _filePath + "total_changes_" + Path.ChangeExtension(_filename, ".csv"));

            _loggerStreamWriter.WriteLine("Files read: " + _filesRead);
            _loggerStreamWriter.WriteLine("Errors: " + _errors);

            _loggerStreamWriter.Close();
        }
    }
}
