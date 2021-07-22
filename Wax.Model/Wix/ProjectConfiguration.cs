namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    using TomsToolbox.Essentials;

    [Serializable]
    [XmlType("Configuration")]
    public class ProjectConfiguration
    {
        private string[]? _deployProjectNames;

        [XmlArray("DeployedProjects")]
        public string[] DeployedProjectNames
        {
            get => _deployProjectNames ?? Array.Empty<string>();
            set => _deployProjectNames = value;
        }

        [XmlArray("DirectoryMappings")]
        public MappingItem[] DirectoryMappingNames
        {
            get => DirectoryMappings.Select(item => new MappingItem { Key = item.Key, Value = item.Value }).ToArray();
            set => DirectoryMappings = value.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        }

        [XmlArray("FileMappings")]
        public MappingItem[] FileMappingNames
        {
            get => FileMappings.Select(item => new MappingItem { Key = item.Key, Value = item.Value }).ToArray();
            set => FileMappings = value.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        }

        [XmlIgnore]
        public Dictionary<string, string> DirectoryMappings { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [XmlIgnore]
        public Dictionary<string, string> FileMappings { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [XmlElement("DeploySymbols")]
        public bool DeploySymbols { get; set; }

        [XmlElement("DeployLocalizations")]
        public bool DeployLocalizations { get; set; } = true;

        [XmlElement("DeployExternalLocalizations")]
        public bool DeployExternalLocalizations { get; set; }

        [XmlElement("ExcludedProjectItems")]
        public string ExcludedProjectItemsValue
        {
            get => ExcludedProjectItems.IsNullOrEmpty() ? "-" : ExcludedProjectItems;
            set => ExcludedProjectItems = value == "-" ? null : value;
        }

        [XmlIgnore]
        public string? ExcludedProjectItems { get; set; }
    }

    [Serializable]
    [XmlType("Item")]
    public class MappingItem
    {
        [XmlAttribute("Key")]
        public string Key
        {
            get;
            set;
        } = string.Empty;

        [XmlAttribute("Value")]
        public string Value
        {
            get;
            set;
        } = string.Empty;
    }
}