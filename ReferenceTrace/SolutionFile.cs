using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using ReferenceTrace.MSProject;

namespace ReferenceTrace
{
    interface IFileLoadSave
    {
        public void Load(Queue<string> sourceLines);
        public void Save(Queue<string> sourceLines);
    }
    class SolutionFile : IFileLoadSave
    {
        public string SolutionPath { get; set; }

        public string Version { get; set; }
        public string VisualStudioVersion { get; set; }
        public string MinimumVisualStudioVersion { get; set; }

        public List<SolutionProject> SolutionProjects { get; set; } = new List<SolutionProject>();
        public List<GlobalSection> GlobalSections { get; set; } = new List<GlobalSection>();

        private List<Project> _projects;
        public List<Project> Projects
        {
            get
            {
                if (_projects == null)
                {
                    _projects = new List<Project>();
                    foreach (var solutionProject in SolutionProjects)
                    {
                        if (solutionProject.Path.EndsWith(".csproj"))
                        {
                            // Load the actual project instances.
                            var projPath =
                                Path.Combine(Path.GetDirectoryName(this.SolutionPath) ?? "", solutionProject.Path);
                            using var projectStream = File.OpenRead(projPath);
                            var newProject = (Project) Extensions.ProjectXmlSerializer.Deserialize(projectStream);
                            newProject.FilePath = projPath;
                            _projects.Add(newProject);
                        }
                    }
                }

                return _projects;
            }
        }

        public SolutionFile(string path = null)
        {
            this.SolutionPath = path;
        }

        // Regex patterns
        private const string VersionPattern = @"Version (?<version>\d*\.\d*)";

        public void Load(Queue<string> sourceLines)
        {
            // Parse line by line
            while (sourceLines.Count > 0)
            {
                string slnLine = sourceLines.Peek()?.Trim();
                if (String.IsNullOrWhiteSpace(slnLine) || slnLine.StartsWith("# ")) goto removeLine;

                if (slnLine.StartsWith("Microsoft Visual Studio Solution File"))
                {
                    // Parse the version information.
                    var match = Regex.Matches(slnLine, VersionPattern).First();
                    this.Version = match.Groups["version"].Value;
                }

                if (slnLine.StartsWith("VisualStudioVersion"))
                {
                    this.VisualStudioVersion =
                        slnLine.Substring(slnLine.IndexOf("=", StringComparison.Ordinal) + 1).Trim();
                }

                if (slnLine.StartsWith("MinimumVisualStudioVersion"))
                {
                    this.MinimumVisualStudioVersion =
                        slnLine.Substring(slnLine.IndexOf("=", StringComparison.Ordinal) + 1).Trim();
                }

                if ("Global".Equals(slnLine))
                {
                    // Find the entire section.
                    sourceLines.Dequeue(); // Dump the opening header
                    while (!"EndGlobal".Equals(sourceLines.Peek()?.Trim()))
                    {
                        var newSection = new GlobalSection();
                        newSection.Load(sourceLines);
                        GlobalSections.Add(newSection);
                    }
                }

                if (slnLine.StartsWith("Project"))
                {
                    // Parse a project
                    var newProject = new SolutionProject();
                    newProject.Load(sourceLines);
                    SolutionProjects.Add(newProject);
                }
                // ReSharper disable once BadControlBracesIndent
            removeLine:
                // Remove the next line.
                sourceLines.Dequeue();
            }
        }

        public void Save(Queue<string> sourceLines)
        {
            // Blank line
            sourceLines.Enqueue("");
            // format
            sourceLines.Enqueue($"Microsoft Visual Studio Solution File, Format Version {Version}");
            // comment
            sourceLines.Enqueue($"# Visual Studio Version {VisualStudioVersion.Substring(0,VisualStudioVersion.IndexOf(".", StringComparison.Ordinal))}");
            // version information
            sourceLines.Enqueue($"VisualStudioVersion = {VisualStudioVersion}");
            sourceLines.Enqueue($"MinimumVisualStudioVersion = {MinimumVisualStudioVersion}");
            // projects
            foreach (var project in SolutionProjects)
                project.Save(sourceLines);
            // global sections
            sourceLines.Enqueue("Global");
            foreach (var section in GlobalSections)
                section.Save(sourceLines);
            sourceLines.Enqueue("EndGlobal");
        }
    }

    class GlobalSection : IFileLoadSave
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
            this.Type = Enum.Parse<GlobalSectionType>(headerMatch.Groups["type"].Value);
            this.Location = Enum.Parse<SolutionLocation>(headerMatch.Groups["location"].Value);

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
            sourceLines.Enqueue($"\tEndGlobalSection");
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
            var platform = new ConfigurationPlatform()
            {
                Configuration = match.Groups["configuration"].Value,
                Platform = match.Groups["platform"].Value,
                Suffix = match.Groups["suffix"].Value
            };
            var tmpGuid = Guid.Empty;
            Guid.TryParse(match.Groups["guid"].Value, out tmpGuid);
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

    class SolutionProject : IFileLoadSave
    {
        public Guid ProjectGuid { get; set; } = Guid.Empty;
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public Guid ParentGuid { get; set; } = Guid.Empty;
        public List<ProjectSection> Sections { get; set; } = new List<ProjectSection>();

        private const string ProjectHeaderPattern =
            @"Project\(\""{(?<parentguid>[A-F0-9\-]+)}\""\)\s*=\s*\""(?<projectname>[\.\w]+)\""\s*,\s*\""(?<projectpath>[\w\.\\\/]+)\""\s*,\s*\""(?<projectguid>{[A-F0-9\-]+})\""";

        public void Load(Queue<string> sourceLineQueue)
        {
            string headerLine = sourceLineQueue.Dequeue()?.Trim();
            var match = Regex.Matches(headerLine, ProjectHeaderPattern).First();
            this.ProjectGuid = Guid.Parse(match.Groups["projectguid"].Value);
            this.Name = match.Groups["projectname"].Value;
            this.Path = match.Groups["projectpath"].Value;
            this.ParentGuid = Guid.Parse(match.Groups["parentguid"].Value);

            while (!"EndProject".Equals(sourceLineQueue.Peek()?.Trim()))
            {
                // Consume the internals
                var newSection = new ProjectSection();
                newSection.Load(sourceLineQueue);
                this.Sections.Add(newSection);
            }
        }

        public void Save(Queue<string> sourceLines)
        {
            // Header
            sourceLines.Enqueue($"Project(\"{{{ParentGuid.ToString().ToUpperInvariant()}}}\") = \"{Name}\", \"{Path}\", \"{{{ProjectGuid.ToString().ToUpperInvariant()}}}\"");
            // Contents
            foreach (var section in Sections)
                section.Save(sourceLines);
            // Footer
            sourceLines.Enqueue("EndProject");
        }
    }

    class ProjectSection : IFileLoadSave
    {
        public string Type { get; set; }
        public string Location { get; set; }
        public Dictionary<string, string> AssignmentPairs { get; set; } = new Dictionary<string, string>();

        private const string HeaderPattern = @"ProjectSection\((?<type>\w*)\)(\s*\=\s*)(?<location>\w*)";

        public void Load(Queue<string> sourceLineQueue)
        {
            string headerLine = sourceLineQueue.Dequeue().Trim();
            var headerMatch = Regex.Matches(headerLine, HeaderPattern).First();
            this.Type = headerMatch.Groups["type"].Value;
            this.Location = headerMatch.Groups["location"].Value;

            // Load the data.
            string assignmentLine;
            while (!"EndProjectSection".Equals(assignmentLine = sourceLineQueue.Dequeue()?.Trim()))
            {
                var lineMatch = Regex.Matches(assignmentLine ?? string.Empty, GlobalSection.PairPattern).First();
                this.AssignmentPairs[lineMatch.Groups["source"].Value] = lineMatch.Groups["target"].Value;
            }
        }

        public void Save(Queue<string> sourceLines)
        {
            // Header
            sourceLines.Enqueue($"\tProjectSection({Type}) = {Location}");
            // Contents
            foreach (var assignment in AssignmentPairs)
                sourceLines.Enqueue($"\t\t{assignment.Value} = {assignment.Key}");
            // Footer
            sourceLines.Enqueue($"\tEndProjectSection");
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
