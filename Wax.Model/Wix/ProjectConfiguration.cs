namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Xml.Serialization;

    using JetBrains.Annotations;

    [Serializable]
    [XmlType("Configuration")]
    public class ProjectConfiguration
    {
        private string[] _deployProjectNames;

        [XmlArray("DeployedProjects")]
        [NotNull]
        public string[] DeployedProjectNames
        {
            get => _deployProjectNames ?? new string[0];
            set => _deployProjectNames = value;
        }

        [XmlArray("DirectoryMappings")]
        [NotNull]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public MappingItem[] DirectoryMappingNames
        {
            get => DirectoryMappings.Select(item => new MappingItem { Key = item.Key, Value = item.Value }).ToArray();
            set => DirectoryMappings = value.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        }

        [XmlArray("FileMappings")]
        [NotNull]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public MappingItem[] FileMappingNames
        {
            get => FileMappings.Select(item => new MappingItem { Key = item.Key, Value = item.Value }).ToArray();
            set => FileMappings = value.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        }

        [XmlIgnore]
        [NotNull]
        public Dictionary<string, string> DirectoryMappings { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [XmlIgnore]
        [NotNull]
        public Dictionary<string, string> FileMappings { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [XmlElement("DeploySymbols")]
        public bool DeploySymbols { get; set; }

        [XmlElement("DeployLocalizations")]
        public bool DeployLocalizations { get; set; } = true;

        [XmlElement("DeployExternalLocalizations")]
        public bool DeployExternalLocalizations { get; set; }
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
        }

        [XmlAttribute("Value")]
        public string Value
        {
            get;
            set;
        }
    }
}