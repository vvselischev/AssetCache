using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace AssetCache
{
    public class FileIncrementCache
    {
        public CacheIndex FileIndex { get; }
        public DateTime LastChangeTime { get; }
        public IEnumerator<YamlDocument> DocumentEnumerator { get; }

        public FileIncrementCache(CacheIndex fileIndex, DateTime lastChangeTime,
            IEnumerator<YamlDocument> documentEnumerator)
        {
            FileIndex = fileIndex;
            LastChangeTime = lastChangeTime;
            DocumentEnumerator = documentEnumerator;
        }
    }
}