using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetCache
{
    public class GlobalCacheIndex : CacheIndex
    {
        private Dictionary<string, CacheIndex> fileIndexes = new Dictionary<string, CacheIndex>();

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