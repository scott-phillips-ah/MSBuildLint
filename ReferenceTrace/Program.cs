using System;
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

        private static int FormatProjectFiles(ProjectFormatOptions options)
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

                    xmlFile.Save(project.FilePath);
                }
            }
            
            return 0;
        }

        private static int TestParallelDependency(ParallelTestOptions options)
        {
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