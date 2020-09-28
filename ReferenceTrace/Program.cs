using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using ReferenceTrace.MSProject;
using ReferenceTrace.NuSpec;
using ShellProgressBar;


namespace ReferenceTrace
{
    class Program
    {
        private ProgressBarOptions DefaultOptions => new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.DarkGray,
            BackgroundCharacter = '\u2593'
        };

        private static void Main(string[] args)
        {
            Console.WriteLine("Parsing command line...");
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();

            var (solution, lines) = ParseSolutionFile(config["solutionfile"]);

            var outputQueue = new Queue<string>();
            solution.Save(outputQueue);

            // Compare save
            AssertLoadSave(lines, outputQueue);

            var missingPackages = new HashSet<string>();
            var removePackages = new Dictionary<Project, HashSet<string>>();

            using (var pbar = new ProgressBar(solution.Projects.Count, "Parsing project files"))
            {
                foreach (var project in solution.Projects)
                {
                    pbar.Tick();
                    var allReferencedPackages = new HashSet<string>();
                    var unnecessaryPackages =
                        GetUnnecessaryReferences(project, config, allReferencedPackages, missingPackages, pbar);
                    removePackages[project] = unnecessaryPackages;
                }
            }

            // Print results
            foreach (var project in removePackages.Keys)
            foreach (var removePackage in removePackages[project])
                Console.Out.WriteLine($"Duplicate package {removePackage} can be removed from {project.FilePath}");

            ApplyChanges(removePackages);
        }

        private static void ApplyChanges(Dictionary<Project, HashSet<string>> removePackages)
        {
            // Apply changes.
            Console.WriteLine("Applying changes...");
            foreach (var project in removePackages.Keys)
            {
                var packageList = removePackages[project];
                if (packageList.Count == 0) continue;

                foreach (var removePackage in packageList)
                {
                    var groups = project.ItemGroup.Where(ig =>
                        ig.PackageReference.Select(pr => pr.Include.ToLowerInvariant()).Contains(removePackage));
                    foreach (var itemGroup in groups)
                        itemGroup.PackageReference.RemoveAll(x => x.Include.ToLowerInvariant().Equals(removePackage));
                }

                File.Delete(project.FilePath);

                // Configure writing parameters
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                var settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;

                using var writer = XmlWriter.Create(project.FilePath, settings);
                Extensions.ProjectXmlSerializer.Serialize(writer, project, ns);
            }
        }

        private static HashSet<string> GetUnnecessaryReferences(Project project, IConfigurationRoot config,
            HashSet<string> allReferencedPackages, HashSet<string> missingPackages, ProgressBarBase pbar)
        {
            var projectReferences = project.ItemGroup.SelectMany(x => x.ProjectReference).ToList();
            var packageReferences = project.ItemGroup.SelectMany(x => x.PackageReference).ToList();

            // Get project references - investigate those
            using (var pbar2 = pbar.Spawn(projectReferences.Count, $"{Path.GetFileName(project.FilePath)}: get unnecessary references"))
            {
                foreach (var referencedProject in projectReferences)
                {
                    pbar2.Tick();
                    
                    var newProjectPath =
                        Path.Combine(Path.GetDirectoryName(project.FilePath) ?? string.Empty,
                            referencedProject.Include);
                    using var inStream = File.OpenRead(newProjectPath);
                    var newProject = (Project) Extensions.ProjectXmlSerializer.Deserialize(inStream);
                    newProject.FilePath = newProjectPath;

                    GetUnnecessaryReferences(newProject, config, allReferencedPackages, missingPackages, pbar2);
                }
            }

            // Get nuget reference - investigate those
            AddNugetDependencies(packageReferences, config, allReferencedPackages, missingPackages);

            return packageReferences.Select(x => x.Include.ToLowerInvariant()).Intersect(allReferencedPackages)
                .ToHashSet();
        }

        private static void AddNugetDependencies(IEnumerable<PackageReference> packageReferences,
            IConfigurationRoot config,
            HashSet<string> allReferencedPackages, HashSet<string> missingPackages)
        {
            foreach (var nugetReference in packageReferences)
            {
                // Deserialize the nuspec file
                var nugetName = nugetReference.Include.ToLowerInvariant();
                var nuspecPaths = Path.Combine(config["nugetcache"], nugetName).Split(';');
                if (nuspecPaths.Any(missingPackages.Contains)) continue;
                // Find a local directory which matches
                var nugetRange = nugetReference.Version.ToVersionRange();
                try
                {
                    var matchDir = nuspecPaths.SelectMany(path => new DirectoryInfo(path).GetDirectories())
                        .FirstOrDefault(x =>
                            nugetRange.Satisfies(x.Name.ToNugetVersion()));
                    var nuspecFile = Path.Combine(matchDir?.FullName ?? throw new IOException(), $"{nugetName}.nuspec");


                    using var nuspecStream = File.OpenRead(nuspecFile);
                    var xmlReader = new XmlTextReader(nuspecStream) {Namespaces = false};
                    var nuspecObject = (Package) Extensions.PackageXmlSerializer.Deserialize(xmlReader);

                    var dependencies = nuspecObject.Metadata?.Dependencies?.Group?.Select(x => x.Dependency)
                        ?.RemoveNulls()
                        ?.ToList() ?? Array.Empty<Dependency>().ToList();
                    allReferencedPackages.AddRange(dependencies.Select(x => x.Id.ToLowerInvariant()));
                    // Recursive check.
                    AddNugetDependencies(
                        dependencies.Select(x => new PackageReference() {Include = x.Id, Version = x.Version}), config,
                        allReferencedPackages, missingPackages);
                }
                catch (IOException)
                {
                    // Console.Error.WriteLine($"Could not find nuget file in: {nuspecPaths}");
                    missingPackages.Add(nugetName);
                }
            }
        }

        private static void AssertLoadSave(Queue<string> lines, Queue<string> outputQueue)
        {
            Debug.Assert(lines.Count == outputQueue.Count, "Matching number of lines");
            for (int ij = 0; ij < lines.Count; ij++)
            {
                var expected = lines.Dequeue();
                var actual = outputQueue.Dequeue();
                // Skip the comments.
                if (expected.StartsWith("#") && actual.StartsWith("#")) continue;
                Debug.Assert(expected.Trim().Equals(actual.Trim()), $"Expected `{expected}`\nActual `{actual}`");
            }
        }

        private static (SolutionFile, Queue<string>) ParseSolutionFile(string solutionPath)
        {
            Console.WriteLine("Loading solution file...");
            var slnFile = new SolutionFile(solutionPath);
            var allLines = File.ReadAllLines(solutionPath).ToList();
            while (String.IsNullOrWhiteSpace(allLines.First()))
                allLines.RemoveAt(0);

            var lineQueue = new Queue<string>(allLines);
            slnFile.Load(lineQueue);
            return (slnFile, new Queue<string>(allLines));
        }
    }
}