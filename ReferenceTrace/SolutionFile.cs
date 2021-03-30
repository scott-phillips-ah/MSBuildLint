using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ReferenceTrace.MSProject;

namespace ReferenceTrace
{
    internal class SolutionFile
    {
        private string SolutionPath { get; set; }

        private List<SolutionProject> SolutionProjects { get; }

        public List<Project> Projects { get; }

        private IEnumerable<string> Lines => File.ReadAllLines(SolutionPath).ToList();

        public SolutionFile(string path)
        {
            SolutionPath = path;
            SolutionProjects = LoadSolutionProjects().ToList();
            Projects = LoadProjects().ToList();
        }

        private IEnumerable<SolutionProject> LoadSolutionProjects()
        {
            return Lines.Where(line => line.StartsWith("Project")).Select(line => new SolutionProject(line));
        }
        private IEnumerable<Project> LoadProjects()
        {
            foreach (var solutionProject in SolutionProjects)
            {
                if (!solutionProject.Path.EndsWith(".csproj")) continue;
                // Load the actual project instances.
                var projPath =
                    Path.Combine(Path.GetDirectoryName(SolutionPath) ?? "", solutionProject.Path);
                var newProject = Extensions.ProjectXmlSerializer.Deserialize<Project>(projPath);
                newProject.FilePath = projPath;
                yield return newProject;
            }
        }
    }

    class GlobalSection
    {
        public GlobalSectionType Type { get; set; }
        public SolutionLocation Location { get; set; }

        public Dictionary<ConfigurationPlatform, ConfigurationPlatform> ConfigurationPairs { get; set; } =
            new Dictionary<ConfigurationPlatform, ConfigurationPlatform>();

        private const string HeaderPattern = @"GlobalSection\((?<type>\w*)\)\s*=\s*(?<location>\w*)";
        public const string PairPattern = @"(?<target>.*?)(?<assignment>\s*\=\s*)(?<source>.*)";

        public void Load(Queue<string> sourceLines)
        {
            var headerLine = sourceLines.Dequeue()?.Trim();
            var headerMatch = Regex.Matches(headerLine, HeaderPattern).First();
            Type = Enum.Parse<GlobalSectionType>(headerMatch.Groups["type"].Value);
            Location = Enum.Parse<SolutionLocation>(headerMatch.Groups["location"].Value);

            string assignmentLine;
            while (!"EndGlobalSection".Equals(assignmentLine = sourceLines.Dequeue()?.Trim()))
            {
                var lineMatch = Regex.Matches(assignmentLine, PairPattern).First();
                // Add the internal pairs
                var targetConfiguration = ConfigurationPlatform.Parse(lineMatch.Groups["target"].Value);
                var sourceConfiguration = ConfigurationPlatform.Parse(lineMatch.Groups["source"].Value);

                ConfigurationPairs[sourceConfiguration] = targetConfiguration;
            }
        }

        public void Save(Queue<string> sourceLines)
        {
            // Header
            sourceLines.Enqueue($"\tGlobalSection({Type}) = {Location}");
            // Contents
            foreach (var assignment in ConfigurationPairs)
                sourceLines.Enqueue($"\t\t{assignment.Value} = {assignment.Key}");
            // Footer
            sourceLines.Enqueue("\tEndGlobalSection");
        }
    }

    class ConfigurationPlatform
    {
        public string Configuration { get; set; } = "Debug";
        public string Platform { get; set; } = "Any CPU";
        public string Suffix { get; set; } = "";
        public Guid ProjectGuid { get; set; } = Guid.Empty;

        private const string ConfigurationPattern = @"({(?<guid>[0-9A-F\-]*)})?\.?((?<configuration>\w*)\|(?<platform>[\w\ ]*)\.?)?(?<suffix>.*)";

        public static ConfigurationPlatform Parse(string source)
        {
            var match = Regex.Matches(source, ConfigurationPattern).First();
            var platform = new ConfigurationPlatform
            {
                Configuration = match.Groups["configuration"].Value,
                Platform = match.Groups["platform"].Value,
                Suffix = match.Groups["suffix"].Value
            };
            Guid.TryParse(match.Groups["guid"].Value, out var tmpGuid);
            platform.ProjectGuid = tmpGuid;
            return platform;
        }

        public override string ToString()
        {
            // Empty sections should be removed.
            var value = $"{(ProjectGuid.Equals(Guid.Empty) ? "" : "{"+ProjectGuid.ToString().ToUpperInvariant()+"}")}.{Configuration}|{Platform}.{Suffix}".Trim('.').Trim('|').Trim('.');

            return value;
        }
    }

    class SolutionProject
    {
        public Guid ProjectGuid { get; set; } = Guid.Empty;
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public Guid ParentGuid { get; set; } = Guid.Empty;

        private const string ProjectHeaderPattern =
            @"Project\(\""{(?<parentguid>[A-F0-9\-]+)}\""\)\s*=\s*\""(?<projectname>[\w\s\.]+)\""\s*,\s*\""(?<projectpath>[\w\s\.\\]+)\""\s*,\s*\""(?<projectguid>{[A-F0-9\-]+})\""";

        public SolutionProject(string headerLine) { Load(headerLine);}

        public void Load(string headerLine)
        {
            var match = Regex.Matches(headerLine.Trim(), ProjectHeaderPattern).First();
            ProjectGuid = Guid.Parse(match.Groups["projectguid"].Value);
            Name = match.Groups["projectname"].Value;
            Path = match.Groups["projectpath"].Value;
            ParentGuid = Guid.Parse(match.Groups["parentguid"].Value);
        }
    }
    
    enum SolutionLocation
    {
        preSolution,
        postSolution
    }
    enum GlobalSectionType
    {
        SharedMSBuildProjectFiles,
        SolutionConfigurationPlatforms,
        ProjectConfigurationPlatforms,
        SolutionProperties,
        NestedProjects,
        ExtensibilityGlobals
    }
}
