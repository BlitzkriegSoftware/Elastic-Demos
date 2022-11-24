using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using System;

namespace STW.Simple.Console.Libs
{
    /// <summary>
    /// Elastic Helper
    /// </summary>
    public static class ElasticHelper
    {

        /// <summary>
        /// Fix Index Name
        /// </summary>
        /// <param name="indexName">Index Name</param>
        /// <returns>Cleaned up index name</returns>
        public static string IndexNameFix(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentNullException(nameof(indexName));
            indexName = indexName.Trim().ToLowerInvariant();
            return indexName;
        }

        /// <summary>
        /// Create desired index if not there
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="client">(required) ElasticClient</param>
        /// <param name="indexName">(required) index name</param>
        /// <param name="dropExisting">Delete and recreate index</param>
        /// <param name="response">CreateIndexResponse</param>
        /// <returns>True if created</returns>
        public static bool IndexCreateIfMissing<T>(this ElasticsearchClient client, ref string indexName, out CreateIndexResponse response, bool dropExisting = false) {

            bool created = false;
            response = null;

            if (client is null) throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentNullException(nameof(indexName));

            indexName = indexName.Trim().ToLowerInvariant();

            bool exists = client.Indices.Exists(indexName).Exists;

            if (exists && dropExisting)
            {
                client.Indices.Delete(indexName);
                exists = false;
            }

            if (!exists)
            {
                CreateIndexRequest cir = new(indexName);
                response = client.Indices.Create(cir);
                created = true;
            }

            return created;
        }

    }
}
