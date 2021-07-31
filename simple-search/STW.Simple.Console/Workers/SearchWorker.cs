using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Nest;

using STW.Simple.Console.Libs;
using STW.Simple.Console.Models;

using System;

namespace STW.Simple.Console.Workers
{
    /// <summary>
    /// Search Worker
    /// </summary>
    public class SearchWorker : ISearchWorker
    {
        private readonly ILogger _logger;
        private readonly IConfigurationRoot _config;
        private ElasticClient _client;

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="logger">ILogger</param>
        /// <param name="config">IConfigurationRoot</param>
        public SearchWorker(ILogger<SearchWorker> logger, IConfigurationRoot config)
        {
            this._logger = logger;
            this._config = config;
        }

        /// <summary>
        /// Run (main logic)
        /// </summary>
        /// <param name="o">CommandOptions</param>
        public void Run(CommandOptions o)
        {
            #region "Parameters and Configuration"
            if (o == null) throw new ArgumentNullException(nameof(o));

            long ms;

            int size = (o.QueryResults > 0) ? o.QueryResults : CommandOptions.QueryResultsDefault;

            // Index names must be all lower case and trimmed
            string indexName = ElasticHelper.IndexNameFix(this._config["DefaultIndex"]);

            // Connection String
            string elasticConnectionString = this._config["ConnectionString"];
            if (string.IsNullOrWhiteSpace(elasticConnectionString))
                throw new InvalidOperationException("ConnectionString: Not configured in config.json");
            #endregion

            #region "Make Client and Index"

            // Make Client
            var settings = new ConnectionSettings(new Uri(elasticConnectionString)).DefaultIndex(indexName);
            _client = new ElasticClient(settings);

            using (var t1 = new Libs.ConsoleTimer("Index"))
            {
                // Create Index
                bool created = _client.IndexCreateIfMissing<Models.Person>(
                    indexName: ref indexName,
                    response: out CreateIndexResponse indexResponse,
                    dropExisting: true);

                ms = t1.ElapsedMilliseconds;
                if (!created)
                {
                    System.Console.WriteLine($"{indexName}, Exists: true, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}\n");
                }
                else
                {
                    System.Console.WriteLine($"{indexName}, Created: {indexResponse.IsValid}, Error: {indexResponse.ServerError}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}\n");
                }
            }
            #endregion

            #region "Create Records to Search"
            using (var t1 = new ConsoleTimer($"Create data: {o.Records} records"))
            {
                bool isLikely = CreateData(_client, o.Records, o.SearchText);
                ms = t1.ElapsedMilliseconds;
                System.Console.WriteLine($"Created {o.Records}, Likely: {isLikely}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }
            #endregion

            #region "Wildcard Search"

            string searchTerm = string.Empty;
            ISearchResponse<Person> results;

            searchTerm = "*" + o.SearchText + "*";
            using (var t1 = new Libs.ConsoleTimer($"Search: {searchTerm}"))
            {
                results = _client.Search<Models.Person>(s => s
                    .From(0)
                    .Size(size)
                    .Index(indexName)
                    .Query(q => q.Wildcard(w => w.Boost(1.0).Field(f => f.FirstName).Value(searchTerm)) ||
                                q.Wildcard(w => w.Boost(1.0).Field(f => f.LastName).Value(searchTerm))
                    )
                );
                ms = t1.ElapsedMilliseconds;
            }

            DumpRecords(results, size, searchTerm, ms);

            #endregion

            #region "Range Search"

            searchTerm = "Id: 1 to 9";
            using (var t1 = new Libs.ConsoleTimer($"Search: {searchTerm}"))
            {

                results = _client.Search<Models.Person>(s => s
                            .From(0)
                            .Size(size)
                            .Index(indexName)
                            .Query(q => q.Range(r => r.Boost(1.0).Field(f => f.Id).GreaterThanOrEquals(1).LessThanOrEquals(9))
                            )
                        );
                ms = t1.ElapsedMilliseconds;
            }

            DumpRecords(results, size, searchTerm, ms);

            #endregion

            #region "Value Search"

            searchTerm = "Id: 9";
            using (var t1 = new Libs.ConsoleTimer($"Search: {searchTerm}"))
            {
                results = _client.Search<Models.Person>(s => s
                    .From(0)
                    .Size(size)
                    .Index(indexName)
                    .Query(q => q.Term(m => m.Id, 9))
                );

                ms = t1.ElapsedMilliseconds;
            }

            DumpRecords(results, size, searchTerm, ms);

            #endregion

            #region "Phrase Search"

            searchTerm = Person.EmailSuffix;
            using (var t1 = new Libs.ConsoleTimer($"Search: {searchTerm}"))
            {
                results = _client.Search<Models.Person>(s => s
                        .From(0)
                        .Size(size)
                        .Index(indexName)
                        .Query(q => q.MatchPhrase(p => p.Field(f => f.EMail).Boost(1.0).Query(searchTerm))
                        )
                    );
                ms = t1.ElapsedMilliseconds;
            }

            DumpRecords(results, size, searchTerm, ms);

            #endregion
        }

        #region "Dump Results"

        /// <summary>
        /// Dump Search Results
        /// </summary>
        /// <param name="results">ISearchResponse</param>
        /// <param name="size">Max requested</param>
        /// <param name="searchTerm">Search Term</param>
        /// <param name="ms">Milliseconds</param>
        public static void DumpRecords(ISearchResponse<Person> results, int size, string searchTerm, long ms = 0)
        {
            if (results is null) throw new ArgumentNullException(nameof(results));

            System.Console.WriteLine($"\nValid: {results.IsValid}, Hits: {results.Hits.Count}/{size}, Search: {searchTerm}, Elaspeds: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            foreach (var p in results.Documents)
            {
                System.Console.WriteLine($"\t{p}");
            }
        }

        #endregion

        #region "Generate Data"

        /// <summary>
        /// Create Data
        /// </summary>
        /// <param name="client">ElasticClient</param>
        /// <param name="count">How many to make</param>
        /// <param name="searchText">search text</param>
        /// <returns>True if likely</returns>
        public static bool CreateData(ElasticClient client, int count, string searchText)
        {
            bool isLikely = false;
            if (client is null) throw new ArgumentNullException(nameof(client));
            System.Console.WriteLine($"Generating {count} Records");
            for (int i = 0; i < count; i++)
            {
                var p = Models.Person.MakeRandom((i + 1));
                if (p.FirstName.Contains(searchText) || p.LastName.Contains(searchText)) isLikely = true;
                System.Console.WriteLine($"\t{p}");
                client.IndexDocument<Models.Person>(p);
            }
            return isLikely;
        }

        #endregion

    }
}
