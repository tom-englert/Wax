namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    using tomenglertde.Wax.Model.Mapping;
    using tomenglertde.Wax.Model.VisualStudio;

    public class WixSourceFile
    {
        private readonly WixProject _project;
        private readonly EnvDTE.ProjectItem _projectItem;
        private readonly XDocument _xmlFile;
        private XDocument _rawXmlFile;
        private readonly XElement _root;

        private readonly List<WixFileNode> _fileNodes;
        private readonly List<WixDirectoryNode> _directoryNodes;
        private readonly List<WixComponentGroupNode> _componentGroups;
        private readonly List<WixFeatureNode> _featureNodes;
        private readonly List<WixDefine> _defines;

        public WixSourceFile(WixProject project, EnvDTE.ProjectItem projectItem)
        {
            Contract.Requires(project != null);
            Contract.Requires(projectItem != null);

            _project = project;
            _projectItem = projectItem;

            _xmlFile = _projectItem.GetXmlContent(LoadOptions.PreserveWhitespace);
            _rawXmlFile = _projectItem.GetXmlContent(LoadOptions.None);
            _root = _xmlFile.Root;

            Contract.Assume(_root != null);

            _defines = _root.Nodes().OfType<XProcessingInstruction>()
                .Where(p => p.Target.Equals(WixNames.Define, StringComparison.Ordinal))
                .Select(p => new WixDefine(this, p))
                .ToList();

            _componentGroups = _root.Descendants(WixNames.ComponentGroupNode)
                .Select(node => new WixComponentGroupNode(this, node))
                .ToList();

            _fileNodes = new List<WixFileNode>();

            _fileNodes.AddRange(_root
                .Descendants(WixNames.FileNode)
                .Select(node => new WixFileNode(this, node, _fileNodes)));

            _directoryNodes = _root
                .Descendants(WixNames.DirectoryNode)
                .Select(node => new WixDirectoryNode(this, node))
                .Where(node => node.Id != "TARGETDIR")
                .Where(node => node.Name != null)
                .ToList();

            _featureNodes = _root
                .Descendants(WixNames.FeatureNode)
                .Select(node => new WixFeatureNode(this, node))
                .ToList();
        }

        public IEnumerable<WixFileNode> FileNodes
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<WixFileNode>>() != null);

                return new ReadOnlyCollection<WixFileNode>(_fileNodes);
            }
        }

        public IEnumerable<WixDirectoryNode> DirectoryNodes
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<WixDirectoryNode>>() != null);

                return new ReadOnlyCollection<WixDirectoryNode>(_directoryNodes);
            }
        }

        public IEnumerable<WixComponentGroupNode> ComponentGroups
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<WixComponentGroupNode>>() != null);

                return new ReadOnlyCollection<WixComponentGroupNode>(_componentGroups);
            }
        }

        public IEnumerable<WixFeatureNode> FeatureNodes
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<WixFeatureNode>>() != null);

                return new ReadOnlyCollection<WixFeatureNode>(_featureNodes);
            }
        }

        public WixProject Project
        {
            get
            {
                Contract.Ensures(Contract.Result<WixProject>() != null);

                return _project;
            }
        }

        public bool HasChanges
        {
            get
            {
                try
                {
                    var xmlFile = _projectItem.GetXmlContent(LoadOptions.None);

                    return xmlFile.ToString(SaveOptions.DisableFormatting) != _rawXmlFile.ToString(SaveOptions.DisableFormatting);
                }
                catch (XmlException)
                {
                    return true;
                }
            }
        }

        internal WixDirectoryNode AddDirectory(string id, string name, string parentId)
        {
            Contract.Requires(id != null);
            Contract.Requires(name != null);
            Contract.Requires(parentId != null);

            var root = _root;

            var fragmentElement = new XElement(WixNames.FragmentNode);
            root.Add(fragmentElement);

            var directoryRefElement = new XElement(WixNames.DirectoryRefNode);
            directoryRefElement.Add(new XAttribute("Id", parentId));
            fragmentElement.Add(directoryRefElement);

            var directoryElement = new XElement(WixNames.DirectoryNode);
            directoryElement.Add(new XAttribute("Id", id));
            directoryElement.Add(new XAttribute("Name", name));
            directoryRefElement.Add(directoryElement);

            Save();

            return AddDirectoryNode(directoryElement);
        }

        public WixDirectoryNode AddDirectoryNode(XElement directoryElement)
        {
            Contract.Requires(directoryElement != null);
            Contract.Ensures(Contract.Result<WixDirectoryNode>() != null);

            var directoryNode = new WixDirectoryNode(this, directoryElement);
            _directoryNodes.Add(directoryNode);
            return directoryNode;
        }

        internal WixComponentGroupNode AddComponentGroup(string directoryId)
        {
            Contract.Requires(directoryId != null);
            Contract.Ensures(Contract.Result<WixComponentGroupNode>() != null);

            var root = _root;

            var fragmentElement = new XElement(WixNames.FragmentNode);
            root.Add(fragmentElement);

            var componentGroupElement = new XElement(WixNames.ComponentGroupNode);
            componentGroupElement.Add(new XAttribute("Id", directoryId + "_files"));
            componentGroupElement.Add(new XAttribute("Directory", directoryId));
            fragmentElement.Add(componentGroupElement);

            Save();

            var componentGroup = new WixComponentGroupNode(this, componentGroupElement);
            _componentGroups.Add(componentGroup);
            return componentGroup;
        }

        internal WixFileNode AddFileComponent(WixComponentGroupNode componentGroup, string id, string name, FileMapping fileMapping)
        {
            Contract.Requires(componentGroup != null);
            Contract.Requires(id != null);
            Contract.Requires(name != null);
            Contract.Requires(fileMapping != null);

            Contract.Assume(componentGroup.SourceFile.Equals(this));

            var componentElement = new XElement(WixNames.ComponentNode);
            var fileElement = new XElement(WixNames.FileNode);

            componentElement.Add(new XAttribute("Id", id));
            componentElement.Add(new XAttribute("Guid", Guid.NewGuid().ToString()));

            fileElement.Add(new XAttribute("Id", id));
            fileElement.Add(new XAttribute("Name", name));

            var project = fileMapping.Project;

            var variableName = string.Format(CultureInfo.InvariantCulture, "{0}_TargetDir", project.Name);

            ForceDirectoryVariable(variableName, project);

            var source = string.Format(CultureInfo.InvariantCulture, "$(var.{0}){1}", variableName, fileMapping.SourceName);
            fileElement.Add(new XAttribute("Source", source));

            componentElement.Add(fileElement);
            componentGroup.Node.Add(componentElement);

            Save();

            var fileNode = new WixFileNode(componentGroup.SourceFile, fileElement, _fileNodes);
            _fileNodes.Add(fileNode);
            return fileNode;
        }

        internal void Save()
        {
            _projectItem.SetContent(_xmlFile.Declaration + _xmlFile.ToString(SaveOptions.DisableFormatting));
            _rawXmlFile = _projectItem.GetXmlContent(LoadOptions.None);
        }

        private void ForceDirectoryVariable(string variableName, Project project)
        {
            Contract.Requires(variableName != null);
            Contract.Requires(project != null);

            if (_defines.Any(d => d.Name.Equals(variableName, StringComparison.Ordinal)))
                return;

            var data = string.Format(CultureInfo.InvariantCulture, "{0}=$(var.{1}.TargetDir)", variableName, project.Name);
            var processingInstruction = new XProcessingInstruction(WixNames.Define, data);

            var lastNode = _defines.LastOrDefault();

            if (lastNode != null)
            {
                lastNode.Node.AddAfterSelf(processingInstruction);
            }
            else
            {
                var firstNode = _root.FirstNode;
                if (firstNode != null)
                {
                    firstNode.AddBeforeSelf(processingInstruction);
                }
                else
                {
                    _root.Add(processingInstruction);
                }
            }

            _defines.Add(new WixDefine(this, processingInstruction));
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_directoryNodes != null);
            Contract.Invariant(_fileNodes != null);
            Contract.Invariant(_projectItem != null);
            Contract.Invariant(_project != null);
            Contract.Invariant(_componentGroups != null);
            Contract.Invariant(_root != null);
            Contract.Invariant(_xmlFile != null);
            Contract.Invariant(_rawXmlFile != null);
        }
    }
}