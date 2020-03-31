using System;

namespace AssetCache
{
    public class FileIncrementCache
    {
        public CacheIndex FileIndex { get; }
        public DateTime LastChangeTime { get; }
        public int ProcessedLinesNumber { get; }

        public FileIncrementCache(CacheIndex fileIndex, DateTime lastChangeTime,
            int processedLinesNumber)
        {
            FileIndex = fileIndex;
            LastChangeTime = lastChangeTime;
            ProcessedLinesNumber = processedLinesNumber;
        }
    }
}