/* 
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Collections.Generic;
using System.Xml.Serialization;

namespace ReferenceTrace.NuSpec
{
    [XmlRoot(ElementName = "dependencies")]
    public class Dependencies
    {
        [XmlElement(ElementName = "group")]
        public List<Group> Group { get; set; }
    }

    [XmlRoot(ElementName = "dependency")]
    public class Dependency
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
    }

    [XmlRoot(ElementName = "group")]
    public class Group
    {
        [XmlElement(ElementName = "dependency")]
        public Dependency Dependency { get; set; }
        [XmlAttribute(AttributeName = "targetFramework")]
        public string TargetFramework { get; set; }
    }

    [XmlRoot(ElementName = "metadata")]
    public class Metadata
    {
        [XmlElement(ElementName = "authors")]
        public string Authors { get; set; }
        [XmlElement(ElementName = "dependencies")]
        public Dependencies Dependencies { get; set; }
        [XmlElement(ElementName = "description")]
        public string Description { get; set; }
        [XmlElement(ElementName = "iconUrl")]
        public string IconUrl { get; set; }
        [XmlElement(ElementName = "id")]
        public string Id { get; set; }
        [XmlElement(ElementName = "language")]
        public string Language { get; set; }
        [XmlElement(ElementName = "licenseUrl")]
        public string LicenseUrl { get; set; }
        [XmlElement(ElementName = "owners")]
        public string Owners { get; set; }
        [XmlElement(ElementName = "projectUrl")]
        public string ProjectUrl { get; set; }
        [XmlElement(ElementName = "requireLicenseAcceptance")]
        public string RequireLicenseAcceptance { get; set; }
        [XmlElement(ElementName = "tags")]
        public string Tags { get; set; }
        [XmlElement(ElementName = "title")]
        public string Title { get; set; }
        [XmlElement(ElementName = "version")]
        public string Version { get; set; }
    }

    [XmlRoot(ElementName = "package")]
    public class Package
    {
        [XmlElement(ElementName = "metadata")]
        public Metadata Metadata { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }
    }

}
