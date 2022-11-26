using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;

using BlitzkriegSoftware.MsTest;

using Elastic_Simple_Search.Test.Libs;
using Elastic_Simple_Search.Test.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Drawing;

namespace Elastic_Simple_Search.Test
{
    /// <summary>
    /// Integration Tests
    /// </summary>
    [TestClass]
    public class IntegrationTests
    {

        #region "Vars, Consts, etc"

        /// <summary>
        /// Elastic Index Names Must be lowercase!
        /// </summary>
        public const string ElasticDefaultIndex = "people";

        /// <summary>
        /// Docker Default Image: Url
        /// </summary>
        public const string ElasticConnectionString = "http://localhost:9200";
        /// <summary>
        /// Docker Default Image: FingerPrint
        /// </summary>
        public const string ElasticFingerPrint = "94:75:CE:4F:EB:05:32:83:40:B8:18:BB:79:01:7B:E0:F0:B6:C3:01:57:DB:4D:F5:D8:B8:A6:BA:BD:6D:C5:C4";
        /// <summary>
        /// Docker Default Image: Username
        /// </summary>
        public const string ElasticUsername = "elastic";
        /// <summary>
        /// Docker Default Image: Password
        /// </summary>
        public const string ElasticPassword = "password";

        /// <summary>
        /// Make this many records
        /// </summary>
        public const int ElasticRecords = 50;

        private ElasticsearchClient _client;
        private static TestContext _testContext;
        private static ILogger<IntegrationTests> _logger;

        private static bool _clientInit = false;
        private static bool _indexInit = false;
        private static bool _dataInit = false;

        #endregion

        #region "Helpers"
        /// <summary>
        /// Create Data
        /// </summary>
        /// <param name="client">ElasticClient</param>
        /// <param name="count">How many to make</param>
        /// <param name="searchText">search text</param>
        /// <returns>True if likely</returns>
        public static bool CreateData(ElasticsearchClient client, int count, string searchText)
        {
            bool isLikely = false;
            if (client is null) throw new ArgumentNullException(nameof(client));
            _logger?.LogInformation($"Generating {count} Records");
            for (int i = 0; i < count; i++)
            {
                Person p = Person.MakeRandom((i + 1));
                if (p.FirstName.Contains(searchText) || p.LastName.Contains(searchText)) isLikely = true;
                _logger?.LogInformation($"\t{p}");
                // This is what adds the record to the search index
                var ci = client.Index<Models.Person>(p);
                _logger?.LogDebug($"{ci.ApiCallDetails}, {ci.IsValidResponse}");
            }
            return isLikely;
        }

        /// <summary>
        /// Dump Search Results
        /// </summary>
        /// <param name="results">ISearchResponse</param>
        /// <param name="size">Max requested</param>
        /// <param name="searchTerm">Search Term</param>
        /// <param name="verbose">Show verbose output</param>
        public static void DumpRecords(IEnumerable<Person> results)
        {
            if (results is null) throw new ArgumentNullException(nameof(results));
            foreach (var p in results)
            {
                _logger?.LogInformation($"\t{p}");
            }
        }

        #endregion

        #region "Test Boilerplate"
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _testContext = context;
            _logger = new MsTestLogger<IntegrationTests>(_testContext);
        }

        #endregion

        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0000_Create_Client()
        {
            using var tx = new TxTimer(_testContext);

            // See: https://www.elastic.co/guide/en/elasticsearch/client/net-api/8.0/connecting.html
            var esettings = new ElasticsearchClientSettings(new Uri(ElasticConnectionString))
                // Only used for Demo in real apps, this is not desirable as streaming is more efficient
                .DisableDirectStreaming()
                .DefaultIndex(ElasticDefaultIndex)
                .CertificateFingerprint(ElasticFingerPrint)
                .Authentication(new BasicAuthentication(ElasticUsername, ElasticPassword))
                ;

            _client = new ElasticsearchClient(esettings);
            _testContext?.WriteLine($"Client Created: {tx.ElapsedMilliseconds}");

            var pingResults = _client.Ping();
            _logger?.LogInformation($"Create Client, Is Valid: {pingResults.IsValidResponse}, Error: {pingResults.ElasticsearchServerError}, Elasped: {tx.ElapsedMilliseconds}\n");

            if (!pingResults.IsValidResponse)
            {
                _logger?.LogError(pingResults.ElasticsearchServerError.ToString());
                Assert.Fail(pingResults.ElasticsearchServerError.ToString());
            }
            else
            {
                _clientInit = true;
            }

        }

        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0010_Create_Index()
        {
            if (!_clientInit) Test_0000_Create_Client();

            using var tx = new TxTimer(_testContext);

            var indexName = ElasticDefaultIndex;
            CreateIndexResponse indexResponse = new();
            bool? created = _client?.IndexCreateIfMissing<Models.Person>(
                indexName: ref indexName,
                response: ref indexResponse,
                dropExisting: true);

            if ((created.HasValue) && (created.Value))
            {
                _logger?.LogInformation($"{ElasticDefaultIndex}, Acknowledged: {indexResponse?.Acknowledged}, Error: {indexResponse?.ElasticsearchServerError}, Elasped: {tx.ElapsedMilliseconds}\n");
            }
            else
            {
                _logger?.LogInformation($"{ElasticDefaultIndex}, Exists: true, Elasped: {tx.ElapsedMilliseconds}\n");
            }
            _indexInit = true;
        }

        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0020_Create_Data()
        {
            if (!_indexInit) Test_0010_Create_Index();

            using var tx = new TxTimer(_testContext);
            bool isLikely = CreateData(_client, ElasticRecords, "S");
            _logger?.LogInformation($"Created {ElasticRecords} in {ElasticDefaultIndex}, Elasped: {tx.ElapsedMilliseconds}\n");

            var refreshResult = _client.Indices.RefreshAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (refreshResult.IsValidResponse)
            {
                _dataInit = true;
            }
            else
            {
                Assert.Fail(refreshResult.DebugInformation);
            }
        }

        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0030_Count_Data()
        {
            if (!_dataInit) Test_0020_Create_Data();

            using var tx = new TxTimer(_testContext);

            CountResponse countResponse = _client.Count();
            _logger?.LogInformation($"Count {countResponse.Count} in {ElasticDefaultIndex}, Elasped: {tx.ElapsedMilliseconds}\n");
        }

        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0050_Delete_By_Id()
        {
            if (!_dataInit) Test_0020_Create_Data();

            using var tx = new TxTimer(_testContext);

            var id = 1;
            var deleteResponse = _client.Delete<Models.Person>(id);
            _logger?.LogInformation($"Count {deleteResponse.Result} in {ElasticDefaultIndex}, Elasped: {tx.ElapsedMilliseconds}\n");
        }

        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0060_Exists_By_Id()
        {
            if (!_dataInit) Test_0020_Create_Data();

            using var tx = new TxTimer(_testContext);

            int id = 11;
            var existsResponse = _client.Exists<Models.Person>(id);
            _logger?.LogInformation($"Count {existsResponse.Exists} in {ElasticDefaultIndex}, Elasped: {tx.ElapsedMilliseconds}\n");
        }

        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0070_Get_By_Id()
        {
            if (!_dataInit) Test_0020_Create_Data();

            using var tx = new TxTimer(_testContext);

            int id = 11;
            var getResponse = _client.Get<Models.Person>(id);
            _logger?.LogInformation($"Get {id} => {getResponse.Source} in {ElasticDefaultIndex}, Elasped: {tx.ElapsedMilliseconds}\n");
        }


        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0080_Update_By_Id()
        {
            if (!_dataInit) Test_0020_Create_Data();

            using var tx = new TxTimer(_testContext);

            int id = 12;
            _ = _client.Delete<Models.Person>(id);
            var model = Person.MakeRandom(id);
            IndexResponse ir = _client.Index<Models.Person>(model);

            var getResponse = _client.Get<Models.Person>(id);
            _logger?.LogInformation($"Get {id} => {getResponse.Source} in {ElasticDefaultIndex}, Elasped: {tx.ElapsedMilliseconds}\n");
        }


        [TestMethod]
        [TestProperty("TestType", "IntegrationTest")]
        public void Test_0110_Search()
        {
            if (!_dataInit) Test_0020_Create_Data();

            using var tx = new TxTimer(_testContext);

            string searchTerm = "*t*";

            var searchResponse = _client.Search<Person>(s => s
                .Query (q => q
                    .MultiMatch (m => m
                        .Fields(new [] {"FirstName", "LastName"})
                        .Operator(Operator.Or)
                        .Query(searchTerm)
                    )
                )
            );

            if (searchResponse.IsValidResponse)
            {
                DumpRecords(searchResponse.Documents);
            } else
            {
                Assert.Fail(searchResponse.DebugInformation);
            }
        }


    } // class
} // namespace