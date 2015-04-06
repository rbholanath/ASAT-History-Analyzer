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
        private readonly IRepositoryCommitsClient _repositoryCommitClient;

        public CommitRetriever()
        {
            var client = SetUpClient();

            _repositoryCommitClient = client.Repository.Commits;
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
                        // If there is an error retrieving the full commit, just use the partial one.
                        var fullCommits = (from partialCommit in commits 
                                           let fullCommit = GetFullCommit(partialCommit, line).Result 
                                           select fullCommit ?? partialCommit).ToList();

                        commitAnalyzer.Analyze(fullCommits, line);
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
            try
            {
                var parts = line.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var user = parts[3];
                var repo = parts[4];
                var path = Utilities.CombineStringArray(parts, 6, '/');

                var request = new CommitRequest { Path = path };

                return await repositoryCommitsClient.GetAll(user, repo, request).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (e is RateLimitExceededException)
                {
                    var ex = (RateLimitExceededException) e;
                    var remaining = ex.Reset;

                    Console.WriteLine("Rate Limited: " + ex.Limit + ". Reset on: " + ex.Reset);

                    Thread.Sleep(remaining.Subtract(DateTime.Now));
                }
                else
                {
                    Console.WriteLine("File: " + line + " gave an error.");
                    Console.WriteLine("\t" + e.Message);

                    return null;
                }
            }

            // It should only get here if we were rate limited. In that case, just try again and return. 
            // This call cannot be in the above catch because it's async.
            // No rate limit check needed, because we were just rate limited.
            try
            {
                return await GetCommit(repositoryCommitsClient, line).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine("File: " + line + " gave an error.");
                Console.WriteLine("\t" + e.Message);
            }

            // It should never get here.
            return null;
        }

        async Task<GitHubCommit> GetFullCommit(GitReference partialCommit, string line)
        {
            try
            {
                var parts = line.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var user = parts[3];
                var repo = parts[4];

                return await _repositoryCommitClient.Get(user, repo, partialCommit.Sha).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (e is RateLimitExceededException)
                {
                    var ex = (RateLimitExceededException) e;
                    var remaining = ex.Reset;

                    Console.WriteLine("Rate Limited: " + ex.Limit + ". Reset on: " + ex.Reset);

                    Thread.Sleep(remaining.Subtract(DateTime.Now));
                }
                else
                {
                    Console.WriteLine("File: " + line + " gave an error.");
                    Console.WriteLine("\t" + e.Message);

                    return null;
                }
            }

            // It should only get here if we were rate limited. In that case, just try again and return. 
            // This call cannot be in the above catch because it's async.
            // No rate limit check needed, because we were just rate limited.
            try
            {
                return await GetFullCommit(partialCommit, line).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine("File: " + line + " gave an error.");
                Console.WriteLine("\t" + e.Message);
            }

            // It should never get here
            return null;
        }
    }
}
