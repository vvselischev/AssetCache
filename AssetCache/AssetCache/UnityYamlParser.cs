using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace AssetCache
{
    public class UnityYamlParser
    {
        public IEnumerator<YamlDocument> ParseFileStream(string path)
        {
            using (var reader = new StreamReader(path))
            {
                string firstLine;
                var header = ReadHeader(reader, out firstLine);

                if (firstLine == "")
                {
                    yield break;
                }

                var currentDocumentText = header + firstLine;
                while (!reader.EndOfStream)
                {
                    var currentLine = reader.ReadLine() + '\n';
                    if (currentLine.StartsWith("---"))
                    {
                        yield return LoadDocument(currentDocumentText);
                        currentDocumentText = header + currentLine;
                    }
                    else
                    {
                        currentDocumentText += currentLine;
                    }
                }
                yield return LoadDocument(currentDocumentText);
            }
        }

        private string ReadHeader(StreamReader reader, out string firstLine)
        {
            var header = "";
            firstLine = "";
            while (!reader.EndOfStream)
            {
                var currentLine = reader.ReadLine() + '\n';
                
                // Fix unity's violation of the yaml format:
                currentLine = Regex.Replace(currentLine, "([0-9]+ &[0-9]+) stripped\n", "$1\n");
                
                if (!currentLine.StartsWith("---"))
                {
                    header += currentLine;
                }
                else
                {
                    firstLine = currentLine;
                    break;
                }
            }

            return header;
        }

        private YamlDocument LoadDocument(string text)
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(text));
            return yaml.Documents[0];
        }
        
        
//        public IEnumerable<YamlDocument> ParseFile(string path)
//        {
//            // Fix unity's violation of the yaml format:
//            var wholeFile = File.ReadAllText(path);
//            wholeFile = Regex.Replace(wholeFile, "([0-9]+ &[0-9]+) stripped\n", "$1\n");
//
//            Console.WriteLine("Finish replace");
//            using (var reader = new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(wholeFile))))
//            {
//                var yaml = new YamlStream();
//                yaml.Load(reader);
//                return null;
//                return yaml.Documents;
//            }
//        }

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
                    var id = ulong.Parse(valueNode.Value);
                    cacheIndex.IncrementIdUsages(id);
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