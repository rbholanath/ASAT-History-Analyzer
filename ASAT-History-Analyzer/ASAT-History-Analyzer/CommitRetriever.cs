using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace ASAT_History_Analyzer
{
    class CommitRetriever
    {
        private readonly GitHubClient _client;
        private readonly IRepositoryCommitsClient _repositoryCommitClient;

        public CommitRetriever()
        {
            _client = SetUpClient();

            _repositoryCommitClient = _client.Repository.Commits;
        }

        public void RetrieveCommits(string filePath)
        {
            Console.WriteLine("Reading files from: " + filePath);

            var filename = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Last();

            var commitAnalyzer = new CommitAnalyzer(Utilities.GetDropboxPath() + @"Out\History\", filename);

            using (var streamReader = new StreamReader(filePath))
            {
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();

                    var commits = GetCommit(_repositoryCommitClient, line).Result;

                    if (commits != null && commits.Count > 0)
                    {
                        commitAnalyzer.Analyze(commits, line);
                    }
                    else
                    {
                        commitAnalyzer.LogError(line);
                    }
                }
            }

            commitAnalyzer.Close();

            Console.WriteLine("Done reading files.");
        }

        GitHubClient SetUpClient()
        {
            var client = new GitHubClient(new ProductHeaderValue("ASAT-History-Analyzer"));

            var token = "";

            using (var streamReader = new StreamReader("credentials.txt"))
            {
                while (!streamReader.EndOfStream)
                {
                    token = streamReader.ReadLine();
                }
            }

            var tokenAuth = new Credentials(token);
            client.Credentials = tokenAuth;

            return client;
        }

        async Task<IReadOnlyList<GitHubCommit>> GetCommit(IRepositoryCommitsClient repositoryCommitsClient, string line)
        {
            var parts = line.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var user = parts[3];
            var repo = parts[4];
            var path = Utilities.CombineStringArray(parts, 6, '/');

            var request = new CommitRequest { Path = path };

            try
            {
                return await repositoryCommitsClient.GetAll(user, repo, request).ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                Console.WriteLine("File: " + line + " gave an error.");

                return null;
            }
            catch (RateLimitExceededException e)
            {
                var remaining = e.Reset;

                Console.WriteLine("Rate Limited: " + e.Limit + ". Reset on: " + e.Reset);

                Thread.Sleep(remaining.Subtract(DateTime.Now));
            }

            // It should only get here if we were rate limited. In that case, just try again and return. 
            // This call cannot be in the above catch because it's async.
            return await GetCommit(repositoryCommitsClient, line);
        }
    }
}
