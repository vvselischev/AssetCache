using System;
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
                    if (!currentLine.StartsWith("---"))
                    {
                        currentDocumentText += currentLine;
                    }
                    else
                    {
                        yield return LoadDocument(currentDocumentText);
                        currentDocumentText = header + currentLine;
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

        public void ParseDocument(YamlDocument document)
        {
            var rootNode = (YamlMappingNode) document.RootNode;

            var id = rootNode.Anchor;
            Console.WriteLine($"id: {id}");

            if (rootNode.Children.Count == 0)
            {
                return;
            }
            
            var entry = rootNode.Children.First();
            Visit(entry.Value);
        }

        private void ParseMappingNode(YamlMappingNode node)
        {
            foreach (var property in node.Children)
            {
                var keyNode = (YamlScalarNode) property.Key;
                if (keyNode.Value == "m_Component")
                {
                    var componentIds = ParseAttachedComponents((YamlSequenceNode)property.Value);
                    Console.Write("Attached: ");
                    foreach (var id in componentIds)
                    {
                        Console.Write(id + " ");
                    }
                    Console.WriteLine();
                } 
                else if (keyNode.Value == "fileID")
                {
                    var valueNode = (YamlScalarNode) property.Value;
                    var id = ulong.Parse(valueNode.Value);

                    if (id != 0)
                    {
                        Console.WriteLine("use id:" + id);
                    }
                }
                else if (keyNode.Value == "guid")
                {
                    var valueNode = (YamlScalarNode) property.Value;
                    var guid = valueNode.Value;
                    Console.WriteLine("use guid:" + guid);
                }

                Visit(property.Value);
            }
        }

        private void Visit(YamlNode node)
        {
            if (node.NodeType == YamlNodeType.Sequence)
            {
                ParseSequenceNode((YamlSequenceNode)node);
            }
            else if (node.NodeType == YamlNodeType.Mapping)
            {
                ParseMappingNode((YamlMappingNode)node);
            }
        }

        private void ParseSequenceNode(YamlSequenceNode node)
        {
            foreach (var entry in node)
            {
                Visit(entry);
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