using System.Collections.Generic;

namespace AssetCache
{
    /// <summary>
    /// An aggregation of several indexes built for different files.
    /// </summary>
    public class GlobalCacheIndex : CacheIndex
    {
        private Dictionary<string, CacheIndex> fileIndexes = new Dictionary<string, CacheIndex>();

        /// <summary>
        /// Merges the given index to the common index.
        /// If the given file is already in index, replaces its index with the given one.
        /// </summary>
        public void AddIndex(string path, CacheIndex index)
        {
            Invalidate(path);
            fileIndexes[path] = index;
            Merge(index);
        }
        
        private void Invalidate(string path)
        {
            if (!fileIndexes.ContainsKey(path))
            {
                return;
            }
            
            RemoveDataFromAnother(fileIndexes[path]);
            fileIndexes.Remove(path);
        }
    }
}