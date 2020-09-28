/* 
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Xml.Serialization;
using System.Collections.Generic;
namespace ReferenceTrace.MSProject
{
	[XmlRoot(ElementName = "Compile")]
	public class Compile
	{
		[XmlAttribute(AttributeName = "Remove")]
		public string Remove { get; set; }
	}

	[XmlRoot(ElementName = "Content")]
	public class Content
	{
		[XmlAttribute(AttributeName = "Remove")]
		public string Remove { get; set; }
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
        [XmlAttribute(AttributeName = "LinkBase")]
        public string LinkBase { get; set; }
        [XmlAttribute(AttributeName = "CopyToPublishDirectory")]
        public string CopyToPublishDirectory { get; set; }
	}

	[XmlRoot(ElementName = "EmbeddedResource")]
	public class EmbeddedResource
	{
		[XmlAttribute(AttributeName = "Remove")]
		public string Remove { get; set; }
	}

	[XmlRoot(ElementName = "FrameworkReference")]
	public class FrameworkReference
	{
		[XmlAttribute(AttributeName = "Include")]
		public string Include { get; set; }
	}

	[XmlRoot(ElementName = "ItemGroup")]
	public class ItemGroup
	{
		[XmlElement(ElementName = "Compile")]
		public Compile Compile { get; set; }
		[XmlElement(ElementName = "EmbeddedResource")]
		public EmbeddedResource EmbeddedResource { get; set; }
		[XmlElement(ElementName = "FrameworkReference")]
		public FrameworkReference FrameworkReference { get; set; }
		[XmlElement(ElementName = "None")]
		public None None { get; set; }
		[XmlElement(ElementName = "PackageReference")]
		public List<PackageReference> PackageReference { get; set; }
		[XmlElement(ElementName = "ProjectReference")]
		public List<ProjectReference> ProjectReference { get; set; }
        [XmlElement(ElementName = "DotNetCliToolReference")]
        public List<DotNetCliToolReference> DotNetCliToolReference { get; set; }
        [XmlElement(ElementName = "Folder")]
        public List<Folder> Folder { get; set; }
        [XmlElement(ElementName = "Content")]
        public List<Content> Content { get; set; }
		[XmlElement(ElementName = "ItemsToCopy")]
        public ItemsToCopy ItemsToCopy { get; set; }
	}

    [XmlRoot(ElementName = "DotNetCliToolReference")]
    public class DotNetCliToolReference
	{
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
        [XmlAttribute(AttributeName = "Version")]
        public string Version { get; set; }
    }

	[XmlRoot(ElementName = "None")]
	public class None
	{
		[XmlAttribute(AttributeName = "Remove")]
		public string Remove { get; set; }
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
        [XmlAttribute(AttributeName = "LinkBase")]
        public string LinkBase { get; set; }
        [XmlElement(ElementName = "CopyToOutputDirectory")]
        public string CopyToOutputDirectory { get; set; }
	}

    [XmlRoot(ElementName = "Folder")]
    public class Folder
	{
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
    }

	[XmlRoot(ElementName = "PackageReference")]
	public class PackageReference
	{
		[XmlAttribute(AttributeName = "Include")]
		public string Include { get; set; }
		[XmlAttribute(AttributeName = "Version")]
		public string Version { get; set; }
		[XmlElement(ElementName = "PrivateAssets")]
		public string PrivateAssets { get; set; }
        [XmlElement(ElementName = "IncludeAssets")]
        public string IncludeAssets { get; set; }
	}

	[XmlRoot(ElementName = "Project")]
	public class Project
	{
        [XmlElement(ElementName = "PropertyGroup")]
        public List<PropertyGroup> PropertyGroup { get; set; }
		[XmlElement(ElementName = "ItemGroup")]
		public List<ItemGroup> ItemGroup { get; set; }
        [XmlElement(ElementName = "Target")]
        public List<Target> Target { get; set; }
        [XmlElement(ElementName = "Import")]
        public List<Import> Import { get; set; }
		[XmlAttribute(AttributeName = "Sdk")]
		public string Sdk { get; set; }
		
        [XmlIgnore]
		public string FilePath { get; set; }
	}

	[XmlRoot(ElementName = "ProjectReference")]
	public class ProjectReference
	{
		[XmlAttribute(AttributeName = "Include")]
		public string Include { get; set; }
	}

	[XmlRoot(ElementName = "PropertyGroup")]
	public class PropertyGroup
	{
		[XmlElement(ElementName = "Configurations")]
		public string Configurations { get; set; }
		[XmlElement(ElementName = "DocumentationFile")]
		public string DocumentationFile { get; set; }
		[XmlElement(ElementName = "NoWarn")]
		public string NoWarn { get; set; }
		[XmlElement(ElementName = "OutputType")]
		public string OutputType { get; set; }
		[XmlElement(ElementName = "PreserveCompilationContext")]
		public string PreserveCompilationContext { get; set; }
		[XmlElement(ElementName = "RuntimeIdentifiers")]
		public string RuntimeIdentifiers { get; set; }
		[XmlElement(ElementName = "ServerGarbageCollection")]
		public string ServerGarbageCollection { get; set; }
        [XmlElement(ElementName = "GenerateDocumentationFile")]
        public string GenerateDocumentationFile { get; set; }
		[XmlElement(ElementName = "TargetFramework")]
		public string TargetFramework { get; set; }
        [XmlElement(ElementName = "IsPackable")]
        public string IsPackable { get; set; }
        [XmlElement(ElementName = "GenerateRuntimeConfigurationFiles")]
        public string GenerateRuntimeConfigurationFiles { get; set; }
        [XmlElement(ElementName = "StartupObject")]
        public string StartupObject { get; set; }
        [XmlElement(ElementName = "ApplicationIcon")]
        public string ApplicationIcon { get; set; }
        [XmlElement(ElementName = "AWSProjectType")]
        public string AWSProjectType { get; set; }
        [XmlElement(ElementName = "PublishReadyToRun")]
        public string PublishReadyToRun { get; set; }
        [XmlElement(ElementName = "Nullable")]
        public string Nullable { get; set; }
        [XmlElement(ElementName = "TreatWarningsAsErrors")]
        public string TreatWarningsAsErrors { get; set; }
        [XmlElement(ElementName = "WarningsAsErrors")]
        public string WarningsAsErrors { get; set; }
        [XmlElement(ElementName = "LangVersion")]
        public string LangVersion { get; set; }
        [XmlElement(ElementName = "RootNamespace")]
        public string RootNamespace { get; set; }
        [XmlElement(ElementName = "UseNETCoreGenerator")]
        public string UseNETCoreGenerator { get; set; }
    }
    [XmlRoot(ElementName = "ItemsToCopy")]
    public class ItemsToCopy
    {
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
    }

    [XmlRoot(ElementName = "Copy")]
    public class Copy
    {
        [XmlAttribute(AttributeName = "SourceFiles")]
        public string SourceFiles { get; set; }
        [XmlAttribute(AttributeName = "DestinationFolder")]
        public string DestinationFolder { get; set; }
        [XmlAttribute(AttributeName = "SkipUnchangedFiles")]
        public string SkipUnchangedFiles { get; set; }
    }

    [XmlRoot(ElementName = "Target")]
    public class Target
    {
        [XmlElement(ElementName = "ItemGroup")]
        public ItemGroup ItemGroup { get; set; }
        [XmlElement(ElementName = "Copy")]
        public Copy Copy { get; set; }
        [XmlElement(ElementName = "Exec")]
        public Exec Exec { get; set; }
		[XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "AfterTargets")]
        public string AfterTargets { get; set; }
    }

    [XmlRoot(ElementName = "Exec")]
    public class Exec
	{
        [XmlAttribute(AttributeName = "Command")]
        public string Command { get; set; }
    }

    [XmlRoot(ElementName = "Import")]
    public class Import
	{
        [XmlAttribute(AttributeName = "Project")]
        public string Project { get; set; }
        [XmlAttribute(AttributeName = "Label")]
        public string Label { get; set; }
	}
}
