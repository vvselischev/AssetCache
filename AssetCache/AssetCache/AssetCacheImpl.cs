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
        private Dictionary<string, IEnumerator<YamlDocument>> documentCache;
        private Dictionary<string, DateTime> lastWriteTime;

        public AssetCacheImpl()
        {
            documentCache = new Dictionary<string, IEnumerator<YamlDocument>>();
            lastWriteTime = new Dictionary<string, DateTime>();
        }
        
        public object Build(string path, Action interruptChecker)
        {
            var fileIndex = new CacheIndex();
            var parser = new UnityYamlParser();
            IEnumerator<YamlDocument> documentEnumerator;

            if (!WasFileChanged(path) && documentCache.ContainsKey(path))
            {
                documentEnumerator = documentCache[path];
            }
            else
            {
                documentEnumerator = parser.ParseFileStream(path);
            }

            var currentStep = 0;
            var currentIndex = new CacheIndex();

            while (documentEnumerator.MoveNext())
            {
                parser.ParseDocument(documentEnumerator.Current, currentIndex);
                
                currentStep++;
                if (currentStep == AccumulationPhaseSteps)
                {
                    currentStep = 0;
                    fileIndex.Merge(currentIndex);
                    currentIndex = new CacheIndex();
                }
            }
            fileIndex.Merge(currentIndex);
            return fileIndex;
        }

        private bool WasFileChanged(string path)
        {
            if (!lastWriteTime.ContainsKey(path))
            {
                return true;
            }

            return File.GetLastWriteTime(path) == lastWriteTime[path];
        }

        public void Merge(string path, object result)
        {
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