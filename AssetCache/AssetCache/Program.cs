using System;
using System.IO;

namespace AssetCache
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // 6 documents
            //const string testPath = "/home/vitalii/AssetCache/AssetCache/Examples/Small.unity";
            
            // 6 documents
            const string testPath = "/home/vitalii/AssetCache/AssetCache/Examples/Medium.unity";
            
            // ~440000 documents
            //var testPath = "/home/vitalii/AssetCache/AssetCache/Examples/SampleScene.unity";
            
            var cache = new AssetCacheImpl();

            var i = 0;
            while (true)
            {
                try
                {
                    var cacheResult = cache.Build(testPath, () =>
                    {
                        i++;
                        if (i == 10 || i == 1 || i == 3)
                        {
                            throw new OperationCanceledException();
                        }
                    });
                    cache.Merge(testPath, cacheResult);
                    break;
                }
                catch (OperationCanceledException)
                {
                    if (i <= 3)
                    {
                        File.SetLastWriteTime(testPath, new DateTime(1984, 4, (i + 1) % 29 + 1));
                    }
                }
            }
            

            Console.WriteLine("id=10304: " + cache.GetLocalAnchorUsages(10304));
            Console.WriteLine("guid=0000000000000000f000000000000000: " + 
                              cache.GetGuidUsages("0000000000000000f000000000000000"));
            Console.WriteLine("components of id=241: ");
            var components = cache.GetComponentsFor(241);
            foreach (var component in components)
            {
                Console.WriteLine(component);
            }

            //Console.ReadKey();
        }
    }
}