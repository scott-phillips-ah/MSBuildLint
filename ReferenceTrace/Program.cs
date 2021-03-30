using System;
using System.Linq;
using CommandLine;
using ShellProgressBar;

namespace ReferenceTrace
{
    internal class Program
    {
        public static ProgressBarOptions DefaultProgressBarOptions => new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.DarkGray,
            BackgroundCharacter = '\u2593',
            DisplayTimeInRealTime = false
        };

        [Verb("clean")]
        public class CleanReferencesOptions
        {
            [Option(Default=false, HelpText = "Solution file to parse")]
            public string SolutionFile { get; set; }
            
            [Option(Default=false, HelpText = "Nuget cache locations")]
            public string NugetCache { get; set; }
        }

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CleanReferencesOptions>(args)
                .MapResult(CleanReferences, errs => -1);
        }
        private static int CleanReferences(CleanReferencesOptions options)
        {
        Console.WriteLine("Parsing command line...");

            var nugetCachePaths = options.NugetCache.Split(';').ToList();
            var solutionFilePath = options.SolutionFile;

            var tracer = new ReferenceTracer(nugetCachePaths, solutionFilePath);
            tracer.FindUnnecessaryReferences();

            return 0;
        }
    }
}