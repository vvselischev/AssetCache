using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetCache
{
    public class CacheIndex
    {
        private Dictionary<ulong, int> idUsages = new Dictionary<ulong, int>();
        private Dictionary<string, int> guidUsages = new Dictionary<string, int>();
        private Dictionary<ulong, List<ulong>> attachedComponents = new Dictionary<ulong, List<ulong>>();
        
        public int GetIdUsages(ulong id)
        {
            if (idUsages.ContainsKey(id))
            {
                return idUsages[id];
            }

            return 0;
        }
        
        public int GetGuidUsages(string guid)
        {
            if (guidUsages.ContainsKey(guid))
            {
                return guidUsages[guid];
            }

            return 0;
        }

        public List<ulong> GetAttachedComponents(ulong id)
        {
            if (attachedComponents.ContainsKey(id))
            {
                return attachedComponents[id];
            }
            
            return new List<ulong>();
        }

        public void IncrementIdUsages(ulong id, int delta = 1)
        {
            idUsages[id] = GetIdUsages(id) + delta;
        }

        public void IncrementGuidUsages(string guid, int delta = 1)
        {
            guidUsages[guid] = GetGuidUsages(guid) + delta;
        }
        
        public void AddAttachedComponents(ulong id, IEnumerable<ulong> components)
        {
            attachedComponents[id] = components.ToList();
        }

        public void DecrementIdUsages(ulong id, int delta = 1)
        {
            idUsages[id] = Math.Max(0, GetIdUsages(id) - delta);
        }
        
        public void DecrementGuidUsages(string guid, int delta = 1)
        {
            guidUsages[guid] = Math.Max(0, GetGuidUsages(guid) - delta);
        }

        public void RemoveAttachedComponents(ulong id)
        {
            attachedComponents.Remove(id);
        }
        
        public void Merge(CacheIndex anotherIndex)
        {
            foreach (var keyValue in anotherIndex.idUsages)
            {
                IncrementIdUsages(keyValue.Key, keyValue.Value);
            }
            
            foreach (var keyValue in anotherIndex.guidUsages)
            {
                IncrementGuidUsages(keyValue.Key, keyValue.Value);
            }
            
            foreach (var keyValue in anotherIndex.attachedComponents)
            {
                AddAttachedComponents(keyValue.Key, keyValue.Value);
            }
        }

        public void RemoveDataFromAnother(CacheIndex anotherIndex)
        {
            foreach (var keyValue in anotherIndex.idUsages)
            {
                DecrementIdUsages(keyValue.Key, keyValue.Value);
            }
            
            foreach (var keyValue in anotherIndex.guidUsages)
            {
                DecrementGuidUsages(keyValue.Key, keyValue.Value);
            }
            
            foreach (var keyValue in anotherIndex.attachedComponents)
            {
                RemoveAttachedComponents(keyValue.Key);
            }
        }
    }
}