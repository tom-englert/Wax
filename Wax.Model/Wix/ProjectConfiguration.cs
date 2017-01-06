namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Xml.Serialization;

    using JetBrains.Annotations;

    [Serializable]
    [XmlType("Configuration")]
    public class ProjectConfiguration
    {
        [NotNull]
        private Dictionary<string, string> _directoryMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        [NotNull]
        private Dictionary<string, string> _fileMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string[] _deployProjectNames;

        [XmlArray("DeployedProjects")]
        [NotNull]
        public string[] DeployedProjectNames
        {
            get
            {
                Contract.Ensures(Contract.Result<string[]>() != null);

                return _deployProjectNames ?? new string[0];
            }
            set
            {
                _deployProjectNames = value;
            }
        }

        [XmlArray("DirectoryMappings")]
        [NotNull]
        public MappingItem[] DirectoryMappingNames
        {
            get
            {
                Contract.Ensures(Contract.Result<MappingItem[]>() != null);

                return _directoryMappings.Select(item => new MappingItem { Key = item.Key, Value = item.Value }).ToArray();
            }
            set
            {
                Contract.Requires(value != null);

                _directoryMappings = value.ToDictionary(_ => _.Key, _ => _.Value, StringComparer.OrdinalIgnoreCase);
            }
        }

        [XmlArray("FileMappings")]
        [NotNull]
        public MappingItem[] FileMappingNames
        {
            get
            {
                Contract.Ensures(Contract.Result<MappingItem[]>() != null);

                return _fileMappings.Select(item => new MappingItem { Key = item.Key, Value = item.Value }).ToArray();
            }
            set
            {
                Contract.Requires(value != null);
                _fileMappings = value.ToDictionary(_ => _.Key, _ => _.Value, StringComparer.OrdinalIgnoreCase);
            }
        }

        [XmlIgnore]
        [NotNull]
        public Dictionary<string, string> DirectoryMappings
        {
            get
            {
                Contract.Ensures(Contract.Result<Dictionary<string, string>>() != null);

                return _directoryMappings;
            }
        }

        [XmlIgnore]
        [NotNull]
        public Dictionary<string, string> FileMappings
        {
            get
            {
                Contract.Ensures(Contract.Result<Dictionary<string, string>>() != null);

                return _fileMappings;
            }
        }

        [XmlElement("DeploySymbols")]
        public bool DeploySymbols { get; set; }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_directoryMappings != null);
            Contract.Invariant(_fileMappings != null);
        }
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