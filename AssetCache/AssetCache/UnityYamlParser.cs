using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace AssetCache
{
    /// <summary>
    /// Uses the YamlDotNet library to parse documents from unity file.
    /// </summary>
    public class UnityYamlParser
    {
        /// <summary>
        /// Converts the document from the given string to the YamlDocument.
        /// </summary>
        public YamlDocument LoadDocument(string text)
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(text));
            return yaml.Documents[0];
        }
        
        /// <summary>
        /// Parses the document and puts the collected data in the given index.
        /// </summary>
        public void ParseDocument(YamlDocument document, CacheIndex cacheIndex)
        {
            var rootNode = document.RootNode as YamlMappingNode;
            var currentId = ulong.Parse(rootNode.Anchor);

            if (rootNode.Children.Count == 0)
            {
                return;
            }
            
            var entry = rootNode.Children.First();
            Visit(entry.Value, cacheIndex, currentId);
        }

        private void ParseMappingNode(YamlMappingNode node, CacheIndex cacheIndex, ulong currentId)
        {
            foreach (var property in node.Children)
            {
                var keyNode = property.Key as YamlScalarNode;
                if (keyNode.Value == "m_Component")
                {
                    var componentIds = ParseAttachedComponents(property.Value as YamlSequenceNode);
                    cacheIndex.AddAttachedComponents(currentId, componentIds.ToList());
                }
                else if (keyNode.Value == "fileID")
                {
                    var valueNode = property.Value as YamlScalarNode;

                    ulong id;
                    if (ulong.TryParse(valueNode.Value, out id))
                    {
                        if (id != 0)
                        {
                            cacheIndex.IncrementIdUsages(id);
                        }
                    }
                }
                else if (keyNode.Value == "guid")
                {
                    var valueNode = property.Value as YamlScalarNode;
                    var guid = valueNode.Value;
                    cacheIndex.IncrementGuidUsages(guid);
                }

                Visit(property.Value, cacheIndex, currentId);
            }
        }

        private void Visit(YamlNode node, CacheIndex cacheIndex, ulong currentId)
        {
            if (node.NodeType == YamlNodeType.Sequence)
            {
                ParseSequenceNode(node as YamlSequenceNode, cacheIndex, currentId);
            }
            else if (node.NodeType == YamlNodeType.Mapping)
            {
                ParseMappingNode(node as YamlMappingNode, cacheIndex, currentId);
            }
        }

        private void ParseSequenceNode(YamlSequenceNode node, CacheIndex cacheIndex, ulong currentId)
        {
            foreach (var entry in node)
            {
                Visit(entry, cacheIndex, currentId);
            }
        }

        private IEnumerable<ulong> ParseAttachedComponents(YamlSequenceNode entries)
        {
            return entries.Cast<YamlMappingNode>()
                .Select(entry => entry.Children.First().Value["fileID"].ToString())
                .Select(ulong.Parse)
                .ToList();
        }
    }
}