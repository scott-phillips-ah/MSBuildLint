using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using LazyCache;
using Microsoft.Extensions.Configuration;
using ReferenceTrace.MSProject;
using ReferenceTrace.NuSpec;
using ShellProgressBar;

namespace ReferenceTrace
{
    internal class Program
    {
        private ProgressBarOptions DefaultOptions => new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.DarkGray,
            BackgroundCharacter = '\u2593'
        };

        private static CachingService NugetCache = new CachingService();
        private static HashSet<string> MissingPackages = new HashSet<string>();

        private static string _nugetCacheLocation;
        private static IEnumerable<string> NugetCachePaths => _nugetCacheLocation.Split(';');

        private static void Main(string[] args)
        {
            Console.WriteLine("Parsing command line...");
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();

            _nugetCacheLocation = config["nugetcache"];

            var solution = ParseSolutionFile(config["solutionfile"]);
            
            var removePackages = new Dictionary<Project, HashSet<string>>();

            using (var pbar = new ProgressBar(solution.Projects.Count, "Parsing project files"))
            {
                foreach (var project in solution.Projects)
                {
                    pbar.Tick();
                    removePackages[project] = GetUnnecessaryReferences(project, new HashSet<string>());
                }
            }

            // Print results
            foreach (var (project, packages) in removePackages)
            foreach (var removePackage in packages)
                Console.WriteLine($"Duplicate package {removePackage} can be removed from {project.FilePath}");
        }

        private static HashSet<string> GetUnnecessaryReferences(Project project, HashSet<string> allReferencedPackages)
        {
            var projectReferences = project.ItemGroup.SelectMany(x => x.ProjectReference).ToList();
            var packageReferences = project.ItemGroup.SelectMany(x => x.PackageReference).ToList();

            // Get project references - investigate those
            foreach (var newProjectPath in projectReferences.Select(referencedProject => Path.Combine(
                Path.GetDirectoryName(project.FilePath) ?? string.Empty,
                referencedProject.Include)))
            {
                var newProject = Extensions.ProjectXmlSerializer.Deserialize<Project>(newProjectPath);
                newProject.FilePath = newProjectPath;

                GetUnnecessaryReferences(newProject, allReferencedPackages);
            }

            // Get nuget reference - investigate those
            AddNugetDependencies(packageReferences, allReferencedPackages);

            return packageReferences.Select(x => x.Include.ToLowerInvariant()).Intersect(allReferencedPackages)
                .ToHashSet();
        }
        
        private static void AddNugetDependencies(IEnumerable<PackageReference> packageReferences,
            HashSet<string> allReferencedPackages)
        {
            foreach (var nugetReference in packageReferences)
            {
                // Deserialize the nuspec file
                var nugetName = nugetReference.Include.ToLowerInvariant();
                var nuspecPaths = NugetCachePaths.Select(x => Path.Combine(x, nugetName)).ToList();
                if (nuspecPaths.Any(MissingPackages.Contains)) continue;
                // Find a local directory which matches
                var nugetRange = nugetReference.Version.ToVersionRange();
                try
                {
                    var matchDir = nuspecPaths.SelectMany(path => new DirectoryInfo(path).GetDirectories())
                        .FirstOrDefault(x =>
                            nugetRange.Satisfies(x.Name.ToNugetVersion()));
                    var nuspecFile = Path.Combine(matchDir?.FullName ?? throw new IOException(), $"{nugetName}.nuspec");

                    var nuspecObject = Extensions.PackageXmlSerializer.Deserialize<Package>(nuspecFile);

                    var dependencies = nuspecObject.Metadata?.Dependencies?.Group?.Select(x => x.Dependency)
                        ?.RemoveNulls()
                        ?.Select(x => new PackageReference {Include = x.Id, Version = x.Version} )
                        ?.ToList() ?? new List<PackageReference>();
                    allReferencedPackages.AddRange(dependencies.Select(x => x.Include.ToLowerInvariant()));
                    // Recursive check.
                    AddNugetDependencies(dependencies, allReferencedPackages);
                }
                catch (IOException)
                {
                    MissingPackages.Add(nugetName);
                }
            }
        }

        private static SolutionFile ParseSolutionFile(string solutionPath) => new SolutionFile(solutionPath);
    }
}