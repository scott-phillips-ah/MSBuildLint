# MSBuildLint
Clean up the MSBuild project files in various ways:

1. `cleanreferences --solutionfile \"[full path to .sln file]\" --nugetcache \"C:\\Users\\[username]\\.nuget\\packages;C:\\Program Files\\dotnet\\packs\""
2. `projectformat --solutionfile "[full path to .sln file]"`
3. `paralleltest --solutionfile "C:\work\MSBuildLint\ReferenceTrace.sln" --TestRuns "15" --ReportFile ".\parallel_report.xml"`

## Clean References
Provides a list of references, sorted by project, which can be removed as duplicates. Please note that some package references (such as `<PackageReference Include="Microsoft.Net.Test.Sdk" Version="16.9.1" />`) need to be included in **every** (test) project. Don't remove these references, or unit testing will fail.

## Project Format
This provides the ability to format project files and organize package references alphabetically. I'm still working on formatting instructions to more closely mimic the default Visual Studio/Rider project format. Open to PRs and feedback on this.

## Parallel Test
This runs `dotnet test` a defined `testruns` number of times and reads the results (`.trx`) files. It then looks through the output files to determine which unit tests can fail if run in parallel. This does require that the solution file in question does **not** have parallel testing turned off via an attribute.
