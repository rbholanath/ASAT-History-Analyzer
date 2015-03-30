using System;
using System.Collections.Generic;
using System.IO;
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

        public async void RetrieveCommits(string filename)
        {
            Console.WriteLine("Reading files from: filename");

            using (var streamReader = new StreamReader(filename))
            {
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();

                    await GetCommit(_repositoryCommitClient, line);
                }
            }

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
            Console.WriteLine("File: " + line);

            var parts = line.Split('/');
            var user = parts[4];
            var repo = parts[5];
            var path = Utilities.CombineStringArray(parts, 7, '/');

            var request = new CommitRequest { Path = path };
            var commits = await repositoryCommitsClient.GetAll(user, repo, request);

            foreach (var commit in commits)
            {
                Console.WriteLine(line + " : " + commit.Commit.Message);
            }

            return commits;
        }
    }
}
