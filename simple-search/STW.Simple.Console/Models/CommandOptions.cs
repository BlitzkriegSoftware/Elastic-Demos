using CommandLine;

namespace STW.Simple.Console.Models
{
    /// <summary>
    /// Command Line Options
    /// </summary>
    public class CommandOptions
    {

        /// <summary>
        /// Default: Records
        /// </summary>
        public const int RecordsDefault = 100;
        /// <summary>
        /// Default: Query Results to Return
        /// </summary>
        public const int QueryResultsDefault = 10;

        /// <summary>
        /// Verbose Output
        /// </summary>
        [Option('v',"Verbose", Default = true, HelpText = "Verbose Output")]
        public bool Verbose { get; set; }

        /// <summary>
        /// Number of Records to Generate
        /// </summary>
        [Option('r',"Records", Default = RecordsDefault, HelpText = "Records to generate" )]
        public int Records { get; set; }

        /// <summary>
        /// Search Text
        /// </summary>
        [Option('s',"Search", Required = true, HelpText = "Search Text")]
        public string SearchText { get; set; }

        /// <summary>
        /// Query Results to Return
        /// </summary>
        [Option('q',"Query-Results",Default = QueryResultsDefault, HelpText = "Query Results to Return")]
        public int QueryResults { get; set; }

    }
}
