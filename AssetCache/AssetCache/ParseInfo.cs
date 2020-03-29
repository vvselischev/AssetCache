using System.Collections.Generic;

namespace AssetCache
{
    public class ParseInfo
    {
        public ulong Id;
        public Dictionary<ulong, long> IdUsages { get; set; }
        public Dictionary<string, long> GuidUsages { get; set; }
        public List<ulong> AttachedComponents { get; set; }

        public ParseInfo()
        {
            IdUsages = new Dictionary<ulong, long>();
            GuidUsages = new Dictionary<string, long>();
            AttachedComponents = new List<ulong>();
        }
    }
}