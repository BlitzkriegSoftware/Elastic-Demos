using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


using STW.Simple.Console.Libs;
using STW.Simple.Console.Models;

using System;
using System.Collections.Generic;

using Elastic_Simple_Search.test.Libs;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;


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
        private ElasticsearchClient _client;

#pragma warning disable IDE0052 // Future
        private readonly float? boost = (float)1.0;
#pragma warning restore IDE0052 // Remove unread private members

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
                var elasticFingerPrint = "94:75:CE:4F:EB:05:32:83:40:B8:18:BB:79:01:7B:E0:F0:B6:C3:01:57:DB:4D:F5:D8:B8:A6:BA:BD:6D:C5:C4";

                // See: https://www.elastic.co/guide/en/elasticsearch/client/net-api/8.0/connecting.html
                var esettings = new ElasticsearchClientSettings(new Uri(elasticConnectionString))
                    // Only used for Demo in real apps, this is not desirable as streaming is more efficient
                    .DisableDirectStreaming()
                    .DefaultIndex(indexName)
                    .CertificateFingerprint(elasticFingerPrint)
                    .Authentication(new BasicAuthentication("elastic", "password"))
                    ;

                _client = new ElasticsearchClient(esettings);

                ms = t1.ElapsedMilliseconds;

                // Verify the connection
                var pingResults = _client.Ping();
                _logger?.LogInformation($"Create Client, Is Valid: {pingResults.IsValidResponse}, Error: {pingResults.ElasticsearchServerError}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}\n");
            
                if(!pingResults.IsValidResponse)
                {
                    _logger?.LogError(pingResults.ElasticsearchServerError.ToString());
                    return;
                }
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
                    _logger?.LogInformation($"{indexName}, Acknowledged: {indexResponse.Acknowledged}, Error: {indexResponse.ElasticsearchServerError}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}\n");
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
                CountResponse countResponse = _client.Count();
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nCount: {countResponse.Count}, IsValid: {countResponse.IsValidResponse}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Delete by ID"

            id = 1;
            using (var t1 = new ConsoleTimer($"Delete Record"))
            {
                _client.Delete<Models.Person>(id);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nDelete: {id}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Exists"

            id = 11;
            using (var t1 = new ConsoleTimer($"Exists Record"))
            {
                var existsResponse = _client.Exists<Models.Person>(id);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nExists: {existsResponse}, IsValid: {existsResponse.IsValidResponse}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Get By Id"

            using (var t1 = new ConsoleTimer($"Get Record"))
            {
                var model = _client.Get<Models.Person>(id);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nGet: {model}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Update Record and Get it Back"

            using (var t1 = new ConsoleTimer($"Update Record"))
            {
                var model = Person.MakeRandom(id);
                _client.Delete<Models.Person>(id);
                IndexResponse ir = _client.Index<Models.Person>(model);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nUpdate: {ir.Result}, IsValid: {ir.IsValidResponse}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            using (var t1 = new ConsoleTimer($"Get Record"))
            {
                var model = _client.Get<Models.Person>(id);
                ms = t1.ElapsedMilliseconds;
                _logger?.LogInformation($"\nGet: {model}, Elasped: {ConsoleTimer.DisplayElaspsedTime(ms)}");
            }

            #endregion

            #region "Wildcard Search"

            SearchResponse<Person> results;

            searchTerm = "*" + o.SearchText + "*";
            using (var t1 = new Libs.ConsoleTimer($"Search: {searchTerm}"))
            {
                var request = new SearchRequest<Models.Person>();


                results = _client.Search<Models.Person>(s => s
                    .From(0)
                    .Size(size)
                    .Index(indexName)
                    //.Query(q => q.Wildcard(w => w.Boost(boost).Field(f => f.FirstName).Value(searchTerm)) ||
                    //            q.Wildcard(w => w.Boost(boost).Field(f => f.LastName).Value(searchTerm))
                    //)
                    .Query(
                            q => q.Wildcard(w=>w.Field(f=>f.FirstName).Value(searchTerm))
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
                var idsQuery = new IdsQuery
                {
                    Values = new Ids(new List<string>() { "1", "2", "3", "4", "5", "6", "7", "8", "9" })
                };
                results = _client.Search<Models.Person>(s => s
                            .From(0)
                            .Size(size)
                            .Index(indexName)
                            .Query(q => q.Ids(idsQuery: idsQuery))
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
                        .Query(q => q.MatchPhrase(p => p.Field(f => f.EMail).Boost((float)1.0).Query(searchTerm))
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
        public void DumpRecords(SearchResponse<Person> results, int size, string searchTerm, long ms = 0, bool verbose = false)
        {
            if (results is null) throw new ArgumentNullException(nameof(results));

            _logger?.LogInformation($"\nRequest: {results.ApiCallDetails.HttpStatusCode},Valid: {results.IsValidResponse}, Hits: {results.Hits.Count}/{size}, Search: {searchTerm}, Elaspsed: {ConsoleTimer.DisplayElaspsedTime(ms)} {(verbose ? "Debug:\n" + results?.DebugInformation : "")}\n");

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
        public bool CreateData(ElasticsearchClient client, int count, string searchText)
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
                client.Index<Models.Person>(p);
            }
            return isLikely;
        }

        #endregion

    }
}
