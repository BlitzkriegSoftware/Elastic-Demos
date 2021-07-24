namespace STW.Simple.Console.Workers
{
    /// <summary>
    /// Interface: Search Worker
    /// </summary>
    public interface ISearchWorker
    {
        /// <summary>
        /// Main Entry Point
        /// </summary>
        /// <param name="o">CommandOptions</param>
        public void Run(Models.CommandOptions o);
    }
}
