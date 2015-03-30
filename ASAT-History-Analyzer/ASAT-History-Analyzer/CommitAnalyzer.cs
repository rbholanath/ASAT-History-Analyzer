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

        private readonly string _filePath;
        private readonly string _filename;

        private readonly StreamWriter _loggerStreamWriter;

        public CommitAnalyzer(string filePath, string filename)
        {
            _timesChanged = new Dictionary<int, int>();
            _relativeDates = new Dictionary<int, int>();
            _absoluteDates = new Dictionary<string, int>();
            _createdOn = new Dictionary<string, int>();

            _filePath = filePath;
            _filename = filename;

            _loggerStreamWriter = File.AppendText(filePath + DateTime.Now.ToString("yyyyMMdd-HHmm") + "_log_" + filename);
        }

        public void Analyze(IReadOnlyList<GitHubCommit> commits, string line)
        {
            _loggerStreamWriter.WriteLine("File: " + line);

            var dates = commits.Select(commit => commit.Commit.Committer.Date).ToList();

            _loggerStreamWriter.WriteLine("\tTimes changed: " + (dates.Count - 1));

            AddOrIncrease(_timesChanged, dates.Count - 1);

            var firstDate = dates.Last();

            AddOrIncrease(_createdOn, firstDate.Date.ToString("d"));

            foreach (var date in dates)
            {
                if (!date.Equals(firstDate))
                {
                    var daysSince = (date.Subtract(firstDate)).Days;

                    AddOrIncrease(_relativeDates, daysSince);

                    AddOrIncrease(_absoluteDates, date.Date.ToString("d"));

                    _loggerStreamWriter.WriteLine("\t" + date.Date.ToString("d") + " : " + daysSince + " days from " + firstDate.Date.ToString("d"));
                }
            }
        }

        private void AddOrIncrease<T>(Dictionary<T, int> dictionary, T key)
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
            _loggerStreamWriter.WriteLine("File: " + line);
            _loggerStreamWriter.WriteLine("\tERROR.");
        }

        public void Close()
        {
            Utilities.WriteDictionary(_relativeDates, _filePath + "relative_" + Path.ChangeExtension(_filename, ".csv"));
            Utilities.WriteDictionary(_absoluteDates, _filePath + "absolute_" + Path.ChangeExtension(_filename, ".csv"));
            Utilities.WriteDictionary(_timesChanged, _filePath + "times_changed_" + Path.ChangeExtension(_filename, ".csv"));
            Utilities.WriteDictionary(_createdOn, _filePath + "created_" + Path.ChangeExtension(_filename, ".csv"));

            _loggerStreamWriter.Close();
        }
    }
}
