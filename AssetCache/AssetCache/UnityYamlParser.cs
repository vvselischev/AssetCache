using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace AssetCache
{
    public class UnityYamlParser
    {
        public IEnumerable<YamlDocument> ParseFileStream(string path)
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

        public ParseInfo ParseDocument(YamlDocument document)
        {
            var parseInfo = new ParseInfo();
            var rootNode = (YamlMappingNode) document.RootNode;

            parseInfo.Id = ulong.Parse(rootNode.Anchor);

            if (rootNode.Children.Count == 0)
            {
                return parseInfo;
            }
            
            var entry = rootNode.Children.First();
            Visit(entry.Value, parseInfo);
            return parseInfo;
        }

        private void ParseMappingNode(YamlMappingNode node, ParseInfo parseInfo)
        {
            foreach (var property in node.Children)
            {
                var keyNode = (YamlScalarNode) property.Key;
                if (keyNode.Value == "m_Component")
                {
                    var componentIds = ParseAttachedComponents((YamlSequenceNode)property.Value);
                    parseInfo.AttachedComponents = componentIds.ToList();
                }
                else if (keyNode.Value == "fileID")
                {
                    var valueNode = (YamlScalarNode) property.Value;
                    var id = ulong.Parse(valueNode.Value);

                    if (!parseInfo.IdUsages.ContainsKey(id))
                    {
                        parseInfo.IdUsages[id] = 0;
                    }
                    parseInfo.IdUsages[id]++;
                }
                else if (keyNode.Value == "guid")
                {
                    var valueNode = (YamlScalarNode) property.Value;
                    var guid = valueNode.Value;
                    
                    if (!parseInfo.GuidUsages.ContainsKey(guid))
                    {
                        parseInfo.GuidUsages[guid] = 0;
                    }
                    parseInfo.GuidUsages[guid]++;
                }

                Visit(property.Value, parseInfo);
            }
        }

        private void Visit(YamlNode node, ParseInfo parseInfo)
        {
            if (node.NodeType == YamlNodeType.Sequence)
            {
                ParseSequenceNode((YamlSequenceNode)node, parseInfo);
            }
            else if (node.NodeType == YamlNodeType.Mapping)
            {
                ParseMappingNode((YamlMappingNode)node, parseInfo);
            }
        }

        private void ParseSequenceNode(YamlSequenceNode node, ParseInfo parseInfo)
        {
            foreach (var entry in node)
            {
                Visit(entry, parseInfo);
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