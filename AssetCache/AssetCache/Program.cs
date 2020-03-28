using System;

namespace AssetCache
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var testPath = "/home/vitalii/AssetCache/AssetCache/Examples/Small.unity";
            //var testPath = "/home/vitalii/AssetCache/AssetCache/Examples/SampleScene.unity";
            var cache = new AssetCacheImpl();
            cache.Build(testPath, null);
            Console.ReadKey();
        }
    }
}