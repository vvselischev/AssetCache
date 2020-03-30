using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace AssetCache
{
    public class AssetCacheImpl : IAssetCache
    {
        private const int AccumulationPhaseSteps = 1;
        
        private CacheIndex globalIndex = new CacheIndex();
        private Dictionary<string, FileIncrementCache> fileIncrementCaches;

        public AssetCacheImpl()
        {
            fileIncrementCaches = new Dictionary<string, FileIncrementCache>();
        }
        
        public object Build(string path, Action interruptChecker)
        {
            var fileIndex = GetFileIndex(path);
            
            var parser = new UnityYamlParser();
            var documentEnumerator = GetDocumentEnumerator(path, parser);

            var currentStep = 0;
            var accumulatedIndex = new CacheIndex();

            while (documentEnumerator.MoveNext())
            {
                parser.ParseDocument(documentEnumerator.Current, accumulatedIndex);
                
                currentStep++;
                if (currentStep == AccumulationPhaseSteps)
                {
                    currentStep = 0;
                    fileIndex.Merge(accumulatedIndex);
                    accumulatedIndex = new CacheIndex();
                    
                    CheckInterrupt(interruptChecker, path, fileIndex, documentEnumerator);
                } 
            }

            if (currentStep != 0)
            {
                fileIndex.Merge(accumulatedIndex);
            }
            
            CheckInterrupt(interruptChecker, path, fileIndex, documentEnumerator);
          
            return fileIndex;
        }

        private void CheckInterrupt(Action interruptChecker, 
            string path, 
            CacheIndex fileIndex, 
            IEnumerator<YamlDocument> documentEnumerator)
        {
            var incrementCache = new FileIncrementCache(fileIndex, File.GetLastWriteTime(path),
                documentEnumerator);
            fileIncrementCaches[path] = incrementCache;
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

        private IEnumerator<YamlDocument> GetDocumentEnumerator(string path, UnityYamlParser parser)
        {
            if (fileIncrementCaches.ContainsKey(path) && !WasFileChanged(path))
            {
                return fileIncrementCaches[path].DocumentEnumerator;
            }

            return parser.ParseFileStream(path);
        }

        private bool WasFileChanged(string path)
        {
            if (fileIncrementCaches.ContainsKey(path))
            {
                return File.GetLastWriteTime(path) != fileIncrementCaches[path].LastChangeTime;
            }

            return true;
        }

        public void Merge(string path, object result)
        {
            fileIncrementCaches.Remove(path);
            globalIndex.Merge(result as CacheIndex);
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
    }
}