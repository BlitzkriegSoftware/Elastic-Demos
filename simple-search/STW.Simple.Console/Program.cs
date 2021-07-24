using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using STW.Simple.Console.Libs;
using STW.Simple.Console.Models;
using System;
using System.IO;

namespace STW.Simple.Console
{
    class Program
    {
        private static string[] _args { get; set; }

        static void Main(string[] args)
        {
            _args = args;

            System.Console.WriteLine($"{Program.ProgramMetadata.ToString()} {Program.ProgramMetadata.Copyright}");

            Parser.Default.ParseArguments<CommandOptions>(args)
                   .WithParsed<CommandOptions>(o =>
                   {
                       var arguments = CommandLine.Parser.Default.FormatCommandLine<CommandOptions>(o);
                       System.Console.WriteLine($"{Program.ProgramMetadata.Product} {arguments}");

                       var tester = Program.Services.GetService<Workers.ISearchWorker>();
                       tester.Run(o);
                   })
                   .WithNotParsed(errors =>
                   {
                       foreach (var e in errors)
                       {
                           System.Console.WriteLine($"{e.Tag}");
                       }
                       Environment.ExitCode = -1;
                   });
        }

        #region "DI"

        private static IServiceProvider _services;

        public static IServiceProvider Services
        {
            get
            {
                if (_services == null)
                {
                    // Create service collection
                    var serviceCollection = new ServiceCollection();

                    // Build DI Stack inc. Logging, Configuration, and Application
                    ConfigureServices(serviceCollection);

                    // Create service provider
                    _services = serviceCollection.BuildServiceProvider();
                }
                return _services;
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            var configurationBuilder = new ConfigurationBuilder();

            string filename = "config.json";
            if (File.Exists(filename)) configurationBuilder.AddJsonFile(filename);

            if ((_args != null) && (_args.Length > 0)) configurationBuilder.AddCommandLine(_args);

            var config = configurationBuilder.Build();
            services.AddSingleton(config);

            // Logging
            services.AddLogging(loggingBuilder => {
                // This line must be 1st
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                // Console is generically cloud friendly
                loggingBuilder.AddConsole();
            });

            // App to run
            services.AddTransient<Workers.ISearchWorker, Workers.SearchWorker>();
        }

        #endregion

        #region "Assembly Version"

        private static Models.BlitzAssemblyVersionMetadata _blitzassemblyversionmetadata = null;

        /// <summary>
        /// Semantic Version, etc from Assembly Metadata
        /// </summary>
        public static Models.BlitzAssemblyVersionMetadata ProgramMetadata
        {
            get
            {
                if (_blitzassemblyversionmetadata == null)
                {
                    _blitzassemblyversionmetadata = new Models.BlitzAssemblyVersionMetadata();
                    var assembly = typeof(Program).Assembly;
                    foreach (var attribute in assembly.GetCustomAttributesData())
                    {
                        if (!attribute.TryParse(out string value))
                        {
                            value = string.Empty;
                        }
                        var name = attribute.AttributeType.Name;
                        System.Diagnostics.Trace.WriteLine($"{name}, {value}");
                        _blitzassemblyversionmetadata.PropertySet(name, value);
                    }
                }
                return _blitzassemblyversionmetadata;
            }
        }

        #endregion

    }
}
