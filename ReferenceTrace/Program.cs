using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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

        public class BaseOptions
        {
            [Option(Required = true, HelpText = "Solution file to parse")]
            public string SolutionFile { get; set; }
        }

        [Verb("cleanreferences", HelpText = "Clean references")]
        public class CleanReferencesOptions : BaseOptions
        {
            [Option(Required = true, HelpText = "Nuget cache locations")]
            public string NugetCache { get; set; }
        }

        [Verb("paralleltest", HelpText = "Identify parallel dependency tests")]
        public class ParallelTestOptions : BaseOptions
        {
            [Option(Default = 20, HelpText = "Number of runs to perform")]
            public int TestRuns { get; set; }
            [Option(Required = true, HelpText = "Report file to generate")]
            public string ReportFile { get; set; }
        }
        
        [Verb("projectformat", HelpText = "Format project files")]
        public class ProjectFormatOptions : BaseOptions
        {
        }

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CleanReferencesOptions, ParallelTestOptions, ProjectFormatOptions>(args)
                .MapResult(
                    (CleanReferencesOptions options) => CleanReferences(options), 
                    (ParallelTestOptions options) => TestParallelDependency(options),
                    (ProjectFormatOptions options) => FormatProjectFiles(options),
                    errs => -1);
        }

        private static int FormatProjectFiles(BaseOptions options)
        {
            var slnFile = new SolutionFile(options.SolutionFile);

            foreach (var project in slnFile.Projects)
            {
                // Load the XML data
                var xmlFile = XDocument.Load(project.FilePath);
                // Find each itemgroup
                if (xmlFile.Root == null) continue;
                foreach (var itemGroup in xmlFile.Root.Descendants("ItemGroup"))
                {
                    var packageReferences = itemGroup.Descendants("PackageReference").OrderBy(e => e.Name.LocalName)
                        .ToList();
                    // Remove and re-add to order
                    foreach (var reference in packageReferences)
                        reference.Remove();
                    foreach (var reference in packageReferences)
                        itemGroup.Add(reference);

                    // TODO - Allow for other save formats
                    xmlFile.Save(project.FilePath);
                }
            }
            
            return 0;
        }

        private static int TestParallelDependency(ParallelTestOptions options)
        {
            var solutionDirectory = Path.GetDirectoryName(options.SolutionFile);
            var testResultPath = Path.GetFullPath(Path.Join(solutionDirectory, "TestResults"));
            // TODO - Clear the results path?
            // Load the solution file
            // Execute `dotnet test` as many times are necessary
            for (var runNumber = 0; runNumber < options.TestRuns; runNumber++)
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"test \"{options.SolutionFile}\" --no-build --logger trx --results-directory \"{testResultPath}\"",
                    WorkingDirectory = solutionDirectory ?? string.Empty,
                };
                var processResult = Process.Start(processStartInfo);
                processResult?.WaitForExit();
            }
            // Read all the .trx file results
            var trxFiles = Directory.GetFiles(testResultPath, "*.trx");
            var trxData = trxFiles.Select(x => TrxTools.TrxParser.TrxControl.ReadTrx(new StreamReader(x))).ToList();

            // Parse the TRX data for all the failed runs
            var failedRuns = trxData.Where(tr => tr.ResultSummary.Outcome.ToLowerInvariant().Equals("failed"));
            var failedTests = failedRuns.SelectMany(tr => tr.Results.Where(
                utr => utr.Outcome.ToLowerInvariant().Equals("failed"))).GroupBy(
                x => x.TestName).Select(y => y.First()).OrderBy(x => x.TestName);

            foreach (var testResult in failedTests)
            {
                Console.WriteLine($"{testResult.TestName}: {testResult.Outcome}");
            }
            // Report back
            return 0;
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