namespace tomenglertde.Wax.Model.Wix
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    using tomenglertde.Wax.Model.Mapping;
    using tomenglertde.Wax.Model.Tools;
    using tomenglertde.Wax.Model.VisualStudio;

    public class WixSourceFile
    {
        private readonly EnvDTE.ProjectItem _projectItem;
        private readonly XDocument _xmlFile;
        private XDocument _rawXmlFile;
        private readonly XElement _root;
        private readonly List<WixFileNode> _fileNodes;
        private readonly List<WixDirectoryNode> _directoryNodes;
        private readonly List<WixComponentGroupNode> _componentGroupNodes;
        private readonly List<WixComponentNode> _componentNodes;
        private readonly List<WixFeatureNode> _featureNodes;
        private readonly List<WixDefine> _defines;

        public WixSourceFile(WixProject project, EnvDTE.ProjectItem projectItem)
        {
            Project = project;
            _projectItem = projectItem;

            try
            {
                _xmlFile = _projectItem.GetXmlContent(LoadOptions.PreserveWhitespace);
                _rawXmlFile = _projectItem.GetXmlContent(LoadOptions.None);
            }
            catch
            {
                var placeholder = @"<?xml version=""1.0"" encoding=""utf-8""?><Include />";
                _xmlFile = XDocument.Parse(placeholder);
                _rawXmlFile = XDocument.Parse(placeholder);
            }

            var root = _xmlFile.Root;

            _root = root ?? throw new InvalidDataException("Invalid source file: " + projectItem.TryGetFileName());

            WixNames = new WixNames(root.GetDefaultNamespace().NamespaceName);

            _defines = root.Nodes().OfType<XProcessingInstruction>()
                .Where(p => p.Target.Equals(WixNames.Define, StringComparison.Ordinal))
                .Select(p => new WixDefine(this, p))
                .ToList();

            _componentGroupNodes = root.Descendants(WixNames.ComponentGroupNode)
                .Select(node => new WixComponentGroupNode(this, node))
                .ToList();

            _componentNodes = root.Descendants(WixNames.ComponentNode)
                .Select(node => new WixComponentNode(this, node))
                .ToList();

            _fileNodes = new List<WixFileNode>();

            _fileNodes.AddRange(root
                .Descendants(WixNames.FileNode)
                .Select(node => new WixFileNode(this, node, _fileNodes)));

            _directoryNodes = root
                .Descendants(WixNames.DirectoryNode)
                .Select(node => new WixDirectoryNode(this, node))
                .Where(node => node.Id != "TARGETDIR")
                .ToList();

            _featureNodes = root
                .Descendants(WixNames.FeatureNode)
                .Select(node => new WixFeatureNode(this, node))
                .ToList();

            var featureNodesLookup = _featureNodes.ToDictionary(item => item.Id);

            foreach (var featureNode in _featureNodes)
            {
                featureNode.BuildTree(featureNodesLookup);
            }
        }

        public WixNames WixNames { get; }

        public IEnumerable<WixFileNode> FileNodes => new ReadOnlyCollection<WixFileNode>(_fileNodes);

        public IEnumerable<WixDirectoryNode> DirectoryNodes => new ReadOnlyCollection<WixDirectoryNode>(_directoryNodes);

        public IEnumerable<WixComponentGroupNode> ComponentGroupNodes => new ReadOnlyCollection<WixComponentGroupNode>(_componentGroupNodes);

        public IEnumerable<WixFeatureNode> FeatureNodes => new ReadOnlyCollection<WixFeatureNode>(_featureNodes);

        public IEnumerable<WixComponentNode> ComponentNodes => new ReadOnlyCollection<WixComponentNode>(_componentNodes);

        public WixProject Project { get; }

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
            var root = _root;

            var fragmentElement = new XElement(WixNames.FragmentNode);
            root.AddWithFormatting(fragmentElement);

            var directoryRefElement = new XElement(WixNames.DirectoryRefNode, new XAttribute("Id", parentId));
            fragmentElement.AddWithFormatting(directoryRefElement);

            var directoryElement = new XElement(WixNames.DirectoryNode, new XAttribute("Id", id), new XAttribute("Name", name));
            directoryRefElement.AddWithFormatting(directoryElement);

            Save();

            return AddDirectoryNode(directoryElement);
        }

        public WixDirectoryNode AddDirectoryNode(XElement directoryElement)
        {
            var directoryNode = new WixDirectoryNode(this, directoryElement);
            _directoryNodes.Add(directoryNode);
            return directoryNode;
        }

        internal WixComponentGroupNode AddComponentGroup(string directoryId)
        {
            var root = _root;

            var fragmentElement = new XElement(WixNames.FragmentNode);
            root.AddWithFormatting(fragmentElement);

            var componentGroupElement = new XElement(WixNames.ComponentGroupNode, new XAttribute("Id", directoryId + "_files"), new XAttribute("Directory", directoryId));
            fragmentElement.AddWithFormatting(componentGroupElement);

            Save();

            var componentGroup = new WixComponentGroupNode(this, componentGroupElement);
            _componentGroupNodes.Add(componentGroup);
            return componentGroup;
        }

        internal WixFileNode AddFileComponent(WixComponentGroupNode componentGroup, string id, string name, FileMapping fileMapping)
        {
            Debug.Assert(componentGroup.SourceFile.Equals(this));

            var project = fileMapping.Project;

            var variableName = string.Format(CultureInfo.InvariantCulture, "{0}_TargetDir", project.Name);

            ForceDirectoryVariable(variableName, project);

            var componentElement = new XElement(WixNames.ComponentNode, new XAttribute("Id", id), new XAttribute("Guid", Guid.NewGuid().ToString()));

            componentGroup.Node.AddWithFormatting(componentElement);

            var source = string.Format(CultureInfo.InvariantCulture, "$(var.{0}){1}", variableName, fileMapping.SourceName);

            var fileElement = new XElement(WixNames.FileNode, new XAttribute("Id", id), new XAttribute("Name", name), new XAttribute("Source", source));

            componentElement.AddWithFormatting(fileElement);

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
    }
}