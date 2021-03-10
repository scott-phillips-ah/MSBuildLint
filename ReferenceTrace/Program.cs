using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ReferenceTrace.MSProject;
using ReferenceTrace.NuSpec;
using ShellProgressBar;

namespace ReferenceTrace
{
    internal class Program
    {
        private static ProgressBarOptions DefaultOptions => new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.DarkGray,
            BackgroundCharacter = '\u2593',
            DisplayTimeInRealTime = false
        };

        private static List<string> _nugetCachePaths;

        private static BasicCache<PackageReference, Package> PackageCache =
            new BasicCache<PackageReference, Package>(LoadPackageFromNugetCache);

        private static BasicCache<string, Project> ProjectCache =
            new BasicCache<string, Project>(x => Extensions.ProjectXmlSerializer.Deserialize<Project>(x));

        private static void Main(string[] args)
        {
            Console.WriteLine("Parsing command line...");
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();

            _nugetCachePaths = config["nugetcache"].Split(';').ToList();
            ;

            var solution = ParseSolutionFile(config["solutionfile"]);

            var removePackages = new Dictionary<Project, HashSet<string>>();

            using (var pbar = new ProgressBar(solution.Projects.Count, "Parsing project files", DefaultOptions))
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
                var newProject = ProjectCache[newProjectPath];
                newProject.FilePath = newProjectPath;

                GetUnnecessaryReferences(newProject, allReferencedPackages);
            }

            // Get nuget reference - investigate those
            AddNugetDependencies(packageReferences, allReferencedPackages);

            return packageReferences.Select(x => x.NugetName).Intersect(allReferencedPackages)
                .ToHashSet();
        }

        private static void AddNugetDependencies(IEnumerable<PackageReference> packageReferences,
            HashSet<string> allReferencedPackages)
        {
            foreach (var nugetReference in packageReferences)
            {
                var nuspecObject = PackageCache[nugetReference];
                if (nuspecObject == null) continue;

                var dependencies = nuspecObject.Metadata?.Dependencies?.Group?.Select(x => x.Dependency)
                    ?.RemoveNulls()
                    ?.Select(x => new PackageReference {Include = x.Id, Version = x.Version})
                    ?.ToList() ?? new List<PackageReference>();
                allReferencedPackages.AddRange(dependencies.Select(x => x.NugetName));
                // Recursive check.
                // AddNugetDependencies(dependencies, allReferencedPackages);
            }
        }

        private static Package LoadPackageFromNugetCache(PackageReference nugetReference)
        {
            // Find a local directory which matches
            var nuspecPaths = _nugetCachePaths.Select(x => Path.Combine(x, nugetReference.NugetName)).ToList();
            var nugetRange = nugetReference.Version.ToVersionRange();
            var matchDir = nuspecPaths.SelectMany(path =>
                    Directory.Exists(path) ? new DirectoryInfo(path).GetDirectories() : new DirectoryInfo[0])
                .FirstOrDefault(x =>
                    nugetRange.Satisfies(x.Name.ToNugetVersion()));
            if (matchDir?.FullName == null)
                return null;
            var nuspecFile = Path.Combine(matchDir?.FullName, $"{nugetReference.NugetName}.nuspec");

            var nuspecObject = Extensions.PackageXmlSerializer.Deserialize<Package>(nuspecFile);
            return nuspecObject;
        }

        private static SolutionFile ParseSolutionFile(string solutionPath) => new SolutionFile(solutionPath);
    }
}