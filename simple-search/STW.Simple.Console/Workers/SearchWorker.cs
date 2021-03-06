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
        #region "Fields and CTOR"

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

        #endregion

        /// <summary>
        /// Run (main logic)
        /// </summary>
        /// <param name="o">CommandOptions</param>
        public void Run(CommandOptions o)
        {

            #region "Parameters and Configuration"

            if (o == null) throw new ArgumentNullException(nameof(o));

            long ms;
            long id;
            string searchTerm = string.Empty;
            int size = (o.QueryResults > 0) ? o.QueryResults : CommandOptions.QueryResultsDefault;

            // Index names must be all lower case and trimmed
            string indexName = this._config["DefaultIndex"];
            if (string.IsNullOrWhiteSpace(indexName)) throw new MissingConfigurationException("DefaultIndex", "Not configured in config.json");
            indexName = ElasticHelper.IndexNameFix(indexName);

            // Connection String
            string elasticConnectionString = this._config["ConnectionString"];
            if (string.IsNullOrWhiteSpace(elasticConnectionString))
                throw new MissingConfigurationException("ConnectionString", "Not configured in config.json");

            #endregion

            #region "Make Client and Index"

            // Make Client
            using (var t1 = new Libs.ConsoleTimer())
            {
                var settings = new ConnectionSettings(new Uri(elasticConnectionString)).DefaultIndex(indexName);

                // Only used for Demo in real apps, this is not desirable as streaming is more efficient
                settings.DisableDirectStreaming();

                _client = new ElasticClient(settings);
                
                ms = t1.ElapsedMilliseconds;

                // Verify the connection
                var pingResults = _client.Ping();
                _logger?.LogInformation($"Create Client, Is Valid: {pingResults.IsValid}, Error: {pingResults?.ServerError?.Status} , Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}\n");
            }

            // Create Index
            using (var t1 = new Libs.ConsoleTimer("Index"))
            {
                bool created = _client.IndexCreateIfMissing<Models.Person>(
                    indexName: ref indexName,
                    response: out CreateIndexResponse indexResponse,
                    dropExisting: true);

                ms = t1.ElapsedMilliseconds;
                if (!created)
                {
                    _logger?.LogInformation($"{indexName}, Exists: true, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}\n");
                }
                else
                {
                    _logger?.LogInformation($"{indexName}, Created: {indexResponse.IsValid}, Error: {indexResponse.ServerError}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}\n");
                }
            }

            #endregion

            #region "Create Records to Search"

            using (var t1 = new ConsoleTimer($"Create data: {o.Records} records"))
            {
                bool isLikely = CreateData(_client, o.Records, o.SearchText);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"Created: {o.Records}, Likely: {isLikely}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Count Records"

            using (var t1 = new ConsoleTimer($"Count records"))
            {
                var countResponse = _client.Count<Models.Person>();
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nCount: {countResponse.Count}, IsValid: {countResponse.IsValid}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Delete by ID"

            id = 1;
            using (var t1 = new ConsoleTimer($"Delete Record"))
            {
                var deleteResponse = _client.Delete<Models.Person>(id);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nDelete: {id}, {deleteResponse.Result}, IsValid: {deleteResponse.IsValid}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Delete by Query"

            id = 5;
            using (var t1 = new ConsoleTimer($"Delete Record"))
            {
                var qbqResult = _client.DeleteByQuery<Models.Person>(p => p.Index(indexName).Query(q => q.Term(m => m.Id, id)));
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nDeleteByQuery: {qbqResult.Total}, IsValid: {qbqResult.IsValid}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Exists"

            id = 11;
            using (var t1 = new ConsoleTimer($"Exists Record"))
            {
                var existsResponse = _client.DocumentExists<Models.Person>(id);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nExists: {existsResponse.Exists}, IsValid: {existsResponse.IsValid}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Get By Id"

            using (var t1 = new ConsoleTimer($"Get Record"))
            {
                var getResponse = _client.Get<Models.Person>(id);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nGet: {getResponse.Source}, IsValid: {getResponse.IsValid}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Update Record and Get it Back"

            using (var t1 = new ConsoleTimer($"Update Record"))
            {
                var model = Models.Person.MakeRandom(id);
                var updateResponse = _client.Update<Models.Person>(new DocumentPath<Models.Person>(model), u =>  u.Doc(model).Refresh(Elasticsearch.Net.Refresh.WaitFor));
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nUpdate: {updateResponse.Result}, IsValid: {updateResponse.IsValid}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            using (var t1 = new ConsoleTimer($"Get Record"))
            {
                var getResponse = _client.Get<Models.Person>(id);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nGet: {getResponse.Source}, IsValid: {getResponse.IsValid}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Wildcard Search"

            ISearchResponse<Person> results;

            searchTerm = "*" + o.SearchText + "*";
            using (var t1 = new Libs.ConsoleTimer($"Search: {searchTerm}"))
            {
                var request = new SearchRequest<Models.Person>();

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

            // _client.Explain<Models.Person>()

            DumpRecords(results, size, searchTerm, ms, o.Verbose);

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

            DumpRecords(results, size, searchTerm, ms, o.Verbose);

            #endregion

            #region "Value Search"

            searchTerm = "Id: {id}";
            using (var t1 = new Libs.ConsoleTimer($"Search: {searchTerm}"))
            {
                results = _client.Search<Models.Person>(s => s
                    .From(0)
                    .Size(size)
                    .Index(indexName)
                    .Query(q => q.Term(m => m.Id, id))
                );

                ms = t1.ElapsedMilliseconds;
            }

            DumpRecords(results, size, searchTerm, ms, o.Verbose);

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

            DumpRecords(results, size, searchTerm, ms, o.Verbose);

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
        /// <param name="verbose">Show verbose output</param>
        public void DumpRecords(ISearchResponse<Person> results, int size, string searchTerm, long ms = 0, bool verbose = false)
        {
            if (results is null) throw new ArgumentNullException(nameof(results));

            _logger?.LogInformation($"\nRequest: {results?.ApiCall?.HttpStatusCode},Valid: {results.IsValid}, Hits: {results.Hits.Count}/{size}, Search: {searchTerm}, Elaspsed: {ConsoleTimer.DisplayElaspsedTime(ms)} {(verbose ? "Debug:\n" + results?.DebugInformation : "")}\n");

            if (verbose)
            {
                foreach (var p in results.Documents)
                {
                    _logger?.LogInformation($"\t{p}");
                }
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
        public bool CreateData(ElasticClient client, int count, string searchText)
        {
            bool isLikely = false;
            if (client is null) throw new ArgumentNullException(nameof(client));
            _logger?.LogInformation($"Generating {count} Records");
            for (int i = 0; i < count; i++)
            {
                var p = Models.Person.MakeRandom((i + 1));
                if (p.FirstName.Contains(searchText) || p.LastName.Contains(searchText)) isLikely = true;
                _logger?.LogInformation($"\t{p}");
                // This is what adds the record to the search index
                client.IndexDocument<Models.Person>(p);
            }
            return isLikely;
        }

        #endregion

    }
}
