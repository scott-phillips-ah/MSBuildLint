using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReferenceTrace.MSProject;
using ReferenceTrace.NuSpec;
using ShellProgressBar;

namespace ReferenceTrace
{
    public class ReferenceTracer
    {
        private readonly List<string> _nugetCachePaths;
        private readonly string _solutionFilePath;
        private readonly BasicCache<PackageReference, Package> _packageCache;
        private readonly BasicCache<string, Project> _projectCache;
        private readonly BasicCache<Project, HashSet<string>> _projectReferenceCache;

        public ReferenceTracer(List<string> nugetCachePaths, string solutionFilePath)
        {
            _nugetCachePaths = nugetCachePaths;
            _solutionFilePath = solutionFilePath;
            _packageCache = new BasicCache<PackageReference, Package>(LoadPackageFromNugetCache);
            _projectCache =
                new BasicCache<string, Project>(x =>
                {
                    var project =  Extensions.ProjectXmlSerializer.Deserialize<Project>(x);
                    project.FilePath = x;
                    return project;
                });
            _projectReferenceCache = new BasicCache<Project, HashSet<string>>(GetReferences);
        }

        public void FindUnnecessaryReferences()
        {
            var solution = ParseSolutionFile(_solutionFilePath);
            var removePackages = new Dictionary<string, HashSet<string>>();

            using (var pbar = new ProgressBar(solution.Projects.Count, "Parsing project files", Program.DefaultProgressBarOptions))
            {
                foreach (var project in solution.Projects)
                {
                    pbar.Tick();
                    removePackages[project.FilePath] = GetUnnecessaryReferences(project);
                }
            }

            // Print results
            Console.WriteLine($"Project: Package");
            foreach (var (project, packages) in removePackages.Where(x => x.Value.Count > 0).OrderBy(x => x.Key))
            {
                Console.WriteLine($"{project}:");
                foreach (var removePackage in packages.OrderBy(x => x))
                    Console.WriteLine($"     {removePackage}");
            }
        }

        private HashSet<string> GetReferences(Project project)
        {
            var references = new HashSet<string>();
            var (projectReferences, packageReferences) = GetProjectAndPackageReferences(project);
            // Get project references - investigate those
            references.AddRange(projectReferences.Select(x => x.FilePath));
            references.AddRange(packageReferences.Select( x => x.NugetName));

            return references;
        }

        private (HashSet<Project> projectReferences, HashSet<PackageReference> packageReferences) GetProjectAndPackageReferences(Project project)
        {
            var projectReferences = project.ItemGroup.SelectMany(x => x.ProjectReference)
                .Select(y => _projectCache[y.IncludeFullPath(project)]).ToHashSet();
            var packageReferences = project.ItemGroup.SelectMany(x => x.PackageReference).ToHashSet();
            return (projectReferences, packageReferences);
        }

        private HashSet<string> GetUnnecessaryReferences(Project project)
        {
            var topLevelReferences = _projectReferenceCache[project];
            var (projectReferences, packageReferences) = GetProjectAndPackageReferences(project);
            var implicitReferences = projectReferences.SelectMany(x => _projectReferenceCache[x]).ToHashSet();

            var impliedProjects = GetProjectDependencies(projectReferences).Except(projectReferences).ToHashSet();
            packageReferences.AddRange(impliedProjects.SelectMany(p =>
            {
                var (_, pkgRefs) = GetProjectAndPackageReferences(p);
                return pkgRefs;
            }));
            
            implicitReferences.AddRange(impliedProjects.Select(x => x.FilePath));
            implicitReferences.AddRange(GetNugetDependencies(packageReferences));

            return implicitReferences.Intersect(topLevelReferences).ToHashSet();
        }

        private IEnumerable<Project> GetProjectDependencies(IEnumerable<Project> referencedProjects)
        {
            var allProjects = new HashSet<Project>();
            foreach (var project in referencedProjects)
            {
                // Self
                allProjects.Add(project);
                // Children
                var (projectReferences, _) = GetProjectAndPackageReferences(project);

                allProjects.AddRange(GetProjectDependencies(projectReferences)); 
            }

            return allProjects;
        }

        private IEnumerable<string> GetNugetDependencies(IEnumerable<PackageReference> packageReferences)
        {
            var allReferencedPackages = new HashSet<string>();
            foreach (var nugetReference in packageReferences)
            {
                var nuspecObject = _packageCache[nugetReference];
                if (nuspecObject == null) continue;

                var dependencies = nuspecObject.GetPackageReferences().ToList();
                allReferencedPackages.AddRange(dependencies.Select(x => x.NugetName));
                // Recursive check.
                allReferencedPackages.AddRange(GetNugetDependencies(dependencies));
            }

            return allReferencedPackages;
        }

        private Package LoadPackageFromNugetCache(PackageReference nugetReference)
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