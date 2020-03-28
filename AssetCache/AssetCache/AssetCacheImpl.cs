using System;
using System.Collections.Generic;

namespace AssetCache
{
    public class AssetCacheImpl : IAssetCache
    {
        public object Build(string path, Action interruptChecker)
        {
            var parser = new UnityYamlParser();
            var documentsEnumerator = parser.ParseFileStream(path);
            while (documentsEnumerator.MoveNext())
            {
                parser.ParseDocument(documentsEnumerator.Current);
            }
            return null;
        }

        public void Merge(string path, object result)
        {
            throw new NotImplementedException();
        }

        public int GetLocalAnchorUsages(ulong anchor)
        {
            throw new NotImplementedException();
        }

        public int GetGuidUsages(string guid)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ulong> GetComponentsFor(ulong gameObjectAnchor)
        {
            throw new NotImplementedException();
        }
    }
}