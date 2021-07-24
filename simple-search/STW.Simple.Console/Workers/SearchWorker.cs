using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using STW.Simple.Console.Models;
using System;
using System.Collections.Generic;
using System.Text;

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
            if (o == null) throw new ArgumentNullException(nameof(o));

            // Index names must be all lower case
            string indexName = this._config["DefaultIndex"].ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(indexName)) 
                    throw new InvalidOperationException("DefaultIndex: Not configured in config.json");

            string elasticConnectionString = this._config["ConnectionString"];
            if (string.IsNullOrWhiteSpace(elasticConnectionString)) 
                    throw new InvalidOperationException("ConnectionString: Not configured in config.json");

            int size = (o.QueryResults > 0) ? o.QueryResults : CommandOptions.QueryResultsDefault;

            var settings = new ConnectionSettings(new Uri(elasticConnectionString)).DefaultIndex(indexName);

            _client = new ElasticClient(settings);

            if (_client.Indices.Exists(indexName).Exists)
            {
                System.Console.WriteLine($"{indexName}, Exists: true\n");
            }
            else
            {
                var response = _client.Indices.Create(indexName,
                        index => index.Map<Models.Person>(
                            x => x.AutoMap()
                        ));

                System.Console.WriteLine($"{indexName}, Created: {response.IsValid}, Error: {response.ServerError}\n");
            }

            // Create Records to Search
            System.Console.WriteLine($"Generating {o.Records} Records");
            for (int i=0; i < o.Records; i++)
            {
                var p = Models.Person.MakeRandom((i + 1));
                System.Console.WriteLine($"\t{p}");
                _client.IndexDocument<Models.Person>(p);
            }

            // Build a search expression
            var searchTerm = "*" + o.SearchText + "*";

            var request = new SearchRequest
            {
                From = 0, // Begin at start (used for paging)
                Size = size, // Page Size
                Query = new WildcardQuery { CaseInsensitive = true, Field = "FirstName", Value = searchTerm } ||
                        new WildcardQuery { CaseInsensitive = true, Field = "LastName", Value = searchTerm }
            };

            // Do a search
            ISearchResponse<Person> results = _client.Search<Models.Person>();

            System.Console.WriteLine($"\nValid: {results.IsValid}, Hits: {results.Hits.Count}/{size}, Search: {searchTerm}");
            foreach (var p in results.Documents)
            {
                System.Console.WriteLine($"\t{p}");
            }

        }

        #region "Generate Data"

        

        #endregion

    }
}
