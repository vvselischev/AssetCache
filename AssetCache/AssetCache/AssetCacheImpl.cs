using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace AssetCache
{
    public class AssetCacheImpl : IAssetCache
    {
        /// <summary>
        /// The default maximum number of documents to be processed during the accumulation phase.
        /// </summary>
        private const int DefaultAccumulationPhaseSteps = 5000;
        private int accumulationPhaseSteps;
        
        /// <summary>
        /// Stores the index to query from.
        /// </summary>
        private GlobalCacheIndex globalIndex = new GlobalCacheIndex();
        
        /// <summary>
        /// Stores a cache for each new file to use in case the file has not been changed
        /// between interruptions.
        /// Contains an already built file index, a timestamp to check file status and a number
        /// of processed lines in order to skip them while reading the input stream next time.
        /// </summary>
        private Dictionary<string, FileIncrementCache> fileIncrementCaches = 
            new Dictionary<string, FileIncrementCache>();

        public AssetCacheImpl(int accumulationPhaseSteps = DefaultAccumulationPhaseSteps)
        {
            this.accumulationPhaseSteps = accumulationPhaseSteps;
        }
        
        /// <inheritdoc />
        /// <summary>
        /// The index build represents the cycle of two phases:
        /// Accumulation phase: read and parse documents from the input stream one by one,
        /// accumulating the information in the temporary index.
        /// Merge phase: copy the information from the temporary index to the file index.
        /// Too frequent checks for interruption are bad,
        /// so we do not perform them during the accumulation phase.
        /// </summary>
        public object Build(string path, Action interruptChecker)
        {
            using (var reader = new StreamReader(path, GetYamlEncoding(path)))
            {
                ValidateFileCache(path);
                var fileIndex = GetFileIndex(path);

                var parser = new UnityYamlParser();
                int[] processedLines;
                var documentsStream = GetDocumentsStream(path, reader, out processedLines);

                var currentStep = 0;
                var accumulatedIndex = new CacheIndex();
                
                foreach (var document in documentsStream)
                {
                    parser.ParseDocument(document, accumulatedIndex);

                    currentStep++;
                    if (currentStep == accumulationPhaseSteps)
                    {
                        fileIndex.Merge(accumulatedIndex);
                        currentStep = 0;
                        accumulatedIndex = new CacheIndex();

                        CheckInterrupt(interruptChecker, path, fileIndex, processedLines[0]);
                    }
                }

                if (currentStep != 0)
                {
                    fileIndex.Merge(accumulatedIndex);
                }

                return fileIndex;
            }
        }
        
        /// <inheritdoc />
        /// <summary>
        /// The same file might be merged again after some changes,
        /// so the global index removes the old information before merging.
        /// </summary>
        public void Merge(string path, object result)
        {
            globalIndex.AddIndex(path, result as CacheIndex);
        }

        public int GetLocalAnchorUsages(ulong anchor)
        {
            return globalIndex.GetIdUsages(anchor);
        }

        public int GetGuidUsages(string guid)
        {
            return globalIndex.GetGuidUsages(guid);
        }

        public IEnumerable<ulong> GetComponentsFor(ulong gameObjectAnchor)
        {
            return globalIndex.GetAttachedComponents(gameObjectAnchor);
        }

        private Encoding GetYamlEncoding(string path)
        {
            var bom = new byte[4];
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            if (bom[0] == 0xff && bom[1] == 0xfe)
            {
                return Encoding.Unicode;
            }

            if (bom[0] == 0xfe && bom[1] == 0xff)
            {
                return Encoding.BigEndianUnicode;
            }
            return Encoding.UTF8;
        }
        
        private void ValidateFileCache(string path)
        {
            if (WasFileChanged(path))
            {
                fileIncrementCaches.Remove(path);
            }
        }

        private void CheckInterrupt(Action interruptChecker, string path, CacheIndex fileIndex, 
            int processedLinesNumber)
        {
            fileIncrementCaches[path] = new FileIncrementCache(fileIndex, 
                File.GetLastWriteTime(path), 
                processedLinesNumber);
            interruptChecker.Invoke();
        }

        private CacheIndex GetFileIndex(string path)
        {
            if (fileIncrementCaches.ContainsKey(path) && !WasFileChanged(path))
            {
                return fileIncrementCaches[path].FileIndex;
            }

            return new CacheIndex();
        }

        private IEnumerable<YamlDocument> GetDocumentsStream(string path, StreamReader reader, 
            out int[] processedLines)
        {
            // A workaround to count processed lines inside enumerator.
            processedLines = new[] {0};
            
            if (fileIncrementCaches.ContainsKey(path) && !WasFileChanged(path))
            {
                processedLines[0] = fileIncrementCaches[path].ProcessedLinesNumber;
            }

            return ParseFileStream(reader, processedLines, processedLines[0]);
        }

        private bool WasFileChanged(string path)
        {
            if (fileIncrementCaches.ContainsKey(path))
            {
                return File.GetLastWriteTime(path) != fileIncrementCaches[path].LastChangeTime;
            }

            return true;
        }

        private IEnumerable<YamlDocument> ParseFileStream(StreamReader reader, int[] counter,
            int skipLines = 0)
        {
            string firstLine;
            var header = ReadHeader(reader, out firstLine, skipLines);

            if (firstLine == "")
            {
                yield break;
            }

            counter[0]++;
            
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

                counter[0]++;
            }

            yield return LoadDocument(currentDocumentText);
        }

        private string ReadHeader(StreamReader reader, out string firstLine, int skipLines = 0)
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

            if (skipLines == 0)
            {
                return header;
            }

            firstLine = SkipLines(reader, skipLines);
            return header;
        }

        private string SkipLines(StreamReader reader, int skipLines)
        {
            var firstLine = "";
            while (!reader.EndOfStream && skipLines > 0)
            {
                firstLine = reader.ReadLine() + '\n';
                skipLines--;
            }

            if (reader.EndOfStream)
            {
                firstLine = "";
            }
            return firstLine;
        }

        private YamlDocument LoadDocument(string text)
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(text));
            return yaml.Documents[0];
        }
    }
}